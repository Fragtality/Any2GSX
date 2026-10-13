using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.API;
using Any2GSX.GSX.Automation;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.PluginInterface.Interfaces.GSX.API;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using Any2GSX.Tools;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX
{
    public class GsxController : ServiceController<Any2GSX, AppService, Config, Definition>, IGsxController
    {
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual SimConnectController SimController => AppService.Instance?.SimService?.Controller;
        public virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController?.Aircraft;
        public virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        public virtual bool IsMsfs2024 => SimConnect.GetSimVersion() == SimVersion.MSFS2024;
        public virtual string PathInstallation { get; } = Sys.GetRegistryValue<string>(GsxConstants.RegPath, GsxConstants.RegValue, null) ?? GsxConstants.PathDefault;
        public virtual GsxApiClient ApiClient { get; } = new();
        public virtual IGsxApiClient IApiClient => ApiClient;
        public virtual GsxApiData ApiData => ApiClient.ApiData;
        public virtual GsxMenu Menu { get; } = new();
        public virtual IGsxMenu IMenu => Menu;
        protected virtual DateTime NextMenuStartupCheck { get; set; } = DateTime.MinValue;
        public virtual IConfig IConfig => Config;
        public virtual GsxAutomationController AutomationController { get; } = new();
        public virtual IGsxAutomationController IAutomationController => AutomationController as IGsxAutomationController;
        public virtual NotificationTracker Tracker => AppService.Instance.NotificationTracker;
        public virtual AutomationState AutomationState => AutomationController.State;
        public event Func<IGsxController, Task> OnCouatlStarted;
        public event Func<IGsxController, Task> OnCouatlStopped;
        public virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices { get; } = [];
        public virtual GsxServiceReposition ServiceReposition => GsxServices[GsxServiceType.Reposition] as GsxServiceReposition;
        public virtual GsxServiceRefuel ServiceRefuel => GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel;
        public virtual GsxServiceCatering ServiceCatering => GsxServices[GsxServiceType.Catering] as GsxServiceCatering;
        public virtual GsxServiceJetway ServiceJetway => GsxServices[GsxServiceType.Jetway] as GsxServiceJetway;
        public virtual GsxServiceStairs ServiceStairs => GsxServices[GsxServiceType.Stairs] as GsxServiceStairs;
        public virtual GsxServicePushback ServicePushBack => GsxServices[GsxServiceType.Pushback] as GsxServicePushback;
        public virtual GsxServiceBoarding ServiceBoard => GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
        public virtual GsxServiceDeboarding ServiceDeboard => GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding;
        public virtual GsxServiceDeice ServiceDeice => GsxServices[GsxServiceType.Deice] as GsxServiceDeice;
        public virtual GsxServiceGpu ServiceGpu => GsxServices[GsxServiceType.GPU] as GsxServiceGpu;
        public virtual GsxServiceCleaning ServiceCleaning => GsxServices[GsxServiceType.Cleaning] as GsxServiceCleaning;
        public virtual GsxServiceLavatory ServiceLavatory => GsxServices[GsxServiceType.Lavatory] as GsxServiceLavatory;
        public virtual GsxServiceWater ServiceWater => GsxServices[GsxServiceType.Water] as GsxServiceWater;
        public virtual GsxServiceState JetwayState => ServiceJetway.State;
        public virtual GsxServiceState JetwayOperation => ServiceJetway.OperatingState;
        public virtual GsxServiceState StairsState => ServiceStairs.State;
        public virtual GsxServiceState StairsOperation => ServiceStairs.OperatingState;
        public virtual bool IsStairConnected => ServiceStairs.IsConnected;
        public virtual bool IsGateConnected => ServiceJetway.IsConnected || ServiceStairs.IsConnected;
        public virtual bool HasGateJetway => ServiceJetway.IsAvailable;
        public virtual bool HasGateStair => ServiceStairs.IsAvailable;
        public virtual bool HasUndergroundRefuel => ServiceRefuel?.IsUnderground == true;
        public virtual bool HasRefuelDialog => ApiClient.IsReady && (ApiData?.HandlerData?.aircraft?.refueling ?? false);
        public virtual bool IsRefuelOnStairSide => ApiClient.IsReady && (ApiData?.HandlerData?.aircraft?.IsRefuelOnStairSide ?? false);
        public virtual bool IsWaterOnStairSide => ApiClient.IsReady && (ApiData?.HandlerData?.aircraft?.IsWaterOnStairSide ?? false);
        public virtual bool IsLavatoryOnStairSide => ApiClient.IsReady && (ApiData?.HandlerData?.aircraft?.IsLavatorOnStairSide ?? false);
        public virtual bool GateServicesValid => ApiClient.IsReady && !string.IsNullOrWhiteSpace(ApiData?.HandlerData?.gate?.uiGateName);
        public virtual bool GsxHandlerValid => ApiClient.IsReady && ApiData?.HandlerData?.airport != null;
        public virtual bool IsDeiceAvail => SubDeiceAvail?.GetNumber() != 0;
        public virtual bool IsRefuelActive => GsxServices[GsxServiceType.Refuel].IsActive;
        public virtual bool IsFuelHoseConnected => (GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel)?.IsHoseConnected ?? false;

        public virtual bool LastReadyState { get; protected set; } = false;
        public virtual int CouatlInvalidCount { get; protected set; } = 0;
        public virtual bool IsProcessRunning { get; protected set; } = false;
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsGsxRunning => IsProcessRunning && ApiClient.IsReady;
        public virtual bool SimGroundState => SimStore["SIM ON GROUND"]?.GetNumber() == 1;
        public virtual bool IsOnGround { get; protected set; } = true;
        public virtual bool FirstGroundCheck { get; protected set; } = true;
        public virtual bool IsAirStart { get; protected set; } = false;
        public virtual bool CanAutomationRun => AircraftController.IsConnected && (Menu.FirstReadyReceived || IsAirStart || AircraftController.GetEngineRunning().GetAwaiter().GetResult());
        protected virtual int GroundCounter { get; set; } = 0;
        public virtual bool IsPaused => SimConnect.IsPaused;
        public virtual bool IsWalkaround => CheckWalkAround();
        public virtual bool SkippedWalkAround { get; protected set; } = false;
        public virtual bool WalkAroundSkipActive { get; protected set; } = false;
        public virtual bool WalkaroundPreActionNotified { get; protected set; } = false;
        public virtual bool WalkaroundNotified { get; protected set; } = false;
        public virtual string AutoSelectionPushback { get; set; } = "";
        public virtual bool HasSelectionPushback => !string.IsNullOrWhiteSpace(AutoSelectionPushback);
        public virtual JsonNode AutoSelectionArrivalGate { get; set; } = null;
        public virtual bool HasSelectionArrivalGate => !string.IsNullOrWhiteSpace(AutoSelectionArrivalGate?.ToString());

        public event Func<Task> WalkaroundPreAction;
        public event Func<Task> WalkaroundWasSkipped;
        public event Func<bool, Task> RepositionSignal;
        public event Func<string, Task> OnPushDirectionChanged;
        public event Func<string, Task> OnArrivalGateChanged;

        public virtual ISimResourceSubscription SubDoorToggleExit1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit3 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit4 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleService1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleService2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo3 { get; protected set; }
        public virtual ISimResourceSubscription SubLoaderAttachCargo1 { get; protected set; }
        public virtual ISimResourceSubscription SubLoaderAttachCargo2 { get; protected set; }
        public virtual ISimResourceSubscription SubLoaderAttachCargo3 { get; protected set; }
        public virtual ConcurrentDictionary<GsxDoor, ISimResourceSubscription> SubsCargoTrainsEmpty { get; } = [];
        public virtual ConcurrentDictionary<GsxDoor, ISimResourceSubscription> SubsCargoTrainsFull { get; } = [];
        public virtual ISimResourceSubscription SubDefaultSmartButton { get; protected set; }
        public virtual ISimResourceSubscription SubPilotTarget { get; protected set; }
        public virtual ISimResourceSubscription SubCrewTarget { get; protected set; }
        public virtual ISimResourceSubscription SubDeiceAvail { get; protected set; }

        public GsxController(Config config) : base(config)
        {
            GsxServices.Add(GsxServiceType.Reposition, new GsxServiceReposition());
            GsxServices.Add(GsxServiceType.Refuel, new GsxServiceRefuel());
            GsxServices.Add(GsxServiceType.Catering, new GsxServiceCatering());
            GsxServices.Add(GsxServiceType.Jetway, new GsxServiceJetway());
            GsxServices.Add(GsxServiceType.Stairs, new GsxServiceStairs());
            GsxServices.Add(GsxServiceType.Boarding, new GsxServiceBoarding());
            GsxServices.Add(GsxServiceType.Deboarding, new GsxServiceDeboarding());
            GsxServices.Add(GsxServiceType.Pushback, new GsxServicePushback());
            GsxServices.Add(GsxServiceType.GPU, new GsxServiceGpu());
            GsxServices.Add(GsxServiceType.Deice, new GsxServiceDeice());
            GsxServices.Add(GsxServiceType.Lavatory, new GsxServiceLavatory());
            GsxServices.Add(GsxServiceType.Water, new GsxServiceWater());
            GsxServices.Add(GsxServiceType.Cleaning, new GsxServiceCleaning());
        }

        public virtual IGsxService GetService(GsxServiceType type)
        {
            if (GsxServices.TryGetValue(type, out var gsxService))
                return gsxService;
            else
                return null;
        }

        public virtual bool TryGetService(GsxServiceType type, out IGsxService gsxService)
        {
            gsxService = null;
            if (GsxServices.TryGetValue(type, out GsxService svc))
            {
                gsxService = svc;
                return true;
            }
            else
                return false;
        }

        protected override Task DoInit()
        {
            SimStore.AddVariable("SIM ON GROUND", SimUnitType.Bool);
            SimStore.AddVariable("ABSOLUTE TIME", SimUnitType.Seconds);
            if (IsMsfs2024)
            {
                SimStore.AddVariable("IS AIRCRAFT", SimUnitType.Number);
                SimStore.AddVariable("IS AVATAR", SimUnitType.Number);
            }

            SimStore.AddVariable(GsxConstants.VarSetRemoteServer);
            SubDoorToggleExit1 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit1);
            SubDoorToggleExit2 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit2);
            SubDoorToggleExit3 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit3);
            SubDoorToggleExit4 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit4);
            SubDoorToggleService1 = SimStore.AddVariable(GsxConstants.VarDoorToggleService1);
            SubDoorToggleService2 = SimStore.AddVariable(GsxConstants.VarDoorToggleService2);
            SubDoorToggleCargo1 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo1);
            SubDoorToggleCargo2 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo2);
            SubDoorToggleCargo3 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo3);
            SubLoaderAttachCargo1 = SimStore.AddVariable(GsxConstants.VarCargoLoader1);
            SubLoaderAttachCargo2 = SimStore.AddVariable(GsxConstants.VarCargoLoader2);
            SubLoaderAttachCargo3 = SimStore.AddVariable(GsxConstants.VarCargoLoader3);
            SubsCargoTrainsEmpty.Add(GsxDoor.CargoDoor1, SimStore.AddVariable(GsxConstants.VarCargoTrainEmptyFront));
            SubsCargoTrainsEmpty.Add(GsxDoor.CargoDoor2, SimStore.AddVariable(GsxConstants.VarCargoTrainEmptyRear));
            SubsCargoTrainsEmpty.Add(GsxDoor.CargoDoor3Main, SimStore.AddVariable(GsxConstants.VarCargoTrainEmptyMain));
            SubsCargoTrainsFull.Add(GsxDoor.CargoDoor1, SimStore.AddVariable(GsxConstants.VarCargoTrainFullFront));
            SubsCargoTrainsFull.Add(GsxDoor.CargoDoor2, SimStore.AddVariable(GsxConstants.VarCargoTrainFullRear));
            SubsCargoTrainsFull.Add(GsxDoor.CargoDoor3Main, SimStore.AddVariable(GsxConstants.VarCargoTrainFullMain));

            SubPilotTarget = SimStore.AddVariable(GsxConstants.VarPilotTarget);
            SubCrewTarget = SimStore.AddVariable(GsxConstants.VarCrewTarget);

            SubDeiceAvail = SimStore.AddVariable(GsxConstants.VarDeiceAvail);

            SubDefaultSmartButton = SimStore.AddVariable(GenericSettings.VarSmartButtonDefault);

            SimStore.AddVariable(GsxConstants.VarSetAutoDoors);
            SimStore.AddVariable(GsxConstants.VarSetAutoFuel);
            SimStore.AddVariable(GsxConstants.VarSetAutoPayload);
            SimStore.AddVariable(GsxConstants.VarSetAutoEquip);
            SimStore.AddVariable(GsxConstants.VarSetDoorMsg);

            foreach (var service in GsxServices)
                service.Value.InitSubscriptions();
            Menu.Init();
            AutomationController.Init();

            ServiceReposition.OnRepositionSignal += OnRepositionSignal;

            ApiClient.ReadyStateChanged += OnReadyStateChange;

            return Task.CompletedTask;
        }

        protected virtual Task OnRepositionSignal(bool state)
        {
            _ = TaskTools.RunPool(() => RepositionSignal?.Invoke(state));

            return Task.CompletedTask;
        }

        public virtual DateTime GetTime()
        {
            try
            {
                if (Profile?.UseSimTime == true)
                {
                    double simAbsTime = SimStore["ABSOLUTE TIME"]?.GetNumber() ?? 0;
                    double epochOffset = (new DateTime(1970, 1, 1) - DateTime.MinValue).TotalSeconds;
                    simAbsTime -= epochOffset;
                    return DateTimeOffset.FromUnixTimeSeconds((long)simAbsTime).DateTime;
                }
                else
                    return DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return DateTime.UtcNow;
        }

        protected virtual void LogHandlerData()
        {
            try
            {
                if (ApiData?.HandlerData?.aircraft != null)
                {
                    Logger.Debug($"GSX Aircraft Profile: '{ApiData?.HandlerData?.aircraft?.dataSourceUsed ?? ""}' (dbKey: {ApiData?.HandlerData?.aircraft?.aircraftDbKey ?? ""})");
                    foreach (var exitGroup in ApiData?.HandlerData?.aircraft?.exits)
                    {
                        int index = 0;
                        foreach (var exit in ApiData?.HandlerData?.aircraft?.exits[exitGroup.Key ?? "0"])
                        {
                            Logger.Debug($"Exits: Group {exitGroup.Key ?? ""} | Index {index} | NoDoor {exit == null || exit?.HasDoor == false} | NoCheck {exit?.IsTypeNoCheck ?? false} | Standard {exit?.IsTypeStandard ?? false} | Custom {exit?.IsTypeCustom ?? false}");
                            index++;
                        }
                    }
                }

                if (ApiData?.HandlerData?.airport != null)
                {
                    Logger.Debug($"GSX Airport Profile for {ApiData?.HandlerData?.airport?.icao}: '{ApiData?.HandlerData?.airport?.USER_CUSTOMIZATION_DATASOURCE_NAME}' [ {string.Join(" ", ApiData?.HandlerData?.airport?.dataSourcesAvailable ?? [])} ]");
                }
            }
            catch
            {
                Logger.Debug($"Error while logging Aircraft Data");
            }
        }

        protected virtual Task OnReadyStateChange(bool state)
        {
            if (!IsActive)
                return Task.CompletedTask;

            if (!LastReadyState && state)
            {
                LastReadyState = state;
                LogHandlerData();
                Logger.Information($"GSX State changed to READY");
                CouatlInvalidCount = 0;
                return TaskTools.RunPool(() => OnCouatlStarted?.Invoke(this), RequestToken);
            }
            else if (LastReadyState && !state)
            {
                LastReadyState = state;
                Logger.Warning($"GSX State changed to NOT Ready");
                Menu.ResetNotRunning();
                App.AppService.SetGsxStartTime();
                return TaskTools.RunPool(() => OnCouatlStopped?.Invoke(this), RequestToken);
            }

            return Task.CompletedTask;
        }

        protected virtual void CheckGround()
        {
            if (FirstGroundCheck)
            {
                IsOnGround = SimGroundState;
                FirstGroundCheck = false;
                IsAirStart = !IsOnGround;
                if (IsAirStart)
                    Logger.Debug($"Air Start detected");
            }
            else if (SimGroundState != IsOnGround && !IsWalkaround)
            {
                GroundCounter++;
                if (GroundCounter > Config.GroundTicks)
                {
                    GroundCounter = 0;
                    IsOnGround = SimGroundState;
                    IsAirStart = false;
                    Logger.Information($"On Ground State changed: {(IsOnGround ? "On Ground" : "In Flight")}");
                }
            }
            else if (SimGroundState == IsOnGround && GroundCounter > 0)
                GroundCounter = 0;
        }

        protected override async Task DoRun()
        {
            try
            {
                ApiClient.Start();
                Menu.Reset();
                CheckProcess();
                await Task.Delay(1000, RequestToken);

                if (IsMsfs2024 && SimConnect.CameraState == 30)
                {
                    Logger.Debug($"Checking MSFS 2024 Aircraft/Avatar Vars ...");
                    int count = 0;
                    while (((SimStore["IS AIRCRAFT"]?.GetNumber() == 0 && SimStore["IS AVATAR"]?.GetNumber() == 0)
                        || (SimStore["IS AIRCRAFT"]?.GetNumber() == 1 && SimStore["IS AVATAR"]?.GetNumber() == 1))
                        && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                    {
                        await Task.Delay(Config.TimerGsxCheck, RequestToken);
                        count++;
                    }

                    if (count > 0)
                        await Task.Delay(Config.GsxServiceStartDelay / 2, RequestToken);
                    Logger.Debug($"MSFS 2024 Aircraft/Avatar Vars valid");
                }
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                bool external = AutomationController.DepartureQueue.ExternalServiceControl;
                await AutomationController.Reset();
                if (external)
                    AutomationController.DepartureQueue.SetExternalControl(external, AutomationController.DepartureQueue.ExternalServiceHandler);

                IsActive = true;
                Logger.Debug($"Wait up to {Config.GsxServiceStartDelay}ms for Aircraft Plugin Connection");
                int delay = 0;
                while (delay <= Config.GsxServiceStartDelay && AircraftController?.IsConnected == false && !RequestToken.IsCancellationRequested)
                {
                    delay += Config.StateMachineInterval;
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                Logger.Debug($"Continue after {delay}ms");
                await AppService.Instance.CommBus.PingModule();
                NextMenuStartupCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxStartupMenuCheck);

                await Menu.RunCommand(GsxMenuCommand.Disable(), false);
                Logger.Debug($"GsxService active (Process: {IsProcessRunning} | IsReady: {ApiClient.IsReady} | FirstReady: {Menu.FirstReadyReceived})");
                Logger.Information($"GsxController active - GSX Ready: {IsGsxRunning} | GSX Menu: {Menu.FirstReadyReceived} | Aircraft Ready: {AircraftController.IsConnected} | Walkaround: {IsWalkaround}");
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    if (FirstGroundCheck && ApiClient.IsReady)
                        await OnReadyStateChange(ApiClient.IsReady);
                    CheckGround();

                    //Walkaround handling
                    if (!SkippedWalkAround && Profile.SkipWalkAround && !WalkAroundSkipActive)
                    {
                        if (IsOnGround)
                            _ = SkipWalkaround();
                        else
                            SkippedWalkAround = true;
                    }

                    if (!Profile.SkipWalkAround && !SkippedWalkAround)
                    {
                        if (IsOnGround && !await AircraftController.GetEngineRunning() && AutomationController.State == AutomationState.SessionStart)
                            await HandleWalkaroundEquipment();

                        SkippedWalkAround = CheckSessionReady() && !IsWalkaround;
                    }

                    if (AutomationController.IsStarted && SkippedWalkAround && !WalkaroundNotified)
                    {
                        if (IsOnGround && AutomationController.State < AutomationState.Departure && WalkaroundWasSkipped != null)
                            _ = TaskTools.RunPool(() => WalkaroundWasSkipped?.Invoke(), RequestToken);
                        WalkaroundNotified = true;
                    }

                    //GSX Startup handling
                    if (!Menu.FirstReadyReceived && NextMenuStartupCheck <= DateTime.Now)
                    {
                        if (Profile.SecureAircraftOnSessionStart && IsProcessRunning && ApiClient.IsReady && Profile.RunAutomationService && !AutomationController.IsStarted && AircraftController.IsConnected && IsOnGround
                            && SkippedWalkAround && await Aircraft.GetSpeed() < 1 && !await Aircraft.GetEngineRunning() && !await Aircraft.GetLightBeacon() && ServicePushBack.PushStatus == 0)
                        {
                            bool chocks = await Aircraft.GetEquipmentChocks();
                            bool hasChocks = await Aircraft.GetHasChocks();
                            Logger.Debug($"HasChocks {hasChocks} | EquipmentChocks {chocks} | IsBrakeSet {await Aircraft.GetBrakeSet()} | Type {Aircraft.GetType().Name}");
                            if (hasChocks && !chocks)
                            {
                                Logger.Information($"GSX Menu not opening - setting Chocks ...");
                                await Aircraft.SetEquipmentChocks(true, true);
                            }
                            else if (!hasChocks && Aircraft.ISettingProfile.GetSetting<bool>(GenericSettings.VarParkBrakeToggleStart) && !await Aircraft.GetBrakeSet())
                            {
                                Logger.Information($"GSX Menu not opening - setting Parking Brake ...");
                                await Aircraft.SetParkingBrake(true);
                            }
                        }

                        if (IsProcessRunning && ApiClient.IsReady && Menu.MenuCommandsAllowed && (AutomationState != AutomationState.TaxiIn || !Config.OpenMenuOnTaxiIn))
                        {
                            Menu.ExternalSequence = true;
                            Logger.Information($"Open GSX Menu on {AutomationState} ...");
                            await Menu.RunCommand(GsxMenuCommand.Close(), false);
                            await Menu.WaitInterval();
                            await Menu.RunCommand(GsxMenuCommand.Open(), IsOnGround && (Profile.EnableMenuForSelection || !Profile.RunAutomationService) && !Profile.PilotsDeckIntegration && AutomationState == AutomationState.SessionStart);
                            await Menu.WaitInterval(2);
                            if (Menu.FirstReadyReceived && Menu.MenuState == GsxMenuState.READY && (Menu.IsGateMenu || !IsOnGround) && Profile.RunAutomationService && !Profile.PilotsDeckIntegration)
                                await Menu.RunCommand(GsxMenuCommand.Disable(), false);
                            Menu.ExternalSequence = false;
                        }

                        if (!IsGsxRunning && AppService.Instance.LastGsxRestart <= DateTime.Now - TimeSpan.FromSeconds(Config.WaitGsxRestart) && IsOnGround)
                        {
                            if (CouatlInvalidCount > 0)
                                Logger.Warning($"GSX API/Process State is still invalid #{CouatlInvalidCount}");
                            CouatlInvalidCount++;
                            if (CouatlInvalidCount > Config.GsxMenuStartupMaxFail && Config.RestartGsxStartupFail)
                            {
                                Logger.Information($"Restarting GSX ...");
                                await AppService.Instance.RestartGsx();
                                CouatlInvalidCount = 0;
                                await Task.Delay(Config.GsxServiceStartDelay, RequestToken);
                            }
                        }

                        NextMenuStartupCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxStartupMenuCheck);
                    }

                    await CheckCouatlConf();

                    //Start Automation
                    if (!AutomationController.IsStarted && CanAutomationRun && !AutomationController.RunFlag)
                    {
                        await Task.Delay(Config.GsxServiceStartDelay / 2, RequestToken);
                        _ = AutomationController.Run();
                    }

                    CheckProcess();

                    await Task.Delay(Config.TimerGsxCheck, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    try { await ApiClient.Stop(); } catch { }
                }
            }

            IsActive = false;
            Logger.Debug($"GsxService ended");
        }

        protected virtual void CheckProcess()
        {
            if (NextProcessCheck <= DateTime.Now)
            {
                IsProcessRunning = CheckBinaries();
                NextProcessCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxProcessCheck);
            }
        }

        public virtual bool CheckBinaries()
        {
            var version = SimConnect.GetSimVersion();
            if (version == SimVersion.MSFS2020)
                return Sys.GetProcessRunning(Config.BinaryGsx2020);
            else if (version == SimVersion.MSFS2024)
                return Sys.GetProcessRunning(Config.BinaryGsx2024);
            else
                return false;
        }

        protected virtual bool CheckWalkAround()
        {
            if (IsMsfs2024)
                return SimStore["IS AVATAR"]?.GetNumber() == 1;
            else
                return false;
        }

        protected virtual bool CheckSessionReady()
        {
            return SimConnect.CameraState < 11;
        }

        protected virtual async Task SkipWalkaround()
        {
            WalkAroundSkipActive = true;
            try
            {
                while (Profile.RunAutomationService && IsWalkaround && !SkippedWalkAround && Profile.SkipWalkAround && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    Logger.Information("Automation: Skip Walkaround");
                    if (AircraftController.IsConnected)
                        await WalkaroundPreActionNotify();
                    await ToggleWalkaround();
                    if (IsWalkaround)
                        await Task.Delay(Config.TimerGsxCheck, RequestToken);

                    SkippedWalkAround = (CheckSessionReady() && !IsWalkaround) || !Profile.RunAutomationService;
                }
                SkippedWalkAround = (CheckSessionReady() && !IsWalkaround) || !Profile.RunAutomationService;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            WalkAroundSkipActive = false;
        }

        protected virtual Task WalkaroundPreActionNotify()
        {
            if (IsWalkaround && !WalkaroundPreActionNotified)
            {
                WalkaroundPreActionNotified = true;
                if (WalkaroundPreAction != null)
                    return TaskTools.RunPool(() => WalkaroundPreAction?.Invoke(), RequestToken);
            }

            return Task.CompletedTask;
        }

        public virtual async Task ToggleWalkaround()
        {
            Logger.Debug("Toggling Walkaround ...");
            string title = WindowTools.GetMsfsWindowTitle();
            Sys.SetForegroundWindow(title);
            await Task.Delay(Config.DelayForegroundChange, RequestToken);
            string active = Sys.GetActiveWindowTitle();
            if (active == title)
            {
                Logger.Debug($"Sending Keystrokes");
                WindowTools.SendWalkaroundKeystroke();
                await Task.Delay(Config.DelayAircraftModeChange, RequestToken);
            }
            else
                Logger.Debug($"Active Window did not match to '{title}'");
        }

        protected virtual async Task HandleWalkaroundEquipment()
        {
            if (!SkippedWalkAround && Profile.CallJetwayStairsInWalkaround && Menu.IsGateMenu)
            {
                if (Profile.CallReposition && !ServiceReposition.IsCalled)
                {
                    Logger.Information("Automation: Reposition Aircraft on Gate");
                    await ServiceReposition.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                else
                {
                    bool temp = Profile.OperatorAutoSelect;
                    Profile.OperatorAutoSelect = true;

                    if (AircraftController.IsConnected)
                        await Aircraft.HandleWalkaroundEquipment();

                    bool hasAirStairForward = await Aircraft.GetHasAirStairForward();
                    bool hasAirStairAft = await Aircraft.GetHasAirStairAft();
                    if (!ServiceStairs.IsCalled
                        && ((!ServiceJetway.IsAvailable && !hasAirStairForward && !hasAirStairAft) || (ServiceJetway.IsAvailable && !hasAirStairAft)))
                    {
                        Logger.Information("Automation: Call GSX Stairs on Walkaround");
                        await ServiceStairs.Call();
                    }

                    Profile.OperatorAutoSelect = temp;
                }
            }
            else if (!Menu.IsGateMenu && Menu.MenuCommandsAllowed && Menu.MenuState >= GsxMenuState.TIMEOUT)
            {
                Logger.Information("Automation: Refresh Menu to handle Walkaround Equip ...");
                await Menu.RunCommand(GsxMenuCommand.Open(), false);
            }
        }

        protected virtual async Task CheckCouatlConf()
        {
            if (NextProcessCheck > DateTime.Now)
                return;

            if (!ApiClient.IsSocketValid && SimStore[GsxConstants.VarSetRemoteServer].GetNumber() != 1)
                Logger.Error("The GSX Remote Control Server needs to be enabled! (GSX Settings > Network)");

            if (Aircraft == null || Aircraft?.IsConnected == false || !IsGsxRunning)
                return;

            try
            {
                bool acPref;
                acPref = await Aircraft.GetSettingAutoMode();
                await CheckSettingApi(GsxConstants.SettingAutoMode, acPref);

                acPref = await Aircraft.GetSettingProgRefuel();
                await CheckSettingApi(GsxConstants.SettingProgFuel, acPref);

                acPref = await Aircraft.GetSettingDetectCustFuel();
                await CheckSettingApi(GsxConstants.SettingCustFuel, acPref);

                if (Config.GsxAllowSettingManipulation)
                    await CheckSettingApi(GsxConstants.SettingAutoStairs, 2);

                if (Config.GsxNotifyOnlyImportant)
                    await CheckSettingApi(GsxConstants.SettingMessageVerbosity, 1);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyOnlyImportant), GsxConstants.SettingMessageVerbosity, 1);

                if (Config.GsxNotifyNoInvoice)
                    await CheckSettingApi(GsxConstants.SettingDisableInvoices, 1);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyNoInvoice), GsxConstants.SettingDisableInvoices, 1);

                if (Config.GsxNotifyNoAudio)
                    await CheckSettingApi(GsxConstants.SettingAuralCues, 0);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyNoAudio), GsxConstants.SettingAuralCues, 0);

                if (Config.GsxNotifyNoCashRegister)
                    await CheckSettingApi(GsxConstants.SettingCashRegisterBeep, 0);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyNoCashRegister), GsxConstants.SettingCashRegisterBeep, 0);

                if (Config.GsxNotifyPinnedMenuAuto)
                    await CheckSettingApi(GsxConstants.SettingPinnedMenuMode, 3);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyPinnedMenuAuto), GsxConstants.SettingPinnedMenuMode, 3);

                if (Config.GsxNotifyNoNumberKeys)
                    await CheckSettingApi(GsxConstants.SettingMenuNumberKey, 0);
                else
                    CheckConfigSetting(nameof(Config.GsxNotifyNoNumberKeys), GsxConstants.SettingMenuNumberKey, 0);

                if (Config.GsxChangePaxDensity)
                {
                    int density = HasGateJetway ? Config.GsxPaxDensityJetway : Config.GsxPaxDensityStairs;
                    await CheckSettingApi(GsxConstants.SettingPaxDensityBoard, density);
                    await CheckSettingApi(GsxConstants.SettingPaxDensityDeboard, density);
                }

                bool isCargo = await Aircraft.GetIsCargo();
                if (Config.GsxChangeMultiTrips)
                {
                    bool target = isCargo ? Config.GsxMultiTripsCargo : Config.GsxMultiTripsPax;
                    await CheckSettingApi(GsxConstants.SettingMultiTripsTrains, target ? 1 : 0);
                    await Task.Delay(Config.MenuCheckInterval, RequestToken);
                }

                if (Config.GsxChangeLoadScale && ApiClient.GetSetting(GsxConstants.SettingMultiTripsTrains) == 1)
                {
                    int density = isCargo ? Config.GsxLoadScaleCargo : Config.GsxLoadScalePax;
                    await CheckSettingApi(GsxConstants.SettingLoadScaleBag, density);
                    await CheckSettingApi(GsxConstants.SettingLoadScaleCargo, density);
                }

                SyncType doorSync = AppService.Instance?.PluginCapabilities?.DoorHandling ?? SyncType.ManualNone;
                acPref = doorSync.HasFlag(SyncType.Aircraft) || doorSync.HasFlag(SyncType.Plugin);
                if (doorSync != SyncType.ManualNone && acPref)
                    await CheckSettingVariable(GsxConstants.VarSetDoorMsg, acPref);

                acPref = await Aircraft.GetSettingAdvAutomation();
                await CheckSettingVariable(GsxConstants.VarSetAutoDoors, acPref);
                await CheckSettingVariable(GsxConstants.VarSetAutoFuel, acPref);
                await CheckSettingVariable(GsxConstants.VarSetAutoPayload, acPref);
                await CheckSettingVariable(GsxConstants.VarSetAutoEquip, acPref);
                if (!acPref)
                    await CheckSettingApi(GsxConstants.SettingProgFuel, 0);

                bool? underground = ApiData?.HandlerData?.gate?.undergroundRefueling;
                acPref = Profile?.GetSetting<bool>(GenericSettings.OptionGateForceUnderground) == true;
                if (underground is false && acPref)
                {
                    Logger.Information($"Enabling 'Underground refueling' for the Gate in GSX' Airport Profile");
                    await ApiClient.SetHandlerGate("undergroundRefueling", true);
                }

                if (ApiData?.HandlerData?.aircraft?.refueling == true && await Aircraft.GetHasFuelSync())
                {
                    Logger.Information("Disabling 'Default Fuel system' in GSX' Aircraft Profile");
                    await ApiClient.SetHandlerAircraft("refueling", false);
                }

                if (Profile?.HasSetting<int>(GenericSettings.OptionAircraftDoorPreferred, out int value) == true && value > 0 && ApiData?.HandlerData?.aircraft?.preferredExit != value - 1)
                {
                    Logger.Information($"Changing preferred Exit to {(AircraftDoor)value} (#{value - 1})");
                    await ApiClient.SetHandlerAircraft("preferredExit", value - 1);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected Task CheckSettingApi(string key, bool target)
        {
            return CheckSettingApi(key, target ? 1 : 0);
        }

        protected async Task CheckSettingApi(string key, double target)
        {
            try
            {
                if (ApiClient.GetSetting(key) != target)
                    await ApiClient.SetSetting(key, target);
            }
            catch (Exception ex)
            {
                Logger.Debug($"{ex.GetType().Name}: {ex.Message}");
            }
        }

        protected void CheckConfigSetting(string property, string key, dynamic target)
        {
            try
            {
                if (Config.GetPropertyValue<bool>(property) == false && ApiClient.GetSetting(key) == target)
                {
                    Config.SetPropertyValue<bool>(property, true);
                    Config.SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"{ex.GetType().Name}: {ex.Message}");
            }
        }

        protected Task CheckSettingVariable(string name, bool target)
        {
            return CheckSettingVariable(name, target ? 1 : 0);
        }

        protected async Task CheckSettingVariable(string name, double target)
        {
            if (SimStore.TryGet(name, out var sub))
            {
                if (sub.GetNumber() != target)
                {
                    Logger.Debug($"Change GSX Config Variable '{name}' to '{target}'");
                    await sub.WriteValue(target);
                }
            }
            else
                Logger.Error($"GSX Config Variable '{name}' not in SimStore");
        }

        public virtual async Task ReloadSimbrief()
        {
            Logger.Information($"Automation: Refreshing GSX SimBrief/VDGS Data");
            Logger.Debug($"Simbrief Refresh - inhibiting Couatl State Changes for {(double)Config.MenuOpenTimeout / 1000.0}s");
            await ApiClient.CommandRun(GsxCommandRunType.ReloadSimbrief);

            await Task.Delay(Config.OperatorWaitTimeout * 2, RequestToken);
        }

        public virtual Task SetPaxBoard(int pax)
        {
            return ServiceBoard.SetPaxTarget(pax);
        }

        public virtual Task SetPaxDeboard(int pax)
        {
            return ServiceDeboard.SetPaxTarget(pax);
        }

        public virtual Task CancelRefuel()
        {
            return GsxServices[GsxServiceType.Refuel].Cancel();
        }

        public virtual void SetPushDirection(string direction)
        {
            bool notify = AutoSelectionPushback != direction;
            if (notify)
            {
                AutoSelectionPushback = direction;
                _ = TaskTools.RunPool(() => OnPushDirectionChanged?.Invoke(AutoSelectionPushback), RequestToken);
            }
        }

        public virtual void SetArrivalGate(JsonNode node)
        {
            bool notify = AutoSelectionArrivalGate?.ToString() != node?.ToString();
            if (notify)
            {
                AutoSelectionArrivalGate = node;
                _ = TaskTools.RunPool(() => OnArrivalGateChanged?.Invoke(AutoSelectionArrivalGate?.ToString() ?? ""), RequestToken);
            }
        }

        public virtual void SetArrivalGateName(string name)
        {
            bool notify = AutoSelectionArrivalGate?.ToString() != name;
            if (notify)
            {
                AutoSelectionArrivalGate = JsonValue.Create(name);
                _ = TaskTools.RunPool(() => OnArrivalGateChanged?.Invoke(AutoSelectionArrivalGate?.ToString() ?? ""), RequestToken);
            }
        }

        public virtual void SetArrivalGateNumber(int number)
        {
            bool notify = AutoSelectionArrivalGate?.ToString() != number.ToString();
            if (notify)
            {
                AutoSelectionArrivalGate = JsonValue.Create(number);
                _ = TaskTools.RunPool(() => OnArrivalGateChanged?.Invoke(AutoSelectionArrivalGate?.ToString() ?? ""), RequestToken);
            }
        }

        public override async Task Stop()
        {
            await ApiClient.Stop();
            AutomationController.Stop();
            Menu.Reset();

            foreach (var service in GsxServices)
                await service.Value.ResetState();

            IsActive = false;
            SkippedWalkAround = false;
            WalkaroundPreActionNotified = false;
            WalkaroundNotified = false;
            WalkAroundSkipActive = false;
            CouatlInvalidCount = 0;
            LastReadyState = false;
            FirstGroundCheck = true;
            GroundCounter = 0;
            IsAirStart = false;
            NextMenuStartupCheck = DateTime.MinValue;

            await base.Stop();
        }

        protected override Task DoCleanup()
        {
            try
            {
                ApiClient.ReadyStateChanged -= OnReadyStateChange;

                ServiceReposition.OnRepositionSignal -= OnRepositionSignal;

                foreach (var service in GsxServices)
                    service.Value.ReleaseSubscriptions();

                Menu.FreeResources();
                AutomationController.FreeResources();

                SimStore.Remove(GsxConstants.VarSetDoorMsg);
                SimStore.Remove(GsxConstants.VarSetAutoDoors);
                SimStore.Remove(GsxConstants.VarSetAutoFuel);
                SimStore.Remove(GsxConstants.VarSetAutoPayload);
                SimStore.Remove(GsxConstants.VarSetAutoEquip);
                SimStore.Remove(GenericSettings.VarSmartButtonDefault);
                SimStore.Remove(GsxConstants.VarDeiceAvail);
                SimStore.Remove(GsxConstants.VarCrewTarget);
                SimStore.Remove(GsxConstants.VarPilotTarget);
                SimStore.Remove(GsxConstants.VarDoorToggleExit1);
                SimStore.Remove(GsxConstants.VarDoorToggleExit2);
                SimStore.Remove(GsxConstants.VarDoorToggleExit3);
                SimStore.Remove(GsxConstants.VarDoorToggleExit4);
                SimStore.Remove(GsxConstants.VarDoorToggleService1);
                SimStore.Remove(GsxConstants.VarDoorToggleService2);
                SimStore.Remove(GsxConstants.VarDoorToggleCargo1);
                SimStore.Remove(GsxConstants.VarDoorToggleCargo2);
                SimStore.Remove(GsxConstants.VarDoorToggleCargo3);
                SimStore.Remove(GsxConstants.VarCargoLoader1);
                SimStore.Remove(GsxConstants.VarCargoLoader2);
                SimStore.Remove(GsxConstants.VarCargoLoader3);
                SimStore.Remove(GsxConstants.VarCargoTrainEmptyFront);
                SimStore.Remove(GsxConstants.VarCargoTrainEmptyRear);
                SimStore.Remove(GsxConstants.VarCargoTrainEmptyMain);
                SimStore.Remove(GsxConstants.VarCargoTrainFullFront);
                SimStore.Remove(GsxConstants.VarCargoTrainFullRear);
                SimStore.Remove(GsxConstants.VarCargoTrainFullMain);
                SimStore.Remove(GsxConstants.VarSetRemoteServer);
                SubsCargoTrainsEmpty.Clear();
                SubsCargoTrainsFull.Clear();

                SimStore.Remove("SIM ON GROUND");
                SimStore.Remove("ABSOLUTE TIME");
                if (IsMsfs2024)
                {
                    SimStore.Remove("IS AIRCRAFT");
                    SimStore.Remove("IS AVATAR");
                }
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
