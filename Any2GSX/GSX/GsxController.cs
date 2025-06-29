using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Tools;
using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Any2GSX.GSX
{
    public class GsxController(Config config) : ServiceController<Any2GSX, AppService, Config, Definition>(config), IGsxController
    {
        protected bool _lock = false;
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
        public virtual MessageReceiver<MsgGsxCouatlStarted> MsgCouatlStarted { get; protected set; }
        public virtual MessageReceiver<MsgGsxCouatlStopped> MsgCouatlStopped { get; protected set; }
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
        public int JetwayState => (int)(ServiceJetway?.State ?? 0);
        public int JetwayOperation => (int)(ServiceJetway?.SubOperating?.GetNumber() ?? 0);
        public int StairsState => (int)(ServiceStairs?.State ?? 0);
        public int StairsOperation => (int)(ServiceStairs?.SubOperating?.GetNumber() ?? 0);
        public virtual bool IsGateConnected => ServiceJetway.IsConnected || ServiceStairs.IsConnected;
        public virtual bool HasGateJetway => ServiceJetway.State != GsxServiceState.NotAvailable;
        public virtual bool HasGateStair => ServiceStairs.State != GsxServiceState.NotAvailable;
        public virtual bool HasUndergroundRefuel => ServiceRefuel?.IsUnderground == true;
        public virtual bool ServicesValid => ServiceStairs.State != GsxServiceState.Unknown || ServiceJetway.State != GsxServiceState.Unknown || !IsOnGround;
        public virtual bool IsDeiceAvail => SubDeiceAvail?.GetNumber() != 0;

        public virtual bool CouatlVarsValid { get; protected set; } = false;
        public virtual int CouatlLastProgress {  get; protected set; } = 0;
        public virtual int CouatlLastStarted { get; protected set; } = 0;
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
        public virtual bool CanAutomationRun => AircraftController.IsConnected && (Menu.FirstReadyReceived || IsAirStart);
        protected virtual int GroundCounter { get; set; } = 0;
        public virtual bool IsPaused => SimConnect.IsPaused;
        public virtual bool IsWalkaround => CheckWalkAround();
        public virtual bool SkippedWalkAround { get; protected set; } = false;
        public virtual bool WalkAroundSkipActive { get; protected set; } = false;
        public virtual bool WalkaroundNotified { get; protected set; } = false;
        public event Func<Task> WalkaroundPreAction;
        public event Func<Task> WalkaroundWasSkipped;

        public virtual ISimResourceSubscription SubDoorToggleExit1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit3 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleExit4 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleService1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleService2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo2 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo3 { get; protected set; }
        public virtual ISimResourceSubscription SubDefaultSmartButton { get; protected set; }
        public virtual ISimResourceSubscription SubPilotTarget { get; protected set; }
        public virtual ISimResourceSubscription SubCrewTarget { get; protected set; }
        public virtual ISimResourceSubscription SubDeiceAvail { get; protected set; }

        protected virtual void InitGsxServices()
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

        protected override Task InitReceivers()
        {
            SimStore.AddVariable(GsxConstants.VarCouatlStarted).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg7).OnReceived += OnCouatlVariable;

            SimStore.AddVariable("SIM ON GROUND", SimUnitType.Bool);
            if (IsMsfs2024)
            {
                SimStore.AddVariable("IS AIRCRAFT", SimUnitType.Number);
                SimStore.AddVariable("IS AVATAR", SimUnitType.Number);
            }
            MsgCouatlStarted = ReceiverStore.Add<MsgGsxCouatlStarted>();
            MsgCouatlStopped = ReceiverStore.Add<MsgGsxCouatlStopped>();

            SubDoorToggleExit1 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit1);
            SubDoorToggleExit2 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit2);
            SubDoorToggleExit3 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit3);
            SubDoorToggleExit4 = SimStore.AddVariable(GsxConstants.VarDoorToggleExit4);
            SubDoorToggleService1 = SimStore.AddVariable(GsxConstants.VarDoorToggleService1);
            SubDoorToggleService2 = SimStore.AddVariable(GsxConstants.VarDoorToggleService2);
            SubDoorToggleCargo1 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo1);
            SubDoorToggleCargo2 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo2);
            SubDoorToggleCargo3 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo3);

            SubPilotTarget = SimStore.AddVariable(GsxConstants.VarPilotTarget);
            SubCrewTarget = SimStore.AddVariable(GsxConstants.VarCrewTarget);

            SubDeiceAvail = SimStore.AddVariable(GsxConstants.VarDeiceAvail);

            SubDefaultSmartButton = SimStore.AddVariable(GenericSettings.VarSmartButtonDefault);
            SimStore.AddVariable(GsxConstants.VarSetAutoMode);

            InitGsxServices();
            Menu.Init();
            AutomationController.Init();

            return Task.CompletedTask;
        }

        protected virtual void OnCouatlVariable(ISimResourceSubscription sub, object data)
        {
            while (_lock && !Token.IsCancellationRequested) { }
            _lock = true;

            try
            {                
                CouatlLastStarted = (int)(SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0);
                CouatlLastProgress = (int)(SimStore[GsxConstants.VarCouatlStartProg7]?.GetNumber() ?? 0);
                if (CouatlLastStarted == 1 && CouatlLastProgress == 100)
                {
                    if (!CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables valid!");
                        CouatlVarsValid = true;
                        CouatlInvalidCount = 0;
                        SetCouatlConf();
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStarted>(this, true));
                        AutomationController.OnCouatlStarted();
                    }
                }
                else
                {
                    if (CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables NOT valid! (started: {CouatlLastStarted} / progress: {CouatlLastProgress})");
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
                        CouatlVarsValid = false;
                        AutomationController.OnCouatlStopped();
                    }
                }

                CouatlVarsReceived = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                _lock = false;
            }
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
                if (!Menu.FirstReadyReceived || (SimStore["IS AIRCRAFT"]?.GetNumber() != 0 && SimStore["IS AVATAR"]?.GetNumber() != 1))
                    await Task.Delay(Config.GsxServiceStartDelay, Token);

                if (IsMsfs2024 && SimConnect.CameraState == 30)
                {
                    Logger.Debug($"Checking MSFS 2024 Aircraft/Avatar Vars ...");
                    int count = 0;
                    while (SimStore["IS AIRCRAFT"]?.GetNumber() != 0 && SimStore["IS AVATAR"]?.GetNumber() != 1 && IsExecutionAllowed && !Token.IsCancellationRequested)
                    {
                        await Task.Delay(Config.TimerGsxCheck, Token);
                        count++;
                    }

                    if (count > 0)
                        await Task.Delay(Config.GsxServiceStartDelay / 2, Token);
                    Logger.Debug($"MSFS 2024 Aircraft/Avatar Vars valid");
                }

                AutomationController.Reset();
                Logger.Debug($"GsxService active (VarsReceived: {CouatlVarsReceived} | FirstReady: {Menu.FirstReadyReceived})");
                IsActive = true;
                Logger.Information($"GsxController active - waiting for Menu to be ready");
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !Token.IsCancellationRequested)
                {
                    if (Config.LogLevel == LogLevel.Verbose)
                        Logger.Verbose($"Controller Tick - VarsReceived: {CouatlVarsReceived} | FirstReady: {Menu.FirstReadyReceived} | VarsValid: {CouatlVarsValid} | IsGsxRunning: {IsGsxRunning}");
                    CheckGround();

                    if (!CouatlVarsReceived && IsProcessRunning)
                        OnCouatlVariable(null, null);

                    if (!SkippedWalkAround && !WalkAroundSkipActive)
                    {
                        if (IsOnGround)
                            _ = SkipWalkaround();
                        else
                            SkippedWalkAround = true;
                    }

                    if (!Menu.FirstReadyReceived && IsProcessRunning && NextMenuStartupCheck <= DateTime.Now && AutomationController.IsOnGround)
                    {
                        if (IsProcessRunning && Profile.RunAutomationService && !AutomationController.IsStarted && AircraftController.IsConnected && SkippedWalkAround && Aircraft.GroundSpeed < 1)
                        {
                            Logger.Debug($"HasChocks {Aircraft.HasChocks} | EquipmentChocks {Aircraft.EquipmentChocks} | IsBrakeSet {Aircraft.IsBrakeSet} | Type {Aircraft.GetType().Name}");
                            if (Aircraft.HasChocks && !Aircraft.EquipmentChocks)
                            {
                                Logger.Information($"GSX Menu not opening - trigger Chocks");
                                await Aircraft.SetEquipmentChocks(true, true);
                            }
                            else if (!Aircraft.HasChocks && !Aircraft.IsBrakeSet)
                            {
                                Logger.Information($"GSX Menu not opening - trigger Parking Brake");
                                await Aircraft.SetParkingBrake(true);
                            }
                        }

                        if (CouatlVarsReceived && CouatlLastStarted == 1 && IsProcessRunning)
                        {
                            Logger.Information($"Trying to open GSX Menu ...");
                            await Menu.OpenHide();
                            await Task.Delay(1000, Token);
                        }

                        if ((!CouatlVarsValid || !IsProcessRunning) && AppService.Instance.LastGsxRestart <= DateTime.Now - TimeSpan.FromSeconds(Config.WaitGsxRestart))
                        {
                            CouatlInvalidCount++;
                            Logger.Warning($"GSX Menu is not starting #{CouatlInvalidCount}");
                            if (CouatlInvalidCount >= Config.GsxMenuStartupMaxFail && Config.RestartGsxStartupFail)
                            {
                                Logger.Information($"Restarting GSX ...");
                                await AppService.Instance.RestartGsx();
                                CouatlInvalidCount = 0;
                                await Task.Delay(Config.GsxServiceStartDelay, App.Token);
                            }
                        }

                        NextMenuStartupCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxStartupMenuCheck);
                    }

                    if (AircraftController.IsConnected)
                    {
                        if (SkippedWalkAround && !WalkaroundNotified && AutomationController.IsStarted)
                        {
                            if (IsOnGround && AutomationController.State < AutomationState.Departure)
                                await TaskTools.RunLogged(async () => await WalkaroundWasSkipped?.Invoke());
                            WalkaroundNotified = true;
                        }

                        if (!AutomationController.IsStarted && CanAutomationRun)
                        {
                            await Task.Delay(1000);
                            _ = AutomationController.Run();
                        }
                    }

                    CheckProcess();

                    await Task.Delay(Config.TimerGsxCheck, Token);
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
                MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
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

        protected virtual void SetCouatlConf()
        {
            Logger.Debug($"Set GSX Settings");
            SimStore[GsxConstants.VarSetAutoMode].WriteValue(-1);
        }

        public virtual async Task ReloadSimbrief()
        {
            Logger.Information($"Refreshing GSX SimBrief/VDGS Data");
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(15, "", true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            await Menu.RunSequence(sequence);
        }

        public virtual async Task SetPaxBoard(int pax)
        {
            await ServiceBoard.SetPaxTarget(pax);
        }

        public virtual async Task SetPaxDeboard(int pax)
        {
            await ServiceDeboard.SetPaxTarget(pax);
        }

        protected virtual async Task SkipWalkaround()
        {
            WalkAroundSkipActive = true;
            bool notified = false;
#if DEBUG
            Logger.Verbose($"ac: {SimStore["IS AIRCRAFT"]?.GetNumber()} | av: {SimStore["IS AVATAR"]?.GetNumber()}");
#endif
            while (Profile.RunAutomationService && IsWalkaround && !SkippedWalkAround && Profile.SkipWalkAround && IsExecutionAllowed)
            {
                Logger.Information("Automation: Skip Walkaround");
                if (IsWalkaround && !notified)
                {
                    if (WalkaroundPreAction != null)
                        await TaskTools.RunLogged(async () => await WalkaroundPreAction?.Invoke());
                    notified = true;
                }
                await ToggleWalkaround();
                if (IsWalkaround)
                    await Task.Delay(Config.TimerGsxCheck, Token);

                SkippedWalkAround = (CheckSessionReady() && !IsWalkaround) || !Profile.RunAutomationService;
            }
            SkippedWalkAround = (CheckSessionReady() && !IsWalkaround) || !Profile.RunAutomationService;
            WalkAroundSkipActive = false;
        }

        public virtual async Task ToggleWalkaround()
        {
            Logger.Debug("Toggling Walkaround ...");
            string title = WindowTools.GetMsfsWindowTitle();
            Sys.SetForegroundWindow(title);
            await Task.Delay(Config.DelayForegroundChange, Token);
            string active = Sys.GetActiveWindowTitle();
            if (active == title)
            {
                Logger.Debug($"Sending Keystrokes");
                WindowTools.SendWalkaroundKeystroke();
                await Task.Delay(Config.DelayAircraftModeChange, Token);
            }
            else
                Logger.Debug($"Active Window did not match to '{title}'");
        }

        public override Task Stop()
        {
            AutomationController.Stop();
            Menu.Reset();

            base.Stop();

            foreach (var service in GsxServices)
                service.Value.ResetState();

            IsActive = false;
            SkippedWalkAround = false;
            WalkaroundNotified = false;
            WalkAroundSkipActive = false;
            CouatlVarsValid = false;
            CouatlVarsReceived = false;
            CouatlLastProgress = 0;
            CouatlLastStarted = 0;
            FirstGroundCheck = true;
            GroundCounter = 0;
            IsAirStart = false;
            NextMenuStartupCheck = DateTime.MinValue;

            return Task.CompletedTask;
        }

        protected override Task FreeResources()
        {
            foreach (var service in GsxServices)
                service.Value.FreeResources();

            Menu.FreeResources();
            AutomationController.FreeResources();

            SimStore.Remove(GenericSettings.VarSmartButtonDefault);
            SimStore.Remove(GsxConstants.VarDeiceAvail);
            SimStore.Remove(GsxConstants.VarCrewTarget);
            SimStore.Remove(GsxConstants.VarPilotTarget);
            SimStore.Remove(GsxConstants.VarSetAutoMode);
            SimStore.Remove(GsxConstants.VarDoorToggleExit1);
            SimStore.Remove(GsxConstants.VarDoorToggleExit2);
            SimStore.Remove(GsxConstants.VarDoorToggleExit3);
            SimStore.Remove(GsxConstants.VarDoorToggleExit4);
            SimStore.Remove(GsxConstants.VarDoorToggleService1);
            SimStore.Remove(GsxConstants.VarDoorToggleService2);
            SimStore.Remove(GsxConstants.VarDoorToggleCargo1);
            SimStore.Remove(GsxConstants.VarDoorToggleCargo2);
            SimStore.Remove(GsxConstants.VarDoorToggleCargo3);
            SimStore[GsxConstants.VarCouatlStarted].OnReceived -= OnCouatlVariable;
            SimStore[GsxConstants.VarCouatlStartProg7].OnReceived -= OnCouatlVariable;
            SimStore.Remove(GsxConstants.VarCouatlStarted);
            SimStore.Remove(GsxConstants.VarCouatlStartProg7);

            SimStore.Remove("SIM ON GROUND");
            if (IsMsfs2024)
            {
                SimStore.Remove("IS AIRCRAFT");
                SimStore.Remove("IS AVATAR");
            }
            ReceiverStore.Remove<MsgGsxCouatlStarted>();
            ReceiverStore.Remove<MsgGsxCouatlStopped>();

            return Task.CompletedTask;
        }
    }
}
