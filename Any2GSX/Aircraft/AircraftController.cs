using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Plugins;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public virtual DispatcherTimerAsync RefuelTimer { get; } = new();
        protected virtual double FuelFobCounter { get; set; } = 0;
        protected virtual double FuelProgress { get; set; } = 0;
        protected virtual double FuelRate { get; set; } = 0;
        protected virtual int PaxProgress { get; set; } = 0;
        protected virtual int CargoApplyProgress { get; set; } = 0;
        protected virtual bool CargoApplyActive { get; set; } = false;
        public virtual bool IsBoarding { get; protected set; } = false;
        public virtual bool IsDeboarding { get; protected set; } = false;
        public virtual double FuelWeightKgPerGallon => SimStore["FUEL WEIGHT PER GALLON"]?.GetNumber() ?? 3.03907;
        public virtual double FuelCapacityKg => FuelWeightKgPerGallon * Aircraft?.FuelCapacityGallon ?? 0;
        public virtual string Title => AppService.Instance.AircraftTitle;
        public virtual bool IsConnected => Aircraft?.IsConnected == true;
        public virtual bool IsAircraftLoaded => Aircraft != null;
        public virtual string PluginId { get; protected set; } = "NULL";
        public virtual bool HasFuelDialog { get; protected set; } = false;
        public virtual AircraftBase Aircraft { get; protected set; } = null;
        public virtual SettingProfile SettingProfile => AppService.Instance.SettingProfile;
        protected virtual bool IsValidState => GsxController.IsActive && Aircraft?.IsConnected == true;
        protected virtual bool IsValidGroundPhase => GsxController.AutomationState < AutomationState.TaxiOut || GsxController.AutomationState > AutomationState.TaxiIn;
        protected virtual AutomationState LoaderConnectPhase { get; set; } = AutomationState.Unknown;
        protected virtual List<SimResourceSubCallback> SubscriptionCallbacks { get; } = [];


        public AircraftController(Config config) : base(config)
        {
            RefuelTimer.Tick += OnRefuelTick;
        }

        public virtual async Task Restart()
        {
            Logger.Debug($"Restart AircraftController");
            await Stop();
            await Task.Delay(1000, Token);
            await Start();
        }

        protected override async Task DoInit()
        {
            Logger.Debug($"Initializing Aircraft Interface ...");
            Aircraft = PluginController.GetAircraftInterface(SettingProfile, SimConnect.AircraftString, out string pluginId);
            PluginId = pluginId;
            Logger.Debug($"Run Init for Plugin '{pluginId}'");
            await Aircraft.Init();

            GsxController.OnCouatlStarted += OnCouatlStarted;
            AutomationController.OnStateChange += OnAutomationStateChange;
            SimStore.AddVariable("FUEL WEIGHT PER GALLON", SimUnitType.Kilogram);

            GsxController.ServiceRefuel.OnStateChanged += OnRefuelStateChanged;
            GsxController.ServiceRefuel.OnHoseConnection += OnRefuelHoseChanged;
            GsxController.ServiceBoard.OnStateChanged += OnBoardStateChanged;
            GsxController.ServiceBoard.OnPaxChange += OnBoardPaxChanged;
            GsxController.ServiceBoard.OnCargoChange += OnBoardCargoChanged;
            GsxController.ServiceDeboard.OnStateChanged += OnDeboardStateChanged;
            GsxController.ServiceDeboard.OnPaxChange += OnDeboardPaxChanged;
            GsxController.ServiceDeboard.OnCargoChange += OnDeboardCargoChanged;
            GsxController.ServiceJetway.OnStateChanged += OnJetwayStateChanged;
            GsxController.ServiceJetway.OnOperationChanged += OnJetwayOperationChange;
            GsxController.ServiceStairs.OnStateChanged += OnStairStateChanged;
            GsxController.ServiceStairs.OnOperationChanged += OnStairOperationChange;
            GsxController.ServicePushBack.OnStateChanged += OnPushStateChange;
            GsxController.ServicePushBack.OnPushStatus += OnPushOperationChange;
            GsxController.ServiceGpu.OnGpuConnection += OnGpuConnectionChange;
            GsxController.ServiceLavatory.OnStateChanged += OnLavatoryStateChange;
            GsxController.ServiceWater.OnStateChanged += OnWaterStateChange;

            GsxController.WalkaroundPreAction += BeforeWalkaroundSkip;
            GsxController.WalkaroundWasSkipped += AfterWalkaroundSkip;

            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleExit1, (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor1, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleExit2, (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor2, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleExit3, (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor3, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleExit4, (sub, data) => OnDoorTrigger(GsxDoor.PaxDoor4, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleService1, (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor1, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleService2, (sub, data) => OnDoorTrigger(GsxDoor.ServiceDoor2, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleCargo1, (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor1, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleCargo2, (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor2, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubDoorToggleCargo3, (sub, data) => OnDoorTrigger(GsxDoor.CargoDoor3Main, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubLoaderAttachCargo1, (sub, data) => OnLoaderAttached(GsxDoor.CargoDoor1, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubLoaderAttachCargo2, (sub, data) => OnLoaderAttached(GsxDoor.CargoDoor2, sub.GetNumber())));
            SubscriptionCallbacks.Add(new(GsxController.SubLoaderAttachCargo3, (sub, data) => OnLoaderAttached(GsxDoor.CargoDoor3Main, sub.GetNumber())));

            Flightplan.OnImport += OnFlightplanImport;
            Flightplan.OnUnload += OnFlightplanUnload;
        }

        protected override async Task DoRun()
        {
            try
            {
                if (Aircraft.InitDelay <= 0)
                    await Task.Delay(1000, Token);
                Logger.Debug($"Interface initialized.");

                Logger.Debug($"Waiting for Aircraft Interface Connection ...");
                while (!Aircraft.IsConnected && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                {
                    await Task.Delay(Config.CheckInterval, RequestToken);
                    await Aircraft.CheckConnection();
                }
                Logger.Debug($"Aircraft connected.");
                await Task.Delay(Config.CheckInterval * 2, RequestToken);

                var units = await Aircraft.GetAircraftUnits();
                if (Config.DisplayUnitSource == DisplayUnitSource.Aircraft && units != Config.DisplayUnitCurrent)
                {
                    Logger.Debug($"Switching DisplayUnit to Aircraft Source");
                    Config.SetDisplayUnit(units);
                }
                HasFuelDialog = await Aircraft.GetSettingFuelDialog();

                while (SimConnect?.IsSessionRunning == true && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    if (Aircraft?.IsConnected == true)
                        await Aircraft?.RunInterval();
                    else
                        await Aircraft?.CheckConnection();

                    if (SimConnect?.IsSessionRunning == true && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                    {
                        if ((Aircraft?.RunIntervalMs ?? 0) > 0)
                            await Task.Delay(Aircraft.RunIntervalMs, RequestToken);
                        else
                            await Task.Delay(1000, RequestToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected override async Task DoCleanup()
        {
            try
            {
                LoaderConnectPhase = AutomationState.Unknown;

                foreach (var callback in SubscriptionCallbacks)
                    callback.Unsubscribe();

                GsxController.ServiceRefuel.OnStateChanged -= OnRefuelStateChanged;
                GsxController.ServiceRefuel.OnHoseConnection -= OnRefuelHoseChanged;
                GsxController.ServiceBoard.OnStateChanged -= OnBoardStateChanged;
                GsxController.ServiceBoard.OnPaxChange -= OnBoardPaxChanged;
                GsxController.ServiceBoard.OnCargoChange -= OnBoardCargoChanged;
                GsxController.ServiceDeboard.OnStateChanged -= OnDeboardStateChanged;
                GsxController.ServiceDeboard.OnPaxChange -= OnDeboardPaxChanged;
                GsxController.ServiceDeboard.OnCargoChange -= OnDeboardCargoChanged;
                GsxController.ServiceJetway.OnStateChanged -= OnJetwayStateChanged;
                GsxController.ServiceJetway.OnOperationChanged -= OnJetwayOperationChange;
                GsxController.ServiceStairs.OnStateChanged -= OnStairStateChanged;
                GsxController.ServiceStairs.OnOperationChanged -= OnStairOperationChange;
                GsxController.ServicePushBack.OnStateChanged -= OnPushStateChange;
                GsxController.ServicePushBack.OnPushStatus -= OnPushOperationChange;
                GsxController.ServiceGpu.OnGpuConnection -= OnGpuConnectionChange;
                GsxController.ServiceLavatory.OnStateChanged -= OnLavatoryStateChange;
                GsxController.ServiceWater.OnStateChanged -= OnWaterStateChange;

                GsxController.WalkaroundPreAction -= BeforeWalkaroundSkip;
                GsxController.WalkaroundWasSkipped -= AfterWalkaroundSkip;

                Flightplan.OnImport -= OnFlightplanImport;
                Flightplan.OnUnload -= OnFlightplanUnload;

                SimStore.Remove("FUEL WEIGHT PER GALLON");
            }
            catch (Exception ex)
            {
                if (Config.LogLevel == LogLevel.Verbose)
                    Logger.LogException(ex);
            }

            try
            {
                if (Aircraft != null)
                    await Aircraft?.Stop();
                PluginController.UnloadPlugin();

                AutomationController.OnStateChange -= OnAutomationStateChange;
                GsxController.OnCouatlStarted -= OnCouatlStarted;
            }
            catch (Exception ex)
            {
                if (Config.LogLevel == LogLevel.Verbose)
                    Logger.LogException(ex);
            }

            Aircraft = null;
        }

        public virtual Task<bool> GetIsCargo()
        {
            if (Aircraft == null)
                return Task.FromResult(false);
            else
                return Aircraft.GetIsCargo();
        }

        public virtual Task<bool> GetEngineRunning()
        {
            if (Aircraft == null)
                return Task.FromResult(false);
            else
                return Aircraft.GetEngineRunning();
        }

        public virtual Task<bool> GetIsFuelOnStairSide()
        {
            if (Aircraft == null)
                return Task.FromResult(false);
            else
                return Aircraft.GetIsFuelOnStairSide();
        }

        protected virtual async Task SetInitialFuelPayload()
        {
            if (!IsConnected)
            {
                Logger.Debug("Aircraft not connected - cannot set Fuel/Payload");
                return;
            }

            var isReady = await Aircraft.GetReadyDepartureServices();
            if (await Aircraft.GetHasFobSaveRestore() && SettingProfile.FuelSaveLoadFob && !isReady)
            {
                double value = Config.GetFuelFob(Title, FuelCapacityKg, SettingProfile.FuelResetBaseKg, out bool saved);
                await Aircraft.SetFuelOnBoardKg(value, value);
                Logger.Information($"Initial Fuel set on Aircraft: {Math.Round(Config.ConvertKgToDisplayUnit(value), 0)} {Config.DisplayUnitCurrentString} ({(saved ? "last Session" : "default")})");
            }

            if (await Aircraft.GetCanSetPayload() && SettingProfile.ResetPayloadOnPrep && !isReady)
            {
                await Aircraft.SetPayloadEmpty();
                Logger.Information($"Initial Payload set to empty");
            }
        }

        protected virtual async Task OnAutomationStateChange(AutomationState state)
        {
            if (state == AutomationState.SessionStart)
                LoaderConnectPhase = AutomationState.Unknown;
            else if (state == AutomationState.Preparation)
                await SetInitialFuelPayload();
            else if (state == AutomationState.Arrival)
            {
                if (await Aircraft.GetHasFobSaveRestore() && SettingProfile.FuelSaveLoadFob)
                {
                    var fob = await Aircraft.GetFuelOnBoardKg();
                    Config.SetFuelFob(Title, fob);
                    Logger.Information($"Fuel saved for Aircraft '{Title}': {Math.Round(Config.ConvertKgToDisplayUnit(fob), 0)} {Config.DisplayUnitCurrentString}");
                }
            }
            else if (state == AutomationState.TaxiOut || state == AutomationState.Flight)
            {
                IsBoarding = false;
                LoaderConnectPhase = AutomationState.Unknown;
            }
            else if (state == AutomationState.TurnAround)
            {
                IsDeboarding = false;
                if (await Aircraft.GetCanSetPayload() && SettingProfile.ResetPayloadOnTurn)
                {
                    await Aircraft.SetPayloadEmpty();
                    Logger.Information($"Payload set to empty on Turn Around");
                }
            }

            await Aircraft.OnAutomationStateChange(state);
        }

        protected virtual async Task OnCouatlStarted(IGsxController gsxController)
        {
            if (!IsValidState)
                return;

            await Aircraft.OnCouatlStarted();

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

            if (AutomationController.State == AutomationState.Pushback && RefuelTimer.IsEnabled)
            {
                await RefuelStopEarly();
                Logger.Information($"Aircraft Refueling finished early due to GSX Restart");
            }
        }

        protected virtual Task BeforeWalkaroundSkip()
        {
            if (IsValidState)
                return Aircraft.BeforeWalkaroundSkip();
            else
                return Task.CompletedTask;
        }

        protected virtual Task AfterWalkaroundSkip()
        {
            if (IsValidState)
                return Aircraft.AfterWalkaroundSkip();
            else
                return Task.CompletedTask;
        }

        protected virtual Task OnDoorTrigger(GsxDoor door, double value)
        {
            if (!IsValidState || !IsValidGroundPhase || (GsxController.ServiceStairs.OverrideActive && IGsxController.IsPaxDoor(door)))
                return Task.CompletedTask;

            Logger.Debug($"Received Trigger for Door {door}: {value}");
            if (IGsxController.IsServiceDoor(door) && SettingProfile.DoorServiceHandling || IGsxController.IsCargoDoor(door) && SettingProfile.DoorCargoHandling || IGsxController.IsPaxDoor(door) && SettingProfile.DoorPaxHandling)
                return Aircraft.OnDoorTrigger(door, value > 0);
            else
                return Task.CompletedTask;
        }

        protected virtual async Task CargoDoorsOpenCloseDelayed(bool target)
        {
            try
            {
                Logger.Debug($"{(target ? "Open" : "Close")} Cargo Doors afer {SettingProfile.DoorCargoOpenCloseDelay}s");
                if (SettingProfile.DoorCargoOpenCloseDelay > 0)
                    await Task.Delay(SettingProfile.DoorCargoOpenCloseDelay * 1000, RequestToken);
                await Aircraft.SetCargoDoors(target, true);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual Task OnLoaderAttached(GsxDoor door, double value)
        {
            Logger.Debug($"Received Loader attached for Door {door}: {value}");
            if (!IsValidState || !IsValidGroundPhase || !IGsxController.IsCargoDoor(door))
                return Task.CompletedTask;

            bool state = value > 0;
            if (state && LoaderConnectPhase != AutomationController.State)
            {
                LoaderConnectPhase = AutomationController.State;
                Logger.Debug($"Loader connected in Phase {LoaderConnectPhase}");
            }

            if (!SettingProfile.DoorCargoHandling)
                return Task.CompletedTask;

            if (state || (!state && !SettingProfile.DoorsCargoKeepOpenOnDetachBoard && !SettingProfile.DoorsCargoKeepOpenOnDetachDeboard)
                || (!state && LoaderConnectPhase == AutomationState.Departure && !SettingProfile.DoorsCargoKeepOpenOnDetachBoard)
                || (!state && LoaderConnectPhase == AutomationState.Arrival && !SettingProfile.DoorsCargoKeepOpenOnDetachDeboard))
            {
                Logger.Debug($"Notify OnLoaderAttached {state} on Door {door}");
                return Aircraft.OnLoaderAttached(door, state);
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnJetwayStateChanged(IGsxService service)
        {
            if (IsValidState && IsValidGroundPhase)
                return Aircraft.OnJetwayStateChange(service.State, SettingProfile.DoorPaxHandling);
            return Task.CompletedTask;
        }

        protected virtual Task OnJetwayOperationChange(GsxServiceState state)
        {
            if (IsValidState && IsValidGroundPhase)
                return Aircraft.OnJetwayOperationChange(state, SettingProfile.DoorPaxHandling);
            return Task.CompletedTask;
        }

        protected virtual Task OnStairStateChanged(IGsxService service)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
                return Aircraft.OnStairStateChange(service.State, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);
            return Task.CompletedTask;
        }

        protected virtual Task OnStairOperationChange(GsxServiceState state)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
                return Aircraft.OnStairOperationChange(state, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);
            return Task.CompletedTask;
        }

        protected virtual Task OnStairVehicleChange(GsxVehicleStair stair, GsxVehicleStairState state)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
                return Aircraft.OnStairVerhicleChange(stair, state, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);
            return Task.CompletedTask;
        }

        protected virtual Task OnPushStateChange(IGsxService servicePushback)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return Task.CompletedTask;

            return Aircraft.PushStateChange(servicePushback.State);
        }

        protected virtual Task OnPushOperationChange(GsxServicePushback servicePushback)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return Task.CompletedTask;

            return Aircraft.PushOperationChange(servicePushback.PushStatus);
        }

        protected virtual async Task OnRefuelStateChanged(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return;

            var hasSync = await Aircraft.GetHasFuelSync();
            var fob = await Aircraft.GetFuelOnBoardKg();
            if (service.State == GsxServiceState.Active && !GsxController.ServiceRefuel.IsHoseConnected)
            {
                await Aircraft.RefuelActive();
                if (SettingProfile.DoorPanelHandling)
                {
                    int delay = Config.PanelRefuelOpenDelayUnderground;
                    if (!GsxController.ServiceRefuel.IsUnderground)
                        delay = Config.PanelRefuelOpenDelayTanker;
                    _ = TaskTools.RunDelayed(() => Aircraft.SetPanelRefuel(true), delay * 1000, RequestToken);
                }
            }
            else if (service.IsCompleted && AutomationController.State <= AutomationState.Pushback)
            {
                if (hasSync && RefuelTimer.IsEnabled && GsxController.IsGsxRunning && !GsxController.ServicePushBack.IsRunning
                    && fob <= Flightplan.FuelRampKg - Config.FuelCompareVariance)
                {
                    Logger.Warning($"Instant load Fuel - FOB did not match planned after GSX Refuel");
                    Logger.Debug($"FOB: {fob} | PlanRamp: {Flightplan.FuelRampKg}");
                    await RefuelStopEarly();
                }
                await Aircraft.RefuelCompleted();
            }

            return;
        }

        protected virtual async Task RefuelStopEarly()
        {
            await Task.Delay(250);
            Logger.Debug($"Stop Refuel - Hose disconnected early: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {await Aircraft.GetFuelOnBoardKg()}");
            RefuelTimer.Stop();
            FuelFobCounter = Flightplan.FuelRampKg;
            FuelProgress = Flightplan.FuelRampKg;
            await Aircraft.RefuelStop(FuelFobCounter, true);
            if (SettingProfile.DoorPanelHandling)
                await Aircraft.SetPanelRefuel(false);
        }

        public virtual async Task OnRefuelHoseChanged(bool connected)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return;

            if (connected && GsxController.ServiceRefuel.IsRunning)
            {
                bool isFuelInc = await SetRefuelRate();
                await Aircraft.RefuelStart(Flightplan.FuelRampKg);
                if (await Aircraft.GetHasFuelSync() && !RefuelTimer.IsEnabled)
                {
                    RefuelTimer.Interval = TimeSpan.FromMilliseconds(Aircraft.RefuelIntervalMs);
                    FuelProgress = 0;
                    RefuelTimer.Start();
                    Logger.Debug($"Aircraft Refuel Timer started (Interval {Aircraft.RefuelIntervalMs}ms)");
                    Logger.Information($"Aircraft {(isFuelInc ? "Refueling" : "Defueling")} started. Refuel Rate: {(isFuelInc ? "" : "-")}{Math.Round(Config.ConvertKgToDisplayUnit(FuelRate), 1)}{Config.DisplayUnitCurrentString}/s{(SettingProfile.UseRefuelTimeTarget ? $" (Time Target {SettingProfile.RefuelTimeTargetSeconds}s)" : "")}");
                }
            }
            else if (!connected && RefuelTimer.IsEnabled && GsxController.IsGsxRunning && (SettingProfile.RefuelFinishOnHose || GsxController.ServiceRefuel.IsCompleting))
            {
                await RefuelStopEarly();
                Logger.Information($"Aircraft Refueling finished early (Hose disconnected). FOB: {Math.Round(Config.ConvertKgToDisplayUnit(await Aircraft.GetFuelOnBoardKg()), 2)}{Config.DisplayUnitCurrentString}");
            }

            if (!connected && SettingProfile.DoorPanelHandling && (GsxController.AutomationState == AutomationState.Departure || GsxController.AutomationState == AutomationState.Pushback || GsxController.AutomationState == AutomationState.Arrival))
            {
                int delay = Config.PanelRefuelCloseDelayUnderground;
                if (!GsxController.ServiceRefuel.IsUnderground)
                    delay = Config.PanelRefuelCloseDelayTanker;
                _ = TaskTools.RunDelayed(() => Aircraft.SetPanelRefuel(false), delay * 1000, RequestToken);
            }
        }

        protected virtual async Task<bool> SetRefuelRate()
        {
            FuelFobCounter = await Aircraft.GetFuelOnBoardKg();
            bool isFuelInc = FuelFobCounter <= Flightplan.FuelRampKg;
            if (SettingProfile.UseRefuelTimeTarget)
                FuelRate = Math.Abs(Flightplan.FuelRampKg - FuelFobCounter) / SettingProfile.RefuelTimeTargetSeconds;
            else
                FuelRate = SettingProfile.RefuelRateKgSec;

            return isFuelInc;
        }

        protected virtual async Task OnRefuelTick()
        {
            try
            {
                if (Token.IsCancellationRequested || Aircraft == null || Config == null)
                {
                    RefuelTimer?.Stop();
                    return;
                }

                if (!SettingProfile.UseRefuelTimeTarget)
                    FuelRate = SettingProfile.RefuelRateKgSec;

                double fob = await Aircraft.GetFuelOnBoardKg();
                bool isFuelInc = fob <= Flightplan.FuelRampKg;

                FuelFobCounter += FuelRate * (isFuelInc ? 1 : -1);
                FuelProgress += FuelRate;


                if (Math.Abs(fob - Flightplan.FuelRampKg) <= Config.FuelCompareVariance)
                {
                    FuelFobCounter = Flightplan.FuelRampKg;
                    Logger.Debug($"Last-Tick: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {fob}");
                    await Aircraft.RefuelTick(isFuelInc, FuelRate, FuelFobCounter, Flightplan.FuelRampKg);
                    RefuelTimer.Stop();
                    Logger.Debug($"Aircraft Refuel Timer ended");
                    await Aircraft.RefuelStop(FuelFobCounter, false);
                    await Task.Delay(200);
                    fob = await Aircraft.GetFuelOnBoardKg();
                    Logger.Information($"Aircraft Refueling finished. FOB: {Math.Round(Config.ConvertKgToDisplayUnit(fob), 2)}{Config.DisplayUnitCurrentString}");
                    _ = TaskTools.RunDelayed(CancelGsxRefuel, Config.RefuelDisconnectTimeout, Token);
                }
                else
                {
                    Logger.Verbose($"Refuel-Tick: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {fob}");
                    await Aircraft.RefuelTick(isFuelInc, FuelRate, FuelFobCounter, Flightplan.FuelRampKg);
                    if (FuelProgress > 1000)
                    {
                        Logger.Information($"Aircraft Refueling in Progress. FOB: {Math.Round(Config.ConvertKgToDisplayUnit(fob), 2)}{Config.DisplayUnitCurrentString}");
                        FuelProgress = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual async Task CancelGsxRefuel()
        {
            if (GsxController.ServiceRefuel.IsHoseConnected)
            {
                Logger.Information($"Cancel GSX Refuel Service after Aircraft has finished Refueling");
                await GsxController.ServiceRefuel.Cancel();
            }
        }

        protected virtual async Task OnBoardStateChanged(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return;

            if (service.State == GsxServiceState.Requested && !IsBoarding)
            {
                PaxProgress = 0;
                CargoApplyProgress = 0;
                IsBoarding = true;
                await Aircraft.BoardRequested(Flightplan.CountPax, Flightplan.WeightCargoKg);
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive)
                    _ = CargoDoorsOpenCloseDelayed(true);
                Logger.Debug($"Boarding is requested");
            }
            else if (service.State == GsxServiceState.Active)
            {
                await Aircraft.BoardActive(Flightplan.CountPax, Flightplan.WeightCargoKg);
                Logger.Information($"Boarding has started");
            }
            else if (IsBoarding && (service.IsCompleted || service.IsCompleting) && AutomationController.State <= AutomationState.Pushback)
            {
                IsBoarding = false;
                CargoApplyProgress = 100;
                CargoApplyActive = false;
                await Aircraft.BoardCompleted(Flightplan.CountPax, Flightplan.WeightPerPaxKg, Flightplan.WeightCargoKg);
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive && !SettingProfile.DoorsCargoKeepOpenOnDetachBoard)
                    _ = CargoDoorsOpenCloseDelayed(false);
                AutomationController.PayloadArrival = await Aircraft.GetPayload();
                Logger.Information($"Boarding is completed");
            }

            return;
        }

        protected virtual Task OnBoardPaxChanged(GsxServiceBoarding serviceBoarding)
        {
            if (!IsValidState || !IsValidGroundPhase || !serviceBoarding.IsRunning || (serviceBoarding.PaxTotal == 0 && PaxProgress > 0))
                return Task.CompletedTask;

            if (serviceBoarding.PaxTotal - PaxProgress >= 20)
            {
                Logger.Information($"Boarding in Progress. Pax boarded: {serviceBoarding.PaxTotal}");
                PaxProgress = serviceBoarding.PaxTotal;
            }
            return Aircraft.BoardChangePax(serviceBoarding.PaxTotal, Flightplan.WeightPerPaxKg, Flightplan.CountPax);
        }

        protected virtual Task OnBoardCargoChanged(GsxServiceBoarding serviceBoarding)
        {
            if (!IsValidState || !IsValidGroundPhase || !serviceBoarding.IsRunning || serviceBoarding.CargoPercent == 0)
                return Task.CompletedTask;

            _ = ApplyCargo(serviceBoarding.CargoPercent, 1);
            return Task.CompletedTask;
        }

        protected virtual async Task OnDeboardStateChanged(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase || service.State < GsxServiceState.Requested)
                return;

            if (service.State == GsxServiceState.Requested && !IsDeboarding)
            {
                PaxProgress = 0;
                CargoApplyProgress = 100;
                IsDeboarding = true;
                await Aircraft.DeboardRequested();
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive)
                    _ = CargoDoorsOpenCloseDelayed(true);
                Logger.Debug($"Deboarding is requested");
            }
            else if (service.State == GsxServiceState.Active)
            {
                await Aircraft.DeboardActive();
                Logger.Information($"Deboarding has started");
            }
            else if (IsDeboarding && (service.IsCompleted || service.IsCompleting) && AutomationController.State >= AutomationState.Arrival)
            {
                IsDeboarding = false;
                CargoApplyProgress = 0;
                CargoApplyActive = false;
                await Aircraft.DeboardCompleted();
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive && !SettingProfile.DoorsCargoKeepOpenOnDetachDeboard)
                    _ = CargoDoorsOpenCloseDelayed(false);
                Logger.Information($"Deboarding is completed");
            }

            return;
        }

        protected virtual Task OnDeboardPaxChanged(GsxServiceDeboarding serviceDeboarding)
        {
            if (!IsValidState || !IsValidGroundPhase || !serviceDeboarding.IsRunning || (serviceDeboarding.PaxTotal == 0 && PaxProgress > 0))
                return Task.CompletedTask;

            int paxOnBoard = AutomationController.PayloadArrival.CountPax - serviceDeboarding.PaxTotal;
            if (paxOnBoard < 0)
                paxOnBoard = 0;

            if (serviceDeboarding.PaxTotal - PaxProgress >= 20 || AutomationController.PayloadArrival.CountPax == serviceDeboarding.PaxTotal)
            {
                Logger.Information($"Deboarding in Progress. Pax deboarded: {serviceDeboarding.PaxTotal}");
                PaxProgress = serviceDeboarding.PaxTotal;
            }
            return Aircraft.DeboardChangePax(paxOnBoard, serviceDeboarding.PaxTotal, Flightplan.WeightPerPaxKg);
        }

        protected virtual Task OnDeboardCargoChanged(GsxServiceDeboarding serviceDeboarding)
        {
            if (!IsValidState || !IsValidGroundPhase || !serviceDeboarding.IsRunning || serviceDeboarding.CargoPercent == 0)
                return Task.CompletedTask;

            int percent = 100 - serviceDeboarding.CargoPercent;
            if (percent < 0)
                percent = 0;

            _ = ApplyCargo(percent, -1);
            return Task.CompletedTask;
        }

        protected virtual async Task ApplyCargo(int cargoPercent, int direction)
        {
            while (CargoApplyActive && (IsBoarding || IsDeboarding) && !RequestToken.IsCancellationRequested)
                await Task.Delay(Config.StateMachineInterval, RequestToken);

            if (!(IsBoarding || IsDeboarding) || RequestToken.IsCancellationRequested)
                return;
            else
                CargoApplyActive = true;

            try
            {
                double scalar;
                while (((direction == 1 && CargoApplyProgress < cargoPercent) || (direction == -1 && CargoApplyProgress > cargoPercent)) && IsExecutionAllowed)
                {
                    CargoApplyProgress += Config.CargoPercentChangePerSec * direction;
                    if (direction == 1 && CargoApplyProgress > 100)
                        CargoApplyProgress = 100;
                    else if (direction == -1 && CargoApplyProgress < 0)
                        CargoApplyProgress = 0;
                    scalar = CargoApplyProgress / 100.0;

                    if (direction == 1)
                        await Aircraft.BoardChangeCargo(CargoApplyProgress, Flightplan.WeightCargoKg * scalar, Flightplan.WeightCargoKg);
                    else if (direction == -1)
                        await Aircraft.DeboardChangeCargo(CargoApplyProgress, AutomationController.PayloadArrival.WeightCargoKg * scalar);

                    await Task.Delay(1000, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            CargoApplyActive = false;
        }

        public virtual async Task OnGpuConnectionChange(bool connected)
        {
            if (!IsValidState || !IsValidGroundPhase || await Aircraft.GetUseGpuGsx() == GsxGpuUsage.Never)
                return;

            if (connected)
                await Aircraft.SetExternalPowerAvailable(true);
            else
                await Aircraft.SetExternalPowerAvailable(false);
        }

        protected virtual Task OnLavatoryStateChange(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase || !SettingProfile.DoorPanelHandling)
                return Task.CompletedTask;

            if (service.State == GsxServiceState.Active)
                _ = TaskTools.RunDelayed(() => Aircraft.SetPanelLavatory(true), Config.PanelLavatoryOpenDelay * 1000, RequestToken);
            else
                return Aircraft.SetPanelLavatory(false);

            return Task.CompletedTask;
        }

        protected virtual Task OnWaterStateChange(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase || !SettingProfile.DoorPanelHandling)
                return Task.CompletedTask;

            if (service.State == GsxServiceState.Active)
                _ = TaskTools.RunDelayed(() => Aircraft.SetPanelWater(true), Config.PanelWaterOpenDelay * 1000, RequestToken);
            else
                return Aircraft.SetPanelWater(false);

            return Task.CompletedTask;
        }

        protected virtual Task OnFlightplanImport(IFlightplan ofp)
        {
            if (!IsValidState)
                return Task.CompletedTask;

            return Aircraft.OnFlightplanImport(ofp);
        }

        protected virtual Task OnFlightplanUnload()
        {
            if (!IsValidState)
                return Task.CompletedTask;

            return Aircraft.OnFlightplanUnload();
        }
    }
}
