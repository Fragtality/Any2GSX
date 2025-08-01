using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Plugins;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Any2GSX.Aircraft
{
    public class AircraftController : ServiceController<Any2GSX, AppService, Config, Definition>
    {
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual PluginController PluginController => AppService.Instance?.PluginController;
        public virtual GsxController GsxController => AppService.Instance?.GsxController;
        public virtual GsxAutomationController AutomationController => GsxController?.AutomationController;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual Flightplan Flightplan => AppService.Instance?.Flightplan;
        public virtual DispatcherTimer RefuelTimer { get; } = new();
        protected virtual double FuelCounter { get; set; } = 0;
        protected virtual double FuelProgress { get; set; } = 0;
        protected virtual double FuelRate { get; set; } = 0;
        protected virtual int PaxProgress { get; set; } = 0;
        public virtual bool IsBoarding { get; protected set; } = false;
        public virtual bool IsDeboarding { get; protected set; } = false;
        public virtual double FuelWeightKgPerGallon => SimStore["FUEL WEIGHT PER GALLON"]?.GetNumber() ?? 3.03907;
        public virtual double FuelCapacityKg => FuelWeightKgPerGallon * Aircraft?.FuelCapacityGallon ?? 0;
        public virtual string Title => AppService.Instance.GetTitle();
        public virtual bool IsConnected => Aircraft?.IsConnected == true;
        public virtual bool IsAircraftLoaded => Aircraft != null;
        public virtual bool IsInterfaceInitialized { get; protected set; } = false;
        public virtual string PluginId { get; protected set; } = "NULL";
        public virtual AircraftBase Aircraft { get; protected set; } = null;
        public virtual SettingProfile SettingProfile => AppService.Instance.SettingProfile;


        public AircraftController(Config config) : base(config)
        {
            RefuelTimer.Tick += OnRefuelTick;
        }

        protected virtual async Task InitInterface()
        {
            Aircraft = PluginController.GetAircraftInterface(SettingProfile, SimConnect.AircraftString, out string pluginId);
            PluginId = pluginId;
            Logger.Debug($"Run Init for Plugin '{pluginId}'");
            await Aircraft.Init();
            await Task.Delay(1000, Token);

            ReceiverStore.Get<MsgGsxCouatlStarted>().OnMessage += OnCouatlStarted;
            AutomationController.OnStateChange += OnAutomationStateChange;
            GsxController.WalkaroundWasSkipped += OnWalkaroundWasSkipped;
            SimStore.AddVariable("FUEL WEIGHT PER GALLON", SimUnitType.Kilogram);

            GsxController.ServiceRefuel.OnStateChanged += OnRefuelStateChanged;
            GsxController.ServiceRefuel.OnHoseConnection += OnRefuelHoseChanged;
            GsxController.ServiceBoard.OnStateChanged += OnBoardStateChanged;
            GsxController.ServiceBoard.OnPaxChange += OnBoardPaxChanged;
            GsxController.ServiceBoard.OnCargoChange += OnBoardCargoChanged;
            GsxController.ServiceBoard.OnLoadingChange += OnBoardLoadingChange;
            GsxController.ServiceDeboard.OnStateChanged += OnDeboardStateChanged;
            GsxController.ServiceDeboard.OnPaxChange += OnDeboardPaxChanged;
            GsxController.ServiceDeboard.OnCargoChange += OnDeboardCargoChanged;
            GsxController.ServiceDeboard.OnUnloadingChange += OnDeboardUnloadingChange;
            GsxController.ServiceJetway.OnStateChanged += OnJetwayStateChanged;
            GsxController.ServiceStairs.OnStateChanged += OnStairStateChanged;
            GsxController.ServiceStairs.SubOperating.OnReceived += OnStairOperationChange;
            GsxController.ServicePushBack.OnStateChanged += OnPushStateChange;
            GsxController.ServicePushBack.OnPushStatus += OnPushOperationChange;

            GsxController.SubDoorToggleExit1.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor1, sub.GetNumber());
            GsxController.SubDoorToggleExit2.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor2, sub.GetNumber());
            GsxController.SubDoorToggleExit3.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor3, sub.GetNumber());
            GsxController.SubDoorToggleExit4.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor4, sub.GetNumber());
            GsxController.SubDoorToggleService1.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor1, sub.GetNumber());
            GsxController.SubDoorToggleService2.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor2, sub.GetNumber());
            GsxController.SubDoorToggleCargo1.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor1, sub.GetNumber());
            GsxController.SubDoorToggleCargo2.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor2, sub.GetNumber());
            GsxController.SubDoorToggleCargo3.OnReceived += (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor3Main, sub.GetNumber());
        }

        protected override async Task DoRun()
        {
            try
            {
                Logger.Debug($"Initializing Aircraft Interface ...");
                await InitInterface();
                await Task.Delay(1000, Token);
                IsInterfaceInitialized = true;
                Logger.Debug($"Interface initialized.");

                Logger.Debug($"Waiting for Aircraft Interface Connection ...");
                while (!Aircraft.IsConnected && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                {
                    await Task.Delay(Config.CheckInterval, RequestToken);
                    await Aircraft.CheckConnection();
                }
                await Task.Delay(Config.CheckInterval * 2, RequestToken);
                Logger.Debug($"Aircraft connected.");

                if (Config.DisplayUnitSource == DisplayUnitSource.Aircraft && Aircraft.UnitAircraft != Config.DisplayUnitCurrent)
                {
                    Logger.Debug($"Switching DisplayUnit to Aircraft Source");
                    Config.SetDisplayUnit(Aircraft.UnitAircraft);
                }

                while (SimConnect?.IsSessionRunning == true && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    if (Aircraft?.IsConnected == true)
                        await Aircraft?.RunInterval();
                    else
                        await Aircraft?.CheckConnection();

                    if (SimConnect?.IsSessionRunning == true && IsExecutionAllowed && Aircraft?.RunIntervalMs > 0 && !RequestToken.IsCancellationRequested)
                        await Task.Delay(Aircraft.RunIntervalMs, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public override async Task Stop()
        {
            if (IsInitialized)
            {
                try
                {
                    GsxController.SubDoorToggleExit1.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor1, sub.GetNumber());
                    GsxController.SubDoorToggleExit2.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor2, sub.GetNumber());
                    GsxController.SubDoorToggleExit3.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor3, sub.GetNumber());
                    GsxController.SubDoorToggleExit4.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor4, sub.GetNumber());
                    GsxController.SubDoorToggleService1.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor1, sub.GetNumber());
                    GsxController.SubDoorToggleService2.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor2, sub.GetNumber());
                    GsxController.SubDoorToggleCargo1.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor1, sub.GetNumber());
                    GsxController.SubDoorToggleCargo2.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor2, sub.GetNumber());
                    GsxController.SubDoorToggleCargo3.OnReceived -= (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor3Main, sub.GetNumber());

                    GsxController.ServiceRefuel.OnStateChanged -= OnRefuelStateChanged;
                    GsxController.ServiceRefuel.OnHoseConnection -= OnRefuelHoseChanged;
                    GsxController.ServiceBoard.OnStateChanged -= OnBoardStateChanged;
                    GsxController.ServiceBoard.OnPaxChange -= OnBoardPaxChanged;
                    GsxController.ServiceBoard.OnCargoChange -= OnBoardCargoChanged;
                    GsxController.ServiceBoard.OnLoadingChange -= OnBoardLoadingChange;
                    GsxController.ServiceDeboard.OnStateChanged -= OnDeboardStateChanged;
                    GsxController.ServiceDeboard.OnPaxChange -= OnDeboardPaxChanged;
                    GsxController.ServiceDeboard.OnCargoChange -= OnDeboardCargoChanged;
                    GsxController.ServiceDeboard.OnUnloadingChange -= OnDeboardUnloadingChange;
                    GsxController.ServiceJetway.OnStateChanged -= OnJetwayStateChanged;
                    GsxController.ServiceStairs.OnStateChanged -= OnStairStateChanged;
                    GsxController.ServiceStairs.SubOperating.OnReceived -= OnStairOperationChange;
                    GsxController.ServicePushBack.OnStateChanged -= OnPushStateChange;
                    GsxController.ServicePushBack.OnPushStatus -= OnPushOperationChange;

                    SimStore.Remove("FUEL WEIGHT PER GALLON");
                }
                catch (Exception ex)
                {
                    if (Config.LogLevel == LogLevel.Verbose)
                        Logger.LogException(ex);
                }
            }

            IsInterfaceInitialized = false;
            if (IsInitialized)
            {
                try
                {
                    if (Aircraft != null)
                        await Aircraft?.Stop();
                    PluginController.UnloadPlugin();

                    GsxController.WalkaroundWasSkipped -= OnWalkaroundWasSkipped;
                    AutomationController.OnStateChange -= OnAutomationStateChange;
                    ReceiverStore.Get<MsgGsxCouatlStarted>().OnMessage -= OnCouatlStarted;
                }
                catch (Exception ex)
                {
                    if (Config.LogLevel == LogLevel.Verbose)
                        Logger.LogException(ex);
                }
            }
            Aircraft = null;

            await base.Stop();
        }

        protected virtual async Task OnWalkaroundWasSkipped()
        {
            if (Aircraft.HasFobSaveRestore && SettingProfile.FuelSaveLoadFob && !Aircraft.ReadyForDepartureServices)
            {
                double value = Config.GetFuelFob(Title, FuelCapacityKg, SettingProfile.FuelResetBaseKg, out bool saved);
                await Aircraft.SetFuelOnBoardKg(value);
                Logger.Information($"Initial Fuel set on Aircraft: {Math.Round(Config.ConvertKgToDisplayUnit(value), 0)} {Config.DisplayUnitCurrentString} ({(saved ? "last Session" : "default")})");
            }

            if (Aircraft.CanSetPayload && SettingProfile.ResetPayloadOnPrep && !Aircraft.ReadyForDepartureServices)
            {
                await Aircraft.SetPayloadEmpty();
                Logger.Information($"Initial Payload set to empty");
            }
        }

        protected virtual async Task OnAutomationStateChange(AutomationState state)
        {
            if (state == AutomationState.Arrival)
            {
                if (Aircraft.HasFobSaveRestore && SettingProfile.FuelSaveLoadFob)
                {
                    Config.SetFuelFob(Title, Aircraft.FuelOnBoardKg);
                    Logger.Information($"Fuel saved for Aircraft '{Title}': {Math.Round(Config.ConvertKgToDisplayUnit(Aircraft.FuelOnBoardKg), 0)} {Config.DisplayUnitCurrentString}");
                }
            }
            else if (state == AutomationState.TaxiOut || state == AutomationState.Flight)
                IsBoarding = false;
            else if (state == AutomationState.TurnAround)
            {
                IsDeboarding = false;
                if (Aircraft.CanSetPayload && SettingProfile.ResetPayloadOnTurn)
                {
                    await Aircraft.SetPayloadEmpty();
                    Logger.Information($"Payload set to empty on Turn Around");
                }
            }

            await Aircraft.OnAutomationStateChange(state);
        }

        protected virtual async void OnCouatlStarted(MsgGsxCouatlStarted msg)
        {
            await Aircraft?.OnCouatlStarted();

            if ((AutomationController.State == AutomationState.Departure || AutomationController.State == AutomationState.Pushback)
                && IsBoarding)
            {
                IsBoarding = false;
                await Aircraft.BoardCompleted(Flightplan.CountPax, Flightplan.WeightPerPaxKg, Flightplan.WeightCargoKg);
                Logger.Information($"Boarding set to completed due to GSX Restart");
            }

            if ((AutomationController.State == AutomationState.Arrival || AutomationController.State == AutomationState.TurnAround)
                && IsDeboarding)
            {
                IsDeboarding = false;
                await Aircraft.DeboardCompleted();
                Logger.Information($"Deboarding set to completed due to GSX Restart");
            }

            if (AutomationController.State == AutomationState.Pushback &&
                RefuelTimer.IsEnabled && !SettingProfile.RefuelFinishOnHose)
            {
                await RefuelStopEarly();
                Logger.Information($"Aircraft Refueling finished early due to GSX Restart");
            }
        }

        protected virtual Task OnDoorTrigger(GsxDoor door, double value)
        {
            Logger.Debug($"Received Trigger for Door {door}: {value}");
            if (GsxController.IsActive && AutomationController.State != AutomationState.Flight && AutomationController.State > AutomationState.SessionStart)
                return Aircraft.OnDoorTrigger(door, value > 0);
            else
                return Task.CompletedTask;
        }

        protected virtual Task OnJetwayStateChanged(IGsxService service)
        {
            if (GsxController.IsActive && AutomationController.State != AutomationState.Flight && AutomationController.State > AutomationState.SessionStart)
                Aircraft.OnJetwayChange(service.State);

            return Task.CompletedTask;
        }

        protected virtual Task OnStairStateChanged(IGsxService service)
        {
            if (GsxController.IsActive && AutomationController.State != AutomationState.Flight && AutomationController.State > AutomationState.SessionStart)
                Aircraft.OnStairChange(service.State);

            return Task.CompletedTask;
        }

        protected virtual void OnStairOperationChange(ISimResourceSubscription sub, object data)
        {
            if (GsxController.IsActive && AutomationController.State != AutomationState.Flight && AutomationController.State > AutomationState.SessionStart)
                return;

            double dValue = sub.GetNumber();
            Aircraft.OnStairOperationChange((GsxServiceState)((int)dValue));
        }

        protected virtual Task OnPushStateChange(IGsxService servicePushback)
        {
            if (!GsxController.IsActive || AutomationController.State == AutomationState.Flight || AutomationController.State <= AutomationState.SessionStart)
                return Task.CompletedTask;

            return Aircraft.PushStateChange(servicePushback.State);
        }

        protected virtual Task OnPushOperationChange(GsxServicePushback servicePushback)
        {
            if (!GsxController.IsActive || AutomationController.State == AutomationState.Flight || AutomationController.State <= AutomationState.SessionStart)
                return Task.CompletedTask;

            Aircraft.PushOperationChange(servicePushback.PushStatus);
            return Task.CompletedTask;
        }

        protected virtual async Task OnRefuelStateChanged(IGsxService service)
        {
            if (!GsxController.IsActive)
                return;

            if (service.State == GsxServiceState.Active && !GsxController.ServiceRefuel.IsHoseConnected)
            {
                if (Aircraft.FuelOnBoardKg >= Flightplan.FuelRampKg - Config.FuelCompareVariance && Aircraft.HasFuelSynch && SettingProfile.RefuelResetDeltaKg > 0)
                {
                    Logger.Warning($"Resetting FOB for GSX Refuel (FOB >= Planned)");
                    double target = Flightplan.FuelRampKg - SettingProfile.RefuelResetDeltaKg;
                    Logger.Debug($"FOB: {Aircraft.FuelOnBoardKg} | PlanRamp: {Flightplan.FuelRampKg} | ResetTarget: {target}");
                    await Aircraft.SetFuelOnBoardKg(target);
                }
                await Aircraft.RefuelActive();
            }
            else if (service.State == GsxServiceState.Completed && AutomationController.State <= AutomationState.Pushback)
            {
                if (Aircraft.HasFuelSynch && RefuelTimer.IsEnabled && SettingProfile.RefuelFinishOnHose
                    && Aircraft.FuelOnBoardKg <= Flightplan.FuelRampKg - Config.FuelCompareVariance && !GsxController.ServicePushBack.IsRunning)
                {
                    Logger.Warning($"Instant load Fuel - FOB did not match planned after GSX Refuel");
                    Logger.Debug($"FOB: {Aircraft.FuelOnBoardKg} | PlanRamp: {Flightplan.FuelRampKg}");
                    await RefuelStopEarly();
                }
                await Aircraft.RefuelCompleted();
            }

            return;
        }

        protected virtual async Task RefuelStopEarly()
        {
            Logger.Debug($"Stop Refuel - Hose disconnected early: {FuelCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {Aircraft.FuelOnBoardKg}");
            RefuelTimer.Stop();
            FuelCounter = Flightplan.FuelRampKg;
            FuelProgress = Flightplan.FuelRampKg;
            await Aircraft.RefuelStop(FuelCounter, true);
        }

        public virtual async Task OnRefuelHoseChanged(bool connected)
        {
            if (!GsxController.IsActive)
                return;

            if (connected && GsxController.ServiceRefuel.IsRunning)
            {
                SetRefuelRate();
                await Aircraft.RefuelStart(Flightplan.FuelRampKg);
                if (Aircraft.HasFuelSynch && !RefuelTimer.IsEnabled)
                {
                    RefuelTimer.Interval = TimeSpan.FromMilliseconds(Aircraft.RefuelIntervalMs);
                    FuelProgress = 0;
                    RefuelTimer.Start();
                    Logger.Debug($"Aircraft Refuel Timer started (Interval {Aircraft.RefuelIntervalMs}ms)");
                    Logger.Information($"Aircraft Refueling started. Refuel Rate: {Math.Round(Config.ConvertKgToDisplayUnit(FuelRate), 1)}{Config.DisplayUnitCurrentString}/s");
                }
            }
            else if (!connected && RefuelTimer.IsEnabled && SettingProfile.RefuelFinishOnHose)
            {
                await RefuelStopEarly();
                Logger.Information($"Aircraft Refueling finished early (Hose disconnected). FOB: {Math.Round(Config.ConvertKgToDisplayUnit(Aircraft.FuelOnBoardKg), 2)}{Config.DisplayUnitCurrentString}");
            }
        }

        protected virtual void SetRefuelRate()
        {
            FuelCounter = Aircraft.FuelOnBoardKg;
            if (SettingProfile.UseRefuelTimeTarget)
            {
                FuelRate = (Flightplan.FuelRampKg - FuelCounter) / SettingProfile.RefuelTimeTargetSeconds;
                if (FuelRate <= 0)
                    FuelRate = SettingProfile.RefuelRateKgSec;
            }
            else
                FuelRate = SettingProfile.RefuelRateKgSec;
        }

        protected virtual void OnRefuelTick(object? sender, EventArgs e)
        {
            if (Token.IsCancellationRequested || Aircraft == null || Config == null)
            {
                RefuelTimer?.Stop();
                return;
            }

            FuelCounter += FuelRate;
            FuelProgress += FuelRate;

            if (Aircraft.FuelOnBoardKg >= Flightplan.FuelRampKg - Config.FuelCompareVariance)
            {
                FuelCounter = Flightplan.FuelRampKg;
                Logger.Debug($"Last-Tick: {FuelCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {Aircraft.FuelOnBoardKg}");
                Aircraft.RefuelTick(FuelRate, FuelCounter);
                RefuelTimer.Stop();
                Logger.Debug($"Aircraft Refuel Timer ended");
                Aircraft.RefuelStop(FuelCounter, false);
                Logger.Information($"Aircraft Refueling finished. FOB: {Math.Round(Config.ConvertKgToDisplayUnit(Aircraft.FuelOnBoardKg), 2)}{Config.DisplayUnitCurrentString}");
            }
            else
            {
                Logger.Verbose($"Refuel-Tick: {FuelCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {Aircraft.FuelOnBoardKg}");
                Aircraft.RefuelTick(FuelRate, FuelCounter);
                if (FuelProgress > 1000)
                {
                    Logger.Information($"Aircraft Refueling in Progress. FOB: {Math.Round(Config.ConvertKgToDisplayUnit(Aircraft.FuelOnBoardKg), 2)}{Config.DisplayUnitCurrentString}");
                    FuelProgress = 0;
                }
            }
        }

        protected virtual Task OnBoardStateChanged(IGsxService service)
        {
            if (!GsxController.IsActive)
                return Task.CompletedTask;

            if (service.State == GsxServiceState.Active)
            {
                PaxProgress = 0;
                IsBoarding = true;
                Aircraft.BoardActive((service as GsxServiceBoarding).PaxTarget, Flightplan.WeightCargoKg);
                Logger.Information($"Boarding has started");
            }
            else if (service.State == GsxServiceState.Completed && AutomationController.State <= AutomationState.Pushback)
            {
                IsBoarding = false;
                Aircraft.BoardCompleted((service as GsxServiceBoarding).PaxTarget, Flightplan.WeightPerPaxKg, Flightplan.WeightCargoKg);
                Logger.Information($"Boarding is completed");
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnBoardPaxChanged(GsxServiceBoarding serviceBoarding)
        {
            if (!GsxController.IsActive || serviceBoarding.State < GsxServiceState.Requested)
                return Task.CompletedTask;

            Aircraft.BoardChangePax(serviceBoarding.PaxTotal, Flightplan.WeightPerPaxKg);
            if (serviceBoarding.PaxTotal - PaxProgress >= 20)
            {
                Logger.Information($"Boarding in Progress. Pax boarded: {serviceBoarding.PaxTotal}");
                PaxProgress = serviceBoarding.PaxTotal;
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnBoardCargoChanged(GsxServiceBoarding serviceBoarding)
        {
            if (!GsxController.IsActive || serviceBoarding.State < GsxServiceState.Requested)
                return Task.CompletedTask;

            Aircraft.BoardChangeCargo(serviceBoarding.CargoPercent, Flightplan.WeightCargoKg * serviceBoarding.CargoScalar);
            if (serviceBoarding.CargoPercent > 0 && Aircraft.IsCargo && Flightplan.CountPax != 0 && Aircraft.PaxOnBoard != Flightplan.CountPax)
            {
                Logger.Information($"Setting Crew Weight for Cargo Aircraft");
                Aircraft.SetCargoCrew(Flightplan.CountPax, Flightplan.WeightPerPaxKg);
            }
            return Task.CompletedTask;
        }

        protected virtual Task OnBoardLoadingChange(GsxDoor door, bool state)
        {
            if (!GsxController.IsActive || (AutomationController.State > AutomationState.Pushback && AutomationController.State < AutomationState.Arrival))
                return Task.CompletedTask;

            Aircraft.BoardLoadingChange(door, state);
            return Task.CompletedTask;
        }

        protected virtual Task OnDeboardStateChanged(IGsxService service)
        {
            if (!GsxController.IsActive || service.State < GsxServiceState.Requested)
                return Task.CompletedTask;

            if (service.State == GsxServiceState.Active)
            {
                PaxProgress = 0;
                IsDeboarding = true;
                Aircraft.DeboardActive();
                Logger.Information($"Deboarding has started");
            }
            else if (service.State == GsxServiceState.Completed && AutomationController.State >= AutomationState.Arrival)
            {
                IsDeboarding = false;
                Aircraft.DeboardCompleted();
                Logger.Information($"Deboarding is completed");
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnDeboardPaxChanged(GsxServiceDeboarding serviceDeboarding)
        {
            if (!GsxController.IsActive || serviceDeboarding.State < GsxServiceState.Requested)
                return Task.CompletedTask;

            Aircraft.DeboardChangePax(Aircraft.PaxOnBoard - serviceDeboarding.PaxTotal, serviceDeboarding.PaxTotal, Flightplan.WeightPerPaxKg);
            if (serviceDeboarding.PaxTotal - PaxProgress >= 20)
            {
                Logger.Information($"Deboarding in Progress. Pax deboarded: {serviceDeboarding.PaxTotal}");
                PaxProgress = serviceDeboarding.PaxTotal;
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnDeboardCargoChanged(GsxServiceDeboarding serviceDeboarding)
        {
            if (!GsxController.IsActive || serviceDeboarding.State < GsxServiceState.Requested)
                return Task.CompletedTask;

            int percent = 100 - serviceDeboarding.CargoPercent;
            double scalar = percent / 100.0;

            Aircraft.DeboardChangeCargo(percent, Flightplan.WeightCargoKg * scalar);
            return Task.CompletedTask;
        }

        protected virtual Task OnDeboardUnloadingChange(GsxDoor door, bool state)
        {
            if (!GsxController.IsActive || (AutomationController.State > AutomationState.Pushback && AutomationController.State < AutomationState.Arrival))
                return Task.CompletedTask;

            Aircraft.DeboardUnloadingChange(door, state);
            return Task.CompletedTask;
        }
    }
}
