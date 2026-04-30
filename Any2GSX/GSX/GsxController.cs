using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Automation;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
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
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX
{
    public class GsxController(Config config) : ServiceController<Any2GSX, AppService, Config, Definition>(config), IGsxController
    {
        protected bool _lock = false;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual SimConnectController SimController => AppService.Instance?.SimService?.Controller;
        public virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController?.Aircraft;
        public virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        public virtual bool IsMsfs2024 => SimConnect.GetSimVersion() == SimVersion.MSFS2024;
        public virtual string PathInstallation { get; } = Sys.GetRegistryValue<string>(GsxConstants.RegPath, GsxConstants.RegValue, null) ?? GsxConstants.PathDefault;
        public virtual GsxMenu Menu { get; } = new();
        public virtual IGsxMenu IMenu => Menu;
        protected virtual DateTime NextMenuStartupCheck { get; set; } = DateTime.MinValue;
        public virtual IConfig IConfig => Config;
        public virtual GsxAutomationController AutomationController { get; } = new();
        public virtual IGsxAutomationController IAutomationController => AutomationController;
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
        public virtual bool HasGateJetway => ServiceJetway.State != GsxServiceState.NotAvailable && ServiceJetway.State != GsxServiceState.Unknown;
        public virtual bool HasGateStair => ServiceStairs.State != GsxServiceState.NotAvailable && ServiceStairs.State != GsxServiceState.Unknown;
        public virtual bool HasUndergroundRefuel => ServiceRefuel?.IsUnderground == true;
        public virtual bool ServicesValid => ServiceStairs.State != GsxServiceState.Unknown || ServiceJetway.State != GsxServiceState.Unknown || !IsOnGround;
        public virtual bool IsDeiceAvail => SubDeiceAvail?.GetNumber() != 0;
        public virtual bool IsRefuelActive => GsxServices[GsxServiceType.Refuel].State == GsxServiceState.Active;
        public virtual bool IsFuelHoseConnected => (GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel)?.SubRefuelHose?.GetNumber() == 1;

        public virtual bool CouatlVarsValid { get; protected set; } = false;
        public virtual int CouatlLastProgress { get; protected set; } = 0;
        public virtual int CouatlLastStarted { get; protected set; } = 0;
        public virtual int CouatlLastSimbrief { get; protected set; } = 0;
        public virtual DateTime CouatlInhibitStateChanges { get; protected set; } = DateTime.MinValue;
        public virtual int CouatlInvalidCount { get; protected set; } = 0;
        public virtual bool CouatlVarsReceived { get; protected set; } = false;
        public virtual bool IsProcessRunning { get; protected set; } = false;
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsGsxRunning => IsProcessRunning && CouatlVarsValid;
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

        public event Func<Task> WalkaroundPreAction;
        public event Func<Task> WalkaroundWasSkipped;
        public event Func<Task> RepositionSignal;

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
        public virtual ISimResourceSubscription SubDefaultSmartButton { get; protected set; }
        public virtual ISimResourceSubscription SubPilotTarget { get; protected set; }
        public virtual ISimResourceSubscription SubCrewTarget { get; protected set; }
        public virtual ISimResourceSubscription SubDeiceAvail { get; protected set; }

        protected virtual void InitGsxServices()
        {
            if (GsxServices.IsEmpty)
            {
                _ = new GsxServiceReposition(this);
                _ = new GsxServiceRefuel(this);
                _ = new GsxServiceCatering(this);
                _ = new GsxServiceJetway(this);
                _ = new GsxServiceStairs(this);
                _ = new GsxServiceBoarding(this);
                _ = new GsxServiceDeboarding(this);
                _ = new GsxServicePushback(this);
                _ = new GsxServiceGpu(this);
                _ = new GsxServiceDeice(this);
                _ = new GsxServiceLavatory(this);
                _ = new GsxServiceWater(this);
                _ = new GsxServiceCleaning(this);
            }
            else
            {
                foreach (var service in GsxServices)
                    service.Value.InitSubscriptions();
            }
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
            SimStore.AddVariable(GsxConstants.VarCouatlStarted)?.OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlSimbrief)?.OnReceived += OnCouatlSimbrief;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg5)?.OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg6)?.OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg7)?.OnReceived += OnCouatlVariable;

            SimStore.AddVariable("SIM ON GROUND", SimUnitType.Bool);
            SimStore.AddVariable("ABSOLUTE TIME", SimUnitType.Seconds);
            if (IsMsfs2024)
            {
                SimStore.AddVariable("IS AIRCRAFT", SimUnitType.Number);
                SimStore.AddVariable("IS AVATAR", SimUnitType.Number);
            }

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

            SubPilotTarget = SimStore.AddVariable(GsxConstants.VarPilotTarget);
            SubCrewTarget = SimStore.AddVariable(GsxConstants.VarCrewTarget);

            SubDeiceAvail = SimStore.AddVariable(GsxConstants.VarDeiceAvail);

            SubDefaultSmartButton = SimStore.AddVariable(GenericSettings.VarSmartButtonDefault);

            SimStore.AddVariable(GsxConstants.VarReadAutoMode);
            SimStore.AddVariable(GsxConstants.VarSetAutoMode);
            SimStore.AddVariable(GsxConstants.VarReadCustFuel);
            SimStore.AddVariable(GsxConstants.VarSetCustFuel);
            SimStore.AddVariable(GsxConstants.VarReadProgFuel);
            SimStore.AddVariable(GsxConstants.VarSetProgFuel);
            SimStore.AddVariable(GsxConstants.VarSetAutoDoors);
            SimStore.AddVariable(GsxConstants.VarSetAutoFuel);
            SimStore.AddVariable(GsxConstants.VarSetAutoPayload);
            SimStore.AddVariable(GsxConstants.VarSetAutoEquip);
            SimStore.AddVariable(GsxConstants.VarSetDoorMsg);

            InitGsxServices();
            Menu.Init();
            AutomationController.Init();

            ServiceReposition.SubRepositioning.OnReceived += OnRepositionSignal;

            return Task.CompletedTask;
        }

        protected virtual Task OnRepositionSignal(ISimResourceSubscription sub, object data)
        {
            if (sub?.GetNumber() > 0)
                _ = TaskTools.RunPool(() => RepositionSignal?.Invoke());

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

        protected virtual Task OnCouatlSimbrief(ISimResourceSubscription sub, object data)
        {
            try
            {
                while (_lock && !Token.IsCancellationRequested) { }
                int state = (int)sub.GetNumber();
                int started = (int)(SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0);
                if (CouatlLastSimbrief == 1 && state == 0 && started == 1 && CouatlInhibitStateChanges < DateTime.Now)
                {
                    Logger.Debug("Simbrief Refresh detected - inhibiting Couatl State Changes for 5s");
                    CouatlInhibitStateChanges = DateTime.Now + TimeSpan.FromSeconds(5);
                }

                CouatlLastSimbrief = state;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                CouatlInhibitStateChanges = DateTime.MinValue;
                CouatlLastSimbrief = 0;
            }
            return Task.CompletedTask;
        }

        protected virtual Task OnCouatlVariable(ISimResourceSubscription sub, object data)
        {
            while (_lock && !Token.IsCancellationRequested) { }
            _lock = true;

            try
            {
                CouatlLastStarted = (int)(SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0);
                CouatlLastProgress = (int)Math.Max(
                    Math.Max(SimStore[GsxConstants.VarCouatlStartProg5]?.GetNumber() ?? 100, SimStore[GsxConstants.VarCouatlStartProg6]?.GetNumber() ?? 100),
                    SimStore[GsxConstants.VarCouatlStartProg7]?.GetNumber() ?? 100
                    );
                if (CouatlLastStarted == 1 && CouatlLastProgress == 0)
                {
                    if (!CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables valid!");
                        CouatlVarsValid = true;
                        CouatlInhibitStateChanges = DateTime.MinValue;
                        CouatlInvalidCount = 0;
                        _ = TaskTools.RunPool(() => OnCouatlStarted?.Invoke(this));
                    }
                }
                else if (CouatlVarsValid)
                {
                    if (CouatlInhibitStateChanges < DateTime.Now)
                    {
                        Logger.Debug($"Couatl Variables NOT valid! (started: {CouatlLastStarted} / progress: {CouatlLastProgress})");
                        CouatlVarsValid = false;
                        _ = TaskTools.RunPool(() => OnCouatlStopped?.Invoke(this));
                    }
                    else
                        Logger.Debug($"Couatl Variables invalid-Change ignored (started: {CouatlLastStarted} / progress: {CouatlLastProgress})");
                }

                CouatlVarsReceived = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            _lock = false;
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
                Menu.Reset();
                await Task.Delay(1000, Token);

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

                AutomationController.Reset();
                IsActive = true;
                Logger.Debug($"GsxService active (VarsReceived: {CouatlVarsReceived} | FirstReady: {Menu.FirstReadyReceived})");
                Logger.Information($"GsxController active - GSX Ready: {CouatlVarsReceived && IsProcessRunning} | GSX Menu: {Menu.FirstReadyReceived} | Aircraft Ready: {AircraftController.IsConnected} | Walkaround: {IsWalkaround}");

                Logger.Debug($"Wait up to {Config.GsxServiceStartDelay}ms for Aircraft Plugin Connection");
                int delay = 0;
                while (delay <= Config.GsxServiceStartDelay && (AircraftController?.IsConnected == false || delay <= 3000) && !RequestToken.IsCancellationRequested)
                {
                    delay += Config.StateMachineInterval;
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                Logger.Debug($"Continue after {delay}ms");
                await AppService.Instance.CommBus.PingModule();

                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    CheckGround();

                    if (!CouatlVarsReceived && IsProcessRunning)
                        await OnCouatlVariable(null, null);

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
                            _ = TaskTools.RunPool(() => WalkaroundWasSkipped?.Invoke());
                        WalkaroundNotified = true;
                    }

                    //GSX Startup handling
                    if (!Menu.FirstReadyReceived && NextMenuStartupCheck <= DateTime.Now && IsOnGround)
                    {
                        if (IsGsxRunning && Profile.RunAutomationService && NextMenuStartupCheck != DateTime.MinValue && !AutomationController.IsStarted && AircraftController.IsConnected
                            && SkippedWalkAround && await Aircraft.GetSpeed() < 1 && !await Aircraft.GetEngineRunning() && !await Aircraft.GetLightBeacon() && ServicePushBack.PushStatus == 0)
                        {
                            Logger.Debug($"HasChocks {await Aircraft.GetHasChocks()} | EquipmentChocks {await Aircraft.GetEquipmentChocks()} | IsBrakeSet {await Aircraft.GetBrakeSet()} | Type {Aircraft.GetType().Name}");
                            if (await Aircraft.GetHasChocks() && !await Aircraft.GetEquipmentChocks())
                            {
                                Logger.Information($"GSX Menu not opening - setting Chocks ...");
                                await Aircraft.SetEquipmentChocks(true, true);
                            }
                            else if (!await Aircraft.GetHasChocks() && !await Aircraft.GetBrakeSet())
                            {
                                Logger.Information($"GSX Menu not opening - setting Parking Brake ...");
                                await Aircraft.SetParkingBrake(true);
                            }
                        }

                        if (IsGsxRunning && Menu.MenuCommandsAllowed)
                        {
                            Menu.ExternalSequence = true;
                            Logger.Information($"Open GSX Menu on Startup ...");
                            await Menu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection || !Profile.RunAutomationService);
                            if (Menu.FirstReadyReceived && Menu.MenuState == GsxMenuState.READY && Menu.IsGateMenu && Profile.RunAutomationService)
                                await Menu.RunCommand(GsxMenuCommand.State(GsxMenuState.DISABLED), false);
                            Menu.ExternalSequence = false;
                        }

                        if ((!CouatlVarsValid || !IsProcessRunning) && NextMenuStartupCheck != DateTime.MinValue && AppService.Instance.LastGsxRestart <= DateTime.Now - TimeSpan.FromSeconds(Config.WaitGsxRestart))
                        {
                            if (CouatlInvalidCount > 0)
                                Logger.Warning($"GSX Variable/Process State is still invalid #{CouatlInvalidCount}");
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

                    await SetCouatlConf();

                    //Start Automation
                    if (!AutomationController.IsStarted && CanAutomationRun && !AutomationController.RunFlag)
                    {
                        await Task.Delay(Config.GsxServiceStartDelay / 2, Token);
                        Tracker.Clear(AppNotification.GateSelect);
                        Tracker.Clear(AppNotification.GateMove);
                        _ = AutomationController.Run();
                    }

                    CheckProcess();

                    await Task.Delay(Config.TimerGsxCheck, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
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

            if (CouatlVarsValid && !IsProcessRunning)
            {
                Logger.Debug($"Couatl Process not running!");
                CouatlVarsValid = false;
                Menu.ResetNotRunning();
                App.AppService.SetGsxStartTime();
                _ = TaskTools.RunPool(() => OnCouatlStopped?.Invoke(this));
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
                        await Task.Delay(Config.TimerGsxCheck, Token);

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
                    return TaskTools.RunPool(() => WalkaroundPreAction?.Invoke());
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

                    if (ServiceStairs.IsConnectable && !await AircraftController.GetIsFuelOnStairSide())
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

        protected virtual async Task SetCouatlConf()
        {
            if (Aircraft == null || NextProcessCheck > DateTime.Now)
                return;

            try
            {
                bool gsxMode = SimStore[GsxConstants.VarReadAutoMode]?.GetNumber() > 0;
                bool acPref = await Aircraft.GetSettingAutoMode();
                if (gsxMode != acPref)
                {
                    Logger.Debug($"Set GSX Setting AutoMode to '{acPref}'");
                    SimStore[GsxConstants.VarSetAutoMode]?.WriteValue(acPref ? 1 : -1);
                }

                gsxMode = SimStore[GsxConstants.VarReadProgFuel]?.GetNumber() > 0 || SimStore[GsxConstants.VarSetProgFuel]?.GetNumber() >= 0;
                acPref = await Aircraft.GetSettingProgRefuel();
                if (gsxMode != acPref)
                {
                    Logger.Debug($"Set GSX Setting ProgRefuel to '{acPref}'");
                    SimStore[GsxConstants.VarSetProgFuel]?.WriteValue(acPref ? 1 : -1);
                }

                gsxMode = SimStore[GsxConstants.VarReadCustFuel]?.GetNumber() > 0;
                acPref = await Aircraft.GetSettingDetectCustFuel();
                if (gsxMode != acPref)
                {
                    Logger.Debug($"Set GSX Setting DetectCustFuel to '{acPref}'");
                    SimStore[GsxConstants.VarSetCustFuel]?.WriteValue(acPref ? 1 : -1);
                }

                SyncType doorSync = AppService.Instance?.PluginCapabilities?.DoorHandling ?? SyncType.ManualNone;
                gsxMode = SimStore[GsxConstants.VarSetDoorMsg]?.GetNumber() > 0;
                acPref = doorSync.HasFlag(SyncType.Aircraft) || doorSync.HasFlag(SyncType.Plugin);
                if (doorSync != SyncType.ManualNone && gsxMode != acPref)
                {
                    Logger.Debug($"Disable GSX Door Message");
                    await SimStore[GsxConstants.VarSetDoorMsg]?.WriteValue(1);
                }

                gsxMode = SimStore[GsxConstants.VarSetAutoDoors]?.GetNumber() > 0 || SimStore[GsxConstants.VarSetAutoFuel]?.GetNumber() > 0 || SimStore[GsxConstants.VarSetAutoPayload]?.GetNumber() > 0 || SimStore[GsxConstants.VarSetAutoEquip]?.GetNumber() > 0;
                acPref = await Aircraft.GetSettingAdvAutomation();
                if (gsxMode != acPref)
                {
                    Logger.Debug($"Disable GSX own Automation for PMDG");
                    await SimStore[GsxConstants.VarSetAutoDoors]?.WriteValue(0);
                    await SimStore[GsxConstants.VarSetAutoFuel]?.WriteValue(0);
                    await SimStore[GsxConstants.VarSetAutoPayload]?.WriteValue(0);
                    await SimStore[GsxConstants.VarSetAutoEquip]?.WriteValue(0);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual async Task ReloadSimbrief()
        {
            Logger.Information($"Automation: Refreshing GSX SimBrief/VDGS Data");
            Logger.Debug($"Simbrief Refresh - inhibiting Couatl State Changes for {(double)Config.MenuOpenTimeout / 1000.0}s");
            CouatlInhibitStateChanges = DateTime.Now + TimeSpan.FromMilliseconds(Config.MenuOpenTimeout);
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(15));
            await Menu.RunSequence(sequence);

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

        public override Task Stop()
        {
            AutomationController.Stop();
            Menu.Reset();

            foreach (var service in GsxServices)
                service.Value.ResetState();

            IsActive = false;
            SkippedWalkAround = false;
            WalkaroundPreActionNotified = false;
            WalkaroundNotified = false;
            WalkAroundSkipActive = false;
            CouatlVarsValid = false;
            CouatlVarsReceived = false;
            CouatlLastProgress = 0;
            CouatlLastStarted = 0;
            CouatlInvalidCount = 0;
            FirstGroundCheck = true;
            GroundCounter = 0;
            IsAirStart = false;
            NextMenuStartupCheck = DateTime.MinValue;

            return base.Stop();
        }

        protected override Task DoCleanup()
        {
            try
            {
                foreach (var service in GsxServices)
                    service.Value.FreeResources();

                Menu.FreeResources();
                AutomationController.FreeResources();

                SimStore.Remove(GsxConstants.VarSetDoorMsg);
                SimStore.Remove(GsxConstants.VarSetAutoDoors);
                SimStore.Remove(GsxConstants.VarSetAutoFuel);
                SimStore.Remove(GsxConstants.VarSetAutoPayload);
                SimStore.Remove(GsxConstants.VarSetAutoEquip);
                SimStore.Remove(GsxConstants.VarReadAutoMode);
                SimStore.Remove(GsxConstants.VarSetAutoMode);
                SimStore.Remove(GsxConstants.VarReadCustFuel);
                SimStore.Remove(GsxConstants.VarSetCustFuel);
                SimStore.Remove(GsxConstants.VarReadProgFuel);
                SimStore.Remove(GsxConstants.VarSetProgFuel);
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
                SimStore[GsxConstants.VarCouatlStarted]?.OnReceived -= OnCouatlVariable;
                SimStore[GsxConstants.VarCouatlSimbrief]?.OnReceived -= OnCouatlSimbrief;
                SimStore[GsxConstants.VarCouatlStartProg5]?.OnReceived -= OnCouatlVariable;
                SimStore[GsxConstants.VarCouatlStartProg6]?.OnReceived -= OnCouatlVariable;
                SimStore[GsxConstants.VarCouatlStartProg7]?.OnReceived -= OnCouatlVariable;
                SimStore.Remove(GsxConstants.VarCouatlStarted);
                SimStore.Remove(GsxConstants.VarCouatlStartProg5);
                SimStore.Remove(GsxConstants.VarCouatlStartProg6);
                SimStore.Remove(GsxConstants.VarCouatlStartProg7);

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
