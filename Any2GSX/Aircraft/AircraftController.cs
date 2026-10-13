using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.Plugins;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Generic;
using System.Linq;
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
        protected virtual bool IsLocked { get; set; } = false;
        protected virtual double FuelFobCounter { get; set; } = 0;
        protected virtual double FuelProgress { get; set; } = 0;
        protected virtual double FuelRate { get; set; } = 0;
        protected virtual bool FuelHoseWasConnected { get; set; } = false;
        protected virtual bool FuelWasPaused { get; set; } = false;
        protected virtual int PaxProgress { get; set; } = 0;
        protected virtual int CargoApplyProgress { get; set; } = 0;
        protected virtual bool CargoApplyActive { get; set; } = false;
        public virtual bool IsBoarding { get; protected set; } = false;
        public virtual bool IsDeboarding { get; protected set; } = false;
        public virtual string Title => AppService.Instance.AircraftTitle;
        public virtual bool IsConnected => Aircraft?.IsConnected == true;
        public virtual bool IsAircraftLoaded => Aircraft != null;
        protected virtual bool WasInSessionStart { get; set; } = false;
        public virtual string PluginId { get; protected set; } = "NULL";
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
            IsLocked = false;
            await Stop();
            await Task.Delay(1000, RequestToken);
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
            GsxController.ServiceStairs.OnVehicleChanged += OnStairVehicleChange;
            GsxController.ServiceCatering.OnVehicleChanged += OnCateringVehicleChange;
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

            GsxController.ApiClient.AircraftHandlerChanged += OnAircraftHandlerChanged;
        }

        protected virtual Task OnAircraftHandlerChanged()
        {
            if (Aircraft?.IsConnected == true)
                Aircraft?.CheckDoorVariables();

            return Task.CompletedTask;
        }

        protected override async Task DoRun()
        {
            try
            {
                if (Aircraft.InitDelay <= 0)
                    await Task.Delay(1000, RequestToken);
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

                await Aircraft.InitDoorMap();
                Aircraft.CheckDoorVariables();
                Logger.Debug($"GSX -> Aircraft Doors mapped.");

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
                FuelFobCounter = 0;
                FuelProgress = 0;
                FuelRate = 0;
                FuelHoseWasConnected = false;
                FuelWasPaused = false;
                PaxProgress = 0;
                CargoApplyProgress = 0;
                CargoApplyActive = false;
                IsBoarding = false;
                IsDeboarding = false;
                WasInSessionStart = false;
                IsLocked = false;
                PluginId = "NULL";

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
                GsxController.ServiceStairs.OnVehicleChanged -= OnStairVehicleChange;
                GsxController.ServiceCatering.OnVehicleChanged -= OnCateringVehicleChange;
                GsxController.ServicePushBack.OnStateChanged -= OnPushStateChange;
                GsxController.ServicePushBack.OnPushStatus -= OnPushOperationChange;
                GsxController.ServiceGpu.OnGpuConnection -= OnGpuConnectionChange;
                GsxController.ServiceLavatory.OnStateChanged -= OnLavatoryStateChange;
                GsxController.ServiceWater.OnStateChanged -= OnWaterStateChange;

                GsxController.WalkaroundPreAction -= BeforeWalkaroundSkip;
                GsxController.WalkaroundWasSkipped -= AfterWalkaroundSkip;

                Flightplan.OnImport -= OnFlightplanImport;
                Flightplan.OnUnload -= OnFlightplanUnload;
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
                double value = Config.GetFuelFob(Title, Aircraft.FuelCapacityKg, SettingProfile.FuelResetBaseKg, out bool saved);
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
            while (IsLocked)
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            IsLocked = true;

            if (state == AutomationState.SessionStart)
            {
                LoaderConnectPhase = AutomationState.Unknown;
                WasInSessionStart = true;
                if (GsxController.IsOnGround && await Aircraft.GetHasOpenDoors() && GsxController.GsxServices.All(kv => !kv.Value.IsRunning))
                    await Aircraft.DoorsAllClose();
            }
            else if (state == AutomationState.Preparation)
            {
                await SetInitialFuelPayload();
                await CheckPaxDoorConnections();
            }
            else if (state == AutomationState.Departure)
            {
                IsDeboarding = false;
                if (!WasInSessionStart && await Aircraft.GetHasOpenDoors() && GsxController.GsxServices.All(kv => !kv.Value.IsRunning))
                {
                    await Aircraft.DoorsAllClose();
                    await Task.Delay(2000, Token);
                }
                await CheckPaxDoorConnections();
            }
            else if (state == AutomationState.Arrival)
            {
                if (await Aircraft.GetHasFobSaveRestore() && SettingProfile.FuelSaveLoadFob)
                {
                    var fob = await Aircraft.GetFuelOnBoardKg();
                    Config.SetFuelFob(Title, fob);
                    Logger.Information($"Fuel saved for Aircraft '{Title}': {Math.Round(Config.ConvertKgToDisplayUnit(fob), 0)} {Config.DisplayUnitCurrentString}");
                }
            }
            else if (state == AutomationState.TaxiOut || state == AutomationState.Flight || state == AutomationState.TaxiIn)
            {
                IsBoarding = false;
                IsDeboarding = false;
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
            IsLocked = false;
        }

        protected virtual async Task OnCouatlStarted(IGsxController gsxController)
        {
            if (!IsValidState)
                return;

            await Aircraft.OnCouatlStarted();

            if (AutomationController.State == AutomationState.Departure || AutomationController.State == AutomationState.Pushback)
            {
                if (IsBoarding)
                {
                    IsBoarding = false;
                    await Aircraft.BoardCompleted(Flightplan.CountPax, Flightplan.WeightPerPaxKg, Flightplan.WeightCargoKg);
                    Logger.Information($"Boarding set to completed due to GSX Restart");
                }

                if (AutomationController.State == AutomationState.Pushback && (RefuelTimer.IsEnabled || (GsxController.ServiceRefuel.WasRunning && await Aircraft.GetFuelOnBoardKg() <= Flightplan.FuelRampKg - Config.FuelCompareVariance)))
                {
                    await RefuelStopEarly();
                    Logger.Information($"Aircraft Refueling finished early due to GSX Restart");
                }
            }

            if ((AutomationController.State == AutomationState.Arrival || AutomationController.State == AutomationState.TurnAround)
                && IsDeboarding)
            {
                IsDeboarding = false;
                await Aircraft.DeboardCompleted();
                Logger.Information($"Deboarding set to completed due to GSX Restart");
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

        protected virtual async Task OnDoorTrigger(GsxDoor gsxDoor, double value)
        {
            if (!IsValidState || !IsValidGroundPhase || (GsxController.ServiceStairs.InhibitStateUpdates && IGsxController.IsPaxDoor(gsxDoor)))
                return;

            AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
            Logger.Debug($"Received Trigger for Door {gsxDoor} ({aircraftDoor}): {value}");
            bool trigger = value > 0;
            bool allowed = IGsxController.IsServiceDoor(gsxDoor) && SettingProfile.DoorServiceHandling || IGsxController.IsCargoDoor(gsxDoor) && SettingProfile.DoorCargoHandling || IGsxController.IsPaxDoor(gsxDoor) && SettingProfile.DoorPaxHandling;
            bool handled = await Aircraft.OnDoorTrigger(aircraftDoor, trigger, allowed);
            if (trigger && allowed && !handled && await Aircraft.IsDoorCustom(gsxDoor))
            {
                bool doorState = await Aircraft.GetDoor(gsxDoor);
                if (Aircraft.GetHandlerExitPreferred() == gsxDoor && (GsxController.ServiceJetway.IsConnected || GsxController.ServiceCleaning.IsRunning))
                    return;
                else
                    await Aircraft.SetDoor(gsxDoor, !doorState);
            }
        }

        protected virtual async Task CargoDoorsOpenCloseDelayed(bool target)
        {
            try
            {
                Logger.Debug($"{(target ? "Open" : "Close")} Cargo Doors afer {SettingProfile.DoorCargoOpenCloseDelay}s");
                if (SettingProfile.DoorCargoOpenCloseDelay > 0)
                    await Task.Delay(SettingProfile.DoorCargoOpenCloseDelay * 1000, RequestToken);
                await Aircraft.SetCargoDoors(target);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual bool IsExitNotLoading(GsxDoor gsxDoor)
        {
            bool isSelfLoader = GsxController.SubsCargoTrainsEmpty[gsxDoor]?.GetNumber() == 1 && GsxController.SubsCargoTrainsFull[gsxDoor]?.GetNumber() == 1;
            return !isSelfLoader || (isSelfLoader && GsxController.ApiClient.GetSetting(GsxConstants.SettingMultiTripsTrains) == 0) || GsxController.ServiceBoard.CargoPercent == 100 || GsxController.ServiceDeboard.CargoPercent == 100 || GsxController.ServiceBoard.IsCompleted || GsxController.ServiceBoard.IsCompleting;
        }

        protected virtual async Task OnLoaderAttached(GsxDoor gsxDoor, double value)
        {
            if (!IsValidState || !IsValidGroundPhase || !IGsxController.IsCargoDoor(gsxDoor))
                return;


            Logger.Debug($"Received Loader attached for Door {gsxDoor}: {value}");
            bool state = value > 0;
            if (state && LoaderConnectPhase != AutomationController.State)
            {
                LoaderConnectPhase = AutomationController.State;
                Logger.Debug($"Loader connected in Phase {LoaderConnectPhase}");
            }

            if (!SettingProfile.DoorCargoHandling)
                return;

            AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
            if (state || (!state && !SettingProfile.DoorsCargoKeepOpenOnDetachBoard && !SettingProfile.DoorsCargoKeepOpenOnDetachDeboard && IsExitNotLoading(gsxDoor))
                || (!state && LoaderConnectPhase == AutomationState.Departure && !SettingProfile.DoorsCargoKeepOpenOnDetachBoard && IsExitNotLoading(gsxDoor))
                || (!state && LoaderConnectPhase == AutomationState.Arrival && !SettingProfile.DoorsCargoKeepOpenOnDetachDeboard && IsExitNotLoading(gsxDoor)))
            {
                bool handled = await Aircraft.OnLoaderAttached(aircraftDoor, state, IGsxController.IsCargoDoor(gsxDoor) && SettingProfile.DoorCargoHandling);
                if (!handled && await Aircraft.IsDoorCustom(gsxDoor) && await Aircraft.GetDoor(gsxDoor) != state)
                {
                    Logger.Debug($"Set state={state} OnLoaderAttached on Door {gsxDoor} ({aircraftDoor})");
                    await Aircraft.SetDoor(gsxDoor, state);
                }
            }
        }

        protected virtual async Task CheckPaxDoorConnections()
        {
            if (GsxController.HasGateJetway && GsxController.JetwayState == GsxServiceState.Active && SettingProfile.DoorPaxHandling)
            {
                var door = Aircraft.GetHandlerExitPreferred();
                if (!await Aircraft.GetDoor(door))
                {
                    Logger.Debug($"Setting Preferred Door {door} for Jetway");
                    await Aircraft.SetDoor(door, true);
                }
            }

            if (GsxController.HasGateStair && GsxController.ServiceStairs.IsAnyStair(s => s == GsxVehicleStairState.InPosition) && SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling)
            {
                foreach (var stair in Enum.GetValues<GsxVehicleLocation>())
                {
                    if (GsxController.ServiceStairs.GetStairState(stair) == GsxVehicleStairState.InPosition)
                    {
                        var door = GsxConstants.MapStairDoor(stair);
                        if (!await Aircraft.GetDoor(door))
                        {
                            Logger.Debug($"Setting Door {door} for Stair {stair}");
                            await Aircraft.SetDoor(door, true);
                        }
                    }
                }
            }
        }

        protected virtual async Task OnJetwayStateChanged(IGsxService service)
        {
            if (IsValidState && IsValidGroundPhase)
            {
                GsxDoor gsxDoor = Aircraft.GetHandlerExitPreferred();
                AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
                bool handled = await Aircraft.OnJetwayStateChange(aircraftDoor, service.State, SettingProfile.DoorPaxHandling);

                if (SettingProfile.DoorPaxHandling && !handled && service.IsActive)
                {
                    bool target = true;
                    if (await Aircraft.IsDoorCustom(gsxDoor) && target != await Aircraft.GetDoor(gsxDoor))
                    {
                        Logger.Debug($"Set target={target} OnJetwayStateChange IsActive on Door {gsxDoor} ({aircraftDoor})");
                        await Aircraft.SetDoor(gsxDoor, target);
                    }
                }
            }
        }

        protected virtual async Task OnJetwayOperationChange(GsxServiceState state)
        {
            if (IsValidState && IsValidGroundPhase)
            {
                GsxDoor gsxDoor = Aircraft.GetHandlerExitPreferred();
                AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
                bool handled = await Aircraft.OnJetwayOperationChange(aircraftDoor, state, SettingProfile.DoorPaxHandling);

                if (SettingProfile.DoorPaxHandling && !handled && state == GsxServiceState.Active)
                {
                    bool target = false;
                    if (await Aircraft.IsDoorCustom(gsxDoor) && target != await Aircraft.GetDoor(gsxDoor))
                    {
                        Logger.Debug($"Set target={target} OnJetwayOperationChange IsActive on Door {gsxDoor} ({aircraftDoor})");
                        await Aircraft.SetDoor(gsxDoor, target);
                    }
                }
            }
        }

        protected virtual Task OnStairStateChanged(IGsxService service)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
            {
                Logger.Debug($"Trigger OnStairStateChange {service.State}");
                return Aircraft.OnStairStateChange(service.State, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);
            }
            return Task.CompletedTask;
        }

        protected virtual Task OnStairOperationChange(GsxServiceState state)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
            {
                Logger.Debug($"Trigger OnStairOperationChange {state}");
                return Aircraft.OnStairOperationChange(state, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);
            }
            return Task.CompletedTask;
        }

        protected virtual async Task OnStairVehicleChange(GsxVehicleLocation stair, GsxVehicleStairState state)
        {
            if (IsValidState && IsValidGroundPhase && !GsxController.ServiceStairs.IsStateOverridden)
            {
                GsxDoor gsxDoor = GsxConstants.MapStairDoor(stair);
                AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
                bool handled = await Aircraft.OnStairVehicleChange(aircraftDoor, stair, state, SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling);

                if (SettingProfile.DoorPaxHandling && SettingProfile.DoorStairHandling && !handled && (!GsxController.HasGateJetway || stair == GsxVehicleLocation.Rear))
                {


                    bool target = false;
                    if (state == GsxVehicleStairState.InPosition)
                        target = true;
                    if (await Aircraft.IsDoorCustom(gsxDoor) && target != await Aircraft.GetDoor(gsxDoor))
                    {
                        Logger.Debug($"Set target={target} OnStairVehicleChange {state} on Door {gsxDoor} ({aircraftDoor}) for Stair {stair}");
                        await Aircraft.SetDoor(gsxDoor, target);
                    }
                }
            }
        }

        protected virtual async Task OnCateringVehicleChange(GsxVehicleLocation truck, GsxVehicleCateringState state)
        {
            if (IsValidState && IsValidGroundPhase)
            {
                GsxDoor gsxDoor = GsxConstants.MapCateringDoor(truck);
                AircraftDoor aircraftDoor = Aircraft.GsxDoorMapping[gsxDoor];
                bool handled = await Aircraft.OnCateringVehicleChange(aircraftDoor, truck, state, SettingProfile.DoorServiceHandling);

                if (SettingProfile.DoorServiceHandling && !handled)
                {
                    bool target = false;
                    if (state == GsxVehicleCateringState.Completing || state == GsxVehicleCateringState.InPosition)
                        target = true;

                    if (await Aircraft.IsDoorCustom(gsxDoor) && target != await Aircraft.GetDoor(gsxDoor))
                    {
                        Logger.Debug($"Set target={target} OnCateringVehicleChange {state} on Door {gsxDoor} ({aircraftDoor}) for Truck {truck}");
                        await Aircraft.SetDoor(gsxDoor, target);
                    }
                }
            }
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
            if (service.IsActive && !GsxController.ServiceRefuel.IsHoseConnected)
            {
                await Aircraft.RefuelActive();
                if (SettingProfile.DoorPanelHandling)
                {
                    _ = TaskTools.RunDelayed(() => Aircraft.SetPanelRefuelOperator(true), Config.PanelRefuelOperatorOpenDelay * 1000, RequestToken);
                    int delay = Config.PanelRefuelOpenDelayUnderground;
                    if (!GsxController.ServiceRefuel.IsUnderground)
                        delay = Config.PanelRefuelOpenDelayTanker;
                    _ = TaskTools.RunDelayed(() => Aircraft.SetPanelRefuel(true), delay * 1000, RequestToken);
                }
            }
            else if (service.IsCompleted && AutomationController.State <= AutomationState.Pushback)
            {
                if (hasSync && (RefuelTimer.IsEnabled || FuelWasPaused || FuelHoseWasConnected) && GsxController.IsGsxRunning && !GsxController.ServicePushBack.IsRunning
                    && fob <= Flightplan.FuelRampKg - Config.FuelCompareVariance)
                {
                    Logger.Warning($"Instant load Fuel - FOB did not match planned after GSX Refuel");
                    Logger.Debug($"FOB: {fob} | PlanRamp: {Flightplan.FuelRampKg}");
                    await RefuelStopEarly();
                }
                await Aircraft.RefuelCompleted();
            }
            else if (service.State <= GsxServiceState.Bypassed)
            {
                FuelHoseWasConnected = false;
                FuelWasPaused = false;
            }
            else if (service.IsRequested)
            {
                if (GsxController.HasRefuelDialog)
                    _ = TaskTools.RunDelayed(() => GsxController.Menu.BlockMenuUpdates(true, "Fuel Dialog"), GsxController.Config.MenuOpenTimeout, GsxController.RequestToken);
                else if (service.HasStairConflict)
                    _ = TaskTools.RunDelayed(() => GsxController.Menu.BlockMenuUpdates(true, "Stair Question"), GsxController.Config.MenuOpenTimeout, GsxController.RequestToken);
            }

            if (!service.IsRequested && !service.IsActive && GsxController.Tracker.IsActive(AppNotification.UpdatesBlocked))
                GsxController.Menu.BlockMenuUpdates(false);

            return;
        }

        protected virtual async Task RefuelStopEarly()
        {
            await Task.Delay(250);
            RefuelTimerStop();
            FuelFobCounter = Flightplan.FuelRampKg;
            FuelProgress = Flightplan.FuelRampKg;
            FuelWasPaused = false;
            await Aircraft.RefuelStop(FuelFobCounter, true);
            if (SettingProfile.DoorPanelHandling)
            {
                await Aircraft.SetPanelRefuel(false);
                await Aircraft.SetPanelRefuelOperator(false);
                await Aircraft.OnRefuelHoseConnection(false);
            }

            await Task.Delay(500);
            Logger.Debug($"Stop Refuel - Hose disconnected early: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {FuelRate} - FOB {await Aircraft.GetFuelOnBoardKg()}");
        }

        public virtual async Task OnRefuelHoseChanged(bool connected)
        {
            if (!IsValidState || !IsValidGroundPhase)
                return;
            bool multiTrips = GsxController.ApiClient.GetSetting(GsxConstants.SettingMultiTrips) != 0;

            if (connected && GsxController.ServiceRefuel.IsRunning)
            {
                bool isFuelInc = await SetRefuelRate();
                if (!FuelHoseWasConnected)
                    await Aircraft.RefuelStart(Flightplan.FuelRampKg);

                if (await Aircraft.GetHasFuelSync() && !RefuelTimer.IsEnabled)
                {
                    RefuelTimer.Interval = TimeSpan.FromMilliseconds(Aircraft.RefuelIntervalMs);
                    FuelProgress = 0;
                    RefuelTimer.Start();
                    Logger.Debug($"Aircraft Refuel Timer started (Interval {Aircraft.RefuelIntervalMs}ms)");
                    Logger.Information($"Aircraft {(isFuelInc ? "Refueling" : "Defueling")} {(FuelHoseWasConnected ? "resumed" : "started")}. Refuel Rate: {(isFuelInc ? "" : "-")}{Math.Round(Config.ConvertKgToDisplayUnit(FuelRate), 1)}{Config.DisplayUnitCurrentString}/s{(SettingProfile.UseRefuelTimeTarget ? $" (Time Target {SettingProfile.RefuelTimeTargetSeconds}s)" : "")}");
                }
                FuelHoseWasConnected = true;
            }
            else if (!connected && RefuelTimer.IsEnabled && GsxController.IsGsxRunning)
            {
                if (((!multiTrips || GsxController.HasUndergroundRefuel) && SettingProfile.RefuelFinishOnHose) || GsxController.ServiceRefuel.IsCompleting)
                {
                    await RefuelStopEarly();
                    Logger.Information($"Aircraft Refueling finished early (Hose disconnected). FOB: {Math.Round(Config.ConvertKgToDisplayUnit(await Aircraft.GetFuelOnBoardKg()), 2)}{Config.DisplayUnitCurrentString}");
                }
                else if (multiTrips && !GsxController.HasUndergroundRefuel)
                {
                    RefuelTimerStop();
                    FuelProgress = 0;
                    Logger.Information($"Aircraft Refueling paused - waiting for next Truck");
                }
            }

            await Aircraft.OnRefuelHoseConnection(connected);
            if (!connected && SettingProfile.DoorPanelHandling && (await Aircraft.GetFuelOnBoardKg() >= Flightplan.FuelRampKg - Config.FuelCompareVariance || !multiTrips)
                && (GsxController.AutomationState == AutomationState.Departure || GsxController.AutomationState == AutomationState.Pushback || GsxController.AutomationState == AutomationState.Arrival))
            {
                _ = TaskTools.RunDelayed(() => Aircraft.SetPanelRefuelOperator(false), Config.PanelRefuelOperatorCloseDelay * 1000, RequestToken);
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
            if (SettingProfile.UseRefuelTimeTarget && !FuelHoseWasConnected)
            {
                FuelRate = Math.Abs(Flightplan.FuelRampKg - FuelFobCounter) / SettingProfile.RefuelTimeTargetSeconds;
                if (FuelRate < Config.RefuelMinimumRate)
                    FuelRate = Config.RefuelMinimumRate;
            }
            else
                FuelRate = SettingProfile.RefuelRateKgSec;


            if (Config.GsxAllowSettingManipulation)
            {
                double gsxRate = Math.Round((FuelRate / GsxConstants.ConstantFuelRate) + 0.5, 0);
                gsxRate = (int)Math.Max(Math.Min(gsxRate, 10), 1);
                Logger.Debug($"Setting GSX Refuel Time Accelaration to x{(int)gsxRate}");
                await GsxController.ApiClient.SetSetting(GsxConstants.SettingRefuelTimeAccel, (int)gsxRate);
            }

            return isFuelInc;
        }

        protected virtual ulong TickCounter { get; set; } = 0;

        protected virtual void RefuelTimerStop()
        {
            RefuelTimer?.Stop();
            TickCounter = 0;
        }

        protected virtual async Task OnRefuelTick()
        {
            try
            {
                if (Token.IsCancellationRequested || Aircraft == null || Config == null)
                {
                    RefuelTimerStop();
                    return;
                }

                if (!SettingProfile.UseRefuelTimeTarget)
                    FuelRate = SettingProfile.RefuelRateKgSec;

                double rate = FuelRate;
                if (TickCounter < 15)
                    rate = 28;

                double fob = await Aircraft.GetFuelOnBoardKg();
                bool isFuelInc = fob <= Flightplan.FuelRampKg;

                FuelFobCounter += rate * (isFuelInc ? 1 : -1);
                FuelProgress += rate;

                if (isFuelInc && FuelFobCounter > Flightplan.FuelRampKg)
                    FuelFobCounter = Flightplan.FuelRampKg;
                else if (!isFuelInc && FuelFobCounter < Flightplan.FuelRampKg)
                    FuelFobCounter = Flightplan.FuelRampKg;

                if (Math.Abs(fob - Flightplan.FuelRampKg) <= Config.FuelCompareVariance)
                {
                    Logger.Debug($"Last-Tick: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {rate}");
                    await Aircraft.RefuelTick(isFuelInc, rate, Flightplan.FuelRampKg, Flightplan.FuelRampKg);
                    RefuelTimerStop();
                    Logger.Debug($"Aircraft Refuel Timer ended");
                    await Aircraft.RefuelStop(Flightplan.FuelRampKg, false);
                    await Task.Delay(200);
                    fob = await Aircraft.GetFuelOnBoardKg();
                    Logger.Information($"Aircraft Refueling finished. FOB: {Math.Round(Config.ConvertKgToDisplayUnit(fob), 2)}{Config.DisplayUnitCurrentString}");
                    _ = TaskTools.RunDelayed(CancelGsxRefuel, Config.RefuelDisconnectTimeout, RequestToken);
                }
                else
                {
                    Logger.Verbose($"Refuel-Tick: {FuelFobCounter} / {Flightplan.FuelRampKg} @ {rate} - FOB {fob}");
                    await Aircraft.RefuelTick(isFuelInc, rate, FuelFobCounter, Flightplan.FuelRampKg);
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
            TickCounter++;
        }

        protected virtual async Task CancelGsxRefuel()
        {
            if (GsxController.ServiceRefuel.IsHoseConnected)
            {
                Logger.Information($"Cancel GSX Refuel Service after Aircraft has finished Refueling");
                await GsxController.ServiceRefuel.Cancel();
                await Task.Delay(Config.RefuelDisconnectTimeout);
                if (GsxController.ServiceRefuel.IsHoseConnected)
                {
                    Logger.Debug("Reschedule cancellation");
                    _ = TaskTools.RunDelayed(CancelGsxRefuel, Config.RefuelDisconnectTimeout, RequestToken);
                }
                else
                    Logger.Debug("Refuel cancelled successfully");
            }
        }

        protected virtual async Task OnBoardStateChanged(IGsxService service)
        {
            Logger.Debug($"OnBoardStateChanged - IsValidState {IsValidState} | IsValidGroundPhase {IsValidGroundPhase} | State {service.State} | IsBoarding {IsBoarding}");
            if (!IsValidState || !IsValidGroundPhase)
                return;

            if (service.IsRequested && !IsBoarding)
            {
                PaxProgress = 0;
                CargoApplyProgress = 0;
                IsBoarding = true;
                await Aircraft.BoardRequested(Flightplan.CountPax, Flightplan.WeightCargoKg);
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive)
                    _ = CargoDoorsOpenCloseDelayed(true);
                Logger.Debug($"Boarding is requested");
            }
            else if (service.IsActive)
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

            if (service.IsRequested && !IsDeboarding)
            {
                PaxProgress = 0;
                CargoApplyProgress = 100;
                IsDeboarding = true;
                await Aircraft.DeboardRequested();
                if (SettingProfile.DoorCargoHandling && SettingProfile.DoorCargoOpenOnActive)
                    _ = CargoDoorsOpenCloseDelayed(true);
                Logger.Debug($"Deboarding is requested");
            }
            else if (service.IsActive)
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

            if (service.IsActive)
                _ = TaskTools.RunDelayed(() =>
                {
                    Logger.Debug($"Set Panel Lavatory: {true}");
                    Aircraft.SetPanelLavatory(true);
                }, Config.PanelLavatoryOpenDelay * 1000, RequestToken);
            else
            {
                Logger.Debug($"Set Panel Lavatory: {false}");
                return Aircraft.SetPanelLavatory(false);
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnWaterStateChange(IGsxService service)
        {
            if (!IsValidState || !IsValidGroundPhase || !SettingProfile.DoorPanelHandling)
                return Task.CompletedTask;

            if (service.IsActive)
                _ = TaskTools.RunDelayed(() =>
                {
                    Logger.Debug($"Set Panel Water: {true}");
                    Aircraft.SetPanelWater(true);
                }, Config.PanelWaterOpenDelay * 1000, RequestToken);
            else
            {
                Logger.Debug($"Set Panel Water: {false}");
                return Aircraft.SetPanelWater(false);
            }

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
