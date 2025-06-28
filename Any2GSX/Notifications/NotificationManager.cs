using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public enum SmartButtonCall
    {
        None = 0,
        Connect = 1,
        NextService = 2,
        PushCall = 3,
        PushStop = 4,
        PushConfirm = 5,
        Deice = 6,
        ClearGate = 7,
        Deboard = 8,
        SkipTurn = 9,
    }

    public class NotificationManager() : ServiceController<Any2GSX, AppService, Config, Definition>(AppService.Instance.Config)
    {
        public virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual bool HasEfbApp => SimConnect?.GetSimVersion() == SimVersion.MSFS2024;
        public virtual GsxController GsxController => AppService.Instance?.GsxController;
        public virtual GsxMenu GsxMenu => GsxController?.Menu;
        public virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        public virtual AircraftBase Aircraft => AppService.Instance?.AircraftController?.Aircraft;
        public virtual GsxAutomationController AutomationController => AppService.Instance?.GsxController?.AutomationController;
        public virtual AutomationState AutomationState => AutomationController.State;

        public virtual List<ExternalConnector> Connectors { get; protected set; } = [];
        public virtual PilotsDeckConnector DeckConnector { get; protected set; } = null;
        protected virtual bool NotifyUpdates => SimConnect?.IsSessionRunning == true && GsxController?.IsActive == true;
        public virtual DateTime ClearInhibitTime { get; protected set; } = DateTime.MinValue;
        public virtual string ReportedProfile { get; protected set; } = "";
        public virtual SmartButtonCall ReportedCall { get; protected set; } = SmartButtonCall.None;
        public virtual string ReportedCallInfo { get; protected set; } = "";
        public virtual AutomationState ReportedPhase { get; protected set; } = AutomationState.Unknown;
        public virtual string ReportedStatus { get; protected set; } = "";
        public virtual string LastCapturedGate { get; protected set; } = "";
        public virtual string ReportedCouatlVars { get; protected set; } = "false";
        public virtual int ReportedServicesCompleted { get; protected set; } = 0;
        public virtual int ReportedServicesRunning { get; protected set; } = 0;
        public virtual int ReportedServicesTotal { get; protected set; } = 0;
        public virtual bool LastServiceCount { get; protected set; } = false;

        protected override Task InitReceivers()
        {
            GsxController.ServiceBoard.OnStateChanged += BoardingOnStateChanged;
            GsxController.ServiceBoard.OnPaxChange += BoardingOnPaxChange;
            GsxController.ServiceBoard.OnCargoChange += BoardingOnCargoChange;
            GsxController.ServiceDeboard.OnStateChanged += DeboardingOnStateChanged;
            GsxController.ServiceDeboard.OnPaxChange += DeboardingOnPaxChange;
            GsxController.ServiceDeboard.OnCargoChange += DeboardingOnCargoChange;
            GsxController.ServiceDeice.OnStateChanged += DeiceOnStateChanged;

            GsxMenu.OnMenuReceived += OnMenuReceived;
            GsxController.AutomationController.OnStateChange += OnAutomationStateChange;

            return Task.CompletedTask;
        }

        protected override Task FreeResources()
        {
            GsxController.AutomationController.OnStateChange -= OnAutomationStateChange;
            GsxMenu.OnMenuReceived -= OnMenuReceived;

            GsxController.ServiceBoard.OnStateChanged -= BoardingOnStateChanged;
            GsxController.ServiceBoard.OnPaxChange -= BoardingOnPaxChange;
            GsxController.ServiceBoard.OnCargoChange -= BoardingOnCargoChange;
            GsxController.ServiceDeboard.OnStateChanged -= DeboardingOnStateChanged;
            GsxController.ServiceDeboard.OnPaxChange -= DeboardingOnPaxChange;
            GsxController.ServiceDeboard.OnCargoChange -= DeboardingOnCargoChange;
            GsxController.ServiceDeice.OnStateChanged -= DeiceOnStateChanged;

            return Task.CompletedTask;
        }

        protected override async Task DoRun()
        {
            try
            {
                if (Profile?.PilotsDeckIntegration == true)
                {
                    Logger.Debug($"Start PilotsDeck Connector");
                    var pilotsdeck = new PilotsDeckConnector();
                    await pilotsdeck.Start();
                    Connectors.Add(pilotsdeck);
                    DeckConnector = pilotsdeck;
                }

                if (HasEfbApp)
                {
                    Logger.Debug($"Start EFB App Connector");
                    var efb = new EfbAppConnector();
                    await efb.Start();
                    Connectors.Add(efb);
                }

                Logger.Debug("Waiting for Automation Controller");
                while (IsExecutionAllowed && AutomationController?.IsStarted == false)
                    await Task.Delay(Config.StateMachineInterval, Token);
                if (!IsExecutionAllowed)
                    return;

                Logger.Debug($"Notification Manager active");                
                await CallOnConnectors((connector) => connector.SetConnected(true, Profile.Name));
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
                while (IsExecutionAllowed)
                {
                    if (NotifyUpdates)
                    {
                        await EvaluateState();
                        await CheckMenu();
                    }
                    await Task.Delay(Config.StateMachineInterval, Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            finally
            {
                try { await CallOnConnectors((connector) => connector.SetConnected(false, "")); } catch { }
            }
        }

        public override async Task Stop()
        {
            try
            {
                await CallOnConnectors((connector) => connector.SetConnected(false, ""));
                await CallOnConnectors((connector) => connector.Stop());
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            Connectors.Clear();
            DeckConnector = null;
            
            await base.Stop();
        }

        protected virtual async Task CallOnConnectors(Func<ExternalConnector, Task> action)
        {
            foreach (var connector in Connectors)
                await action?.Invoke(connector);
        }

        protected virtual Task DeiceOnStateChanged(IGsxService service)
        {
            if (service.State == GsxServiceState.Completed)
                LastCapturedGate = "";

            return Task.CompletedTask;
        }

        public virtual Task OnMenuChangeParking(IGsxMenu menu)
        {
            try
            {
                string line = menu.MenuLines[0];
                Logger.Debug($"Trying to get Gate/Pad from Line '{line}'");
                if (GsxConstants.MenuRegexFacility.GroupMatches(line, 1, out var group))
                {
                    string parking = Regex.Replace(group, @"\([^\)]+\)", "").Trim();
                    parking = Regex.Replace(parking, @"\[[^\]]+\]", "").Trim();
                    if (AutomationController.State == AutomationState.TaxiOut)
                    {
                        LastCapturedGate = parking.Replace("deice", "", StringComparison.InvariantCultureIgnoreCase).Replace("de-ice", "", StringComparison.InvariantCultureIgnoreCase).Trim();
                        Logger.Debug($"Capture set to '{LastCapturedGate}'");
                    }
                    else
                    {
                        var matches = GsxConstants.MenuRegexGate.Matches(parking);
                        if (matches.Count > 0)
                        {
                            LastCapturedGate = matches[0].Value.Trim();
                            Logger.Debug($"Capture set to '{LastCapturedGate}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                LastCapturedGate = "";
            }

            return Task.CompletedTask;
        }

        protected virtual async Task EvaluateState()
        {
            SmartButtonCall call = ReportedCall;
            string callInfo = "";
            AutomationState phase = ReportedPhase;
            string status = ReportedStatus;
            int svcCompleted = 0;
            int svcRunning = 0;
            int svcTotal = 0;
            bool svcCounted = false;
            string couatlVars = ReportedCouatlVars;

            if (AutomationState == AutomationState.SessionStart)
            {
                call = SmartButtonCall.None;
                phase = AutomationState;
                status = "";
            }
            else if (AutomationState == AutomationState.Preparation)
            {
                call = SmartButtonCall.None;
                phase = AutomationState;
                status = "";

                if (AutomationController.ExecutedReposition && GsxController.SkippedWalkAround && AutomationController.GroundEquipmentPlaced)
                    status = "Trigger Departure!";
                else if (((GsxController.HasGateJetway && !GsxController.ServiceJetway.IsConnected && !GsxController.ServiceJetway.IsOperating)
                    || (GsxController.HasGateStair && !GsxController.ServiceStairs.IsConnected && !GsxController.ServiceStairs.IsOperating))
                    && !GsxController.ServiceJetway.IsConnected && !GsxController.ServiceStairs.IsConnected)
                    call = SmartButtonCall.Connect;

                if (GsxController.ServiceJetway.IsOperating)
                    status = "Operating Jetway ...";
                if (GsxController.ServiceStairs.IsOperating && GsxController.ServiceStairs.State != GsxServiceState.Active)
                    status = "Operating Stairs ...";

                if (GsxController.ServiceReposition.IsCalling)
                    status = "Reposition Aircraft ...";
            }
            else if (AutomationState == AutomationState.Departure)
            {
                call = AutomationController.DepartureServicesCompleted || !AutomationController.DepartureServicesEnumerator.CheckEnumeratorValid() ? SmartButtonCall.None : SmartButtonCall.NextService;
                phase = AutomationState;
                status = "";

                foreach (var serviceCfg in Profile.DepartureServices.Values)
                {
                    if (serviceCfg.ServiceActivation == GsxServiceActivation.Skip)
                        continue;

                    if (GsxController.GsxServices[serviceCfg.ServiceType].IsActive)
                    {
                        status = $"{serviceCfg.ServiceType} active ...";
                        svcRunning++;
                    }
                    else if (GsxController.GsxServices[serviceCfg.ServiceType].IsRunning)
                        svcRunning++;
                    else if (GsxController.GsxServices[serviceCfg.ServiceType].IsCompleted || GsxController.GsxServices[serviceCfg.ServiceType].IsSkipped)
                        svcCompleted++;

                    svcTotal++;
                }
                svcCounted = true;

                if (GsxController.ServiceJetway.IsOperating)
                    status = "Operating Jetway ...";
                if (GsxController.ServiceStairs.IsOperating && GsxController.ServiceStairs.State != GsxServiceState.Active)
                    status = "Operating Stairs ...";

                if (GsxController.ServiceRefuel.IsActive)
                {
                    if (!GsxController.ServiceRefuel.IsHoseConnected && !GsxController.ServiceRefuel.WasHoseConnected)
                        status = "Connecting Hose ...";
                    else if (GsxController.ServiceRefuel.IsHoseConnected && GsxController.ServiceRefuel.WasHoseConnected)
                        status = "Loading Fuel ...";
                    else if (!GsxController.ServiceRefuel.IsHoseConnected && GsxController.ServiceRefuel.WasHoseConnected)
                        status = "Removing Hose ...";
                }
                else if (GsxController.ServiceBoard.IsActive)
                {
                    if (AircraftController.Aircraft.IsCargo)
                        status = "Loading ...";
                    else
                        status = "Boarding ...";
                }

                if (!AutomationController.IsFinalReceived && AutomationController.FinalDelay > 0)
                    status = $"Final in {AutomationController.FinalDelay}s";

                if (call == SmartButtonCall.NextService && AutomationController.DepartureServicesEnumerator.CheckEnumeratorValid())
                    callInfo = AutomationController.DepartureServicesCurrent.ServiceType.ToString();
            }
            else if (AutomationState == AutomationState.Pushback)
            {
                call = GsxController.ServicePushBack.IsCompleted || GsxController.ServicePushBack.IsRunning ? SmartButtonCall.None : SmartButtonCall.PushCall;
                phase = AutomationState;
                status = "";

                if (!AutomationController.IsFinalReceived && AutomationController.FinalDelay > 0)
                    status = $"Final in {AutomationController.FinalDelay}s";

                if (GsxController.ServiceJetway.IsOperating)
                    status = "Operating Jetway ...";
                if (GsxController.ServiceStairs.IsOperating && GsxController.ServiceStairs.State != GsxServiceState.Active)
                    status = "Operating Stairs ...";

                if (GsxController.ServicePushBack.IsRunning)
                {
                    double value = GsxController.ServicePushBack.PushStatus;
                    if (value == 1)
                        status = "Insert Pin ...";
                    else if (value == 2)
                        status = "Locking Gear ...";
                    else if (value == 3 || value == 4)
                        status = "Direction?";
                    else if (value == 6)
                        status = "Pushing Back ...";
                    else if (value == 7)
                        status = "Set Brake!";
                    else if (value == 8)
                        status = "Confirm?";
                    else if (value == 5 || value == 9)
                        status = "Unlocking Gear ...";
                    else if (value >= 10)
                        status = "Show Pin ...";

                    if (value >= 6 && value < 8)
                        call = SmartButtonCall.PushStop;
                    else if (value == 8)
                        call = SmartButtonCall.PushConfirm;
                }
            }
            else if (AutomationState == AutomationState.TaxiOut)
            {
                call = SmartButtonCall.None;
                phase = AutomationState;
                status = "";

                if (!string.IsNullOrWhiteSpace(LastCapturedGate) && !GsxController.ServiceDeice.IsRunning)
                {
                    status = $"[ {LastCapturedGate} ]";
                    if (GsxMenu.IsGateMenu && GsxMenu.MenuLines[0]!?.StartsWith(GsxConstants.MenuRequestDeice, StringComparison.InvariantCultureIgnoreCase) == true)
                        call = SmartButtonCall.Deice;
                }
                else if (GsxController.ServiceDeice.IsRunning)
                    status = $"{GsxServiceType.Deice} active ...";                
            }
            else if (AutomationState > AutomationState.TaxiOut && AutomationState <= AutomationState.TaxiIn)
            {
                call = SmartButtonCall.None;
                phase = AutomationState;
                status = "";

                if (AutomationState == AutomationState.TaxiIn && !string.IsNullOrWhiteSpace(LastCapturedGate))
                {
                    status = $"[ {LastCapturedGate} ]";
                    call = SmartButtonCall.ClearGate;
                }
            }
            else if(AutomationState == AutomationState.Arrival)
            {
                call = GsxController.ServiceDeboard.IsRunning ? SmartButtonCall.None : SmartButtonCall.Deboard;
                phase = AutomationState;
                status = "";

                if (Aircraft.HasChocks && !Aircraft.EquipmentChocks && AutomationController.ChockDelay > 0)
                    status = $"Chocks in {AutomationController.ChockDelay}s";
                else if (GsxController.ServiceJetway.IsOperating || GsxController.ServiceStairs.IsOperating)
                {
                    if (GsxController.ServiceJetway.IsOperating)
                        status = "Operating Jetway ...";
                    if (GsxController.ServiceStairs.IsOperating && GsxController.ServiceStairs.State != GsxServiceState.Active)
                        status = "Operating Stairs ...";
                }
                else if (GsxController.ServiceDeboard.IsActive)
                {
                    if (AircraftController.Aircraft.IsCargo)
                        status = "Unloading ...";
                    else
                        status = "Deboarding ...";
                }
            }
            else if (AutomationState == AutomationState.TurnAround)
            {
                call = SmartButtonCall.SkipTurn;
                phase = AutomationState;
                status = "";

                if (AutomationController.InitialTurnDelay)
                    status = $"Turn-Delay {(AutomationController.TimeNextTurnCheck - DateTime.Now).Seconds}s ...";
                else if (!GsxController.IsGateConnected)
                    status = $"Connect Jetway/Stairs!";
                else if (!AutomationController.InitialTurnDelay && !AppService.Instance.Flightplan.LastOnlineCheck)
                    status = $"Create new OFP!";
                else if (!Aircraft.ReadyForDepartureServices)
                    status = "Trigger Departure!";
            }

            if (GsxController.CouatlVarsValid)
                couatlVars = "true";
            else
                couatlVars = "false";

            if (ReportedPhase != phase || ReportedStatus != status)
            {
                await CallOnConnectors((connector) => connector.SetState(phase, status));
                ReportedPhase = phase;
                ReportedStatus = status;
            }

            if (ReportedCouatlVars != couatlVars)
            {
                await CallOnConnectors((connector) => connector.SetCouatlVars(couatlVars));
                ReportedCouatlVars = couatlVars;
            }

            if (ReportedCall != call || ReportedCallInfo != callInfo)
            {
                await CallOnConnectors((connector) => connector.SetSmartCall(call, callInfo));
                ReportedCall = call;
                ReportedCallInfo = callInfo;
            }

            if (!svcCounted && LastServiceCount != svcCounted)
                await CallOnConnectors((connector) => connector.ClearDepartureServices());
            else if ((ReportedServicesCompleted != svcCompleted)
                || (ReportedServicesRunning != svcRunning)
                || (ReportedServicesTotal != svcTotal))
            {
                await CallOnConnectors((connector) => connector.SetDepartureServices(svcCompleted, svcRunning, svcTotal));
                ReportedServicesCompleted = svcCompleted;
                ReportedServicesRunning = svcRunning;
                ReportedServicesTotal = svcTotal;
            }

            if (ReportedProfile != (Profile?.Name ?? ""))
            {
                await CallOnConnectors((connector) => connector.SetConnected(true, Profile?.Name ?? ""));
                ReportedProfile = Profile?.Name ?? "";
            }
        }

        protected virtual async Task CheckMenu()
        {
            try
            {
                var automation = GsxController.AutomationController.State;
                var now = DateTime.Now;
                if ((Profile.PilotsDeckIntegration || Config.RefreshMenuForEfb) && !GsxMenu.SuppressMenuRefresh && !GsxMenu.IsSequenceActive && ClearInhibitTime <= DateTime.Now
                    && !(automation == AutomationState.SessionStart || automation == AutomationState.Pushback || automation == AutomationState.Flight))
                {
                    if (Profile.PilotsDeckIntegration)
                        await DeckConnector.SetConnected(true, Profile.Name);
                    Logger.Debug($"PilotsDeck Integration: Refresh Menu (After Clear)");
                    await GsxMenu.OpenHide();
                }

                if (automation == AutomationState.TaxiOut && !string.IsNullOrWhiteSpace(LastCapturedGate)
                    && !GsxMenu.SuppressMenuRefresh && GsxMenu.MenuState == GsxMenuState.TIMEOUT
                    && Aircraft.IsBrakeSet && Aircraft.GroundSpeed < Config.SpeedTresholdTaxiOut && !GsxController.ServiceDeice.IsRunning)
                {
                    Logger.Debug($"Open Menu to for Deice Pad ({AppService.Instance?.NotificationManager?.LastCapturedGate})");
                    await GsxMenu.OpenHide();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual async void OnMenuReceived(IGsxMenu menu)
        {
            if (!NotifyUpdates)
                return;

            if (GsxMenu.MenuState == GsxMenuState.READY || GsxMenu.MenuState == GsxMenuState.HIDE)
            {
                ClearInhibitTime = DateTime.MaxValue;
                await CallOnConnectors((connector) => connector.SetMenuTitle(GsxMenu.MenuTitle));
                await CallOnConnectors((connector) => connector.SetMenuLines(GsxMenu.MenuLines));
            }
            else
                await ClearMenu(GsxMenu.MenuState == GsxMenuState.TIMEOUT);

            if (GsxMenu.MenuState == GsxMenuState.TIMEOUT && (Profile.PilotsDeckIntegration || Config.RefreshMenuForEfb))
            {
                var automation = GsxController.AutomationController.State;
                if ((GsxController.ServicePushBack.PushStatus == 0 || GsxController.ServicePushBack.State < GsxServiceState.Requested) && !GsxMenu.SuppressMenuRefresh && !GsxMenu.IsSequenceActive
                    && (automation == AutomationState.Preparation || automation == AutomationState.Departure || automation == AutomationState.Pushback || automation == AutomationState.Arrival || automation == AutomationState.TurnAround))
                {
                    Logger.Debug($"PilotsDeck Integration: Refresh Menu (Timeout)");
                    await GsxMenu.OpenHide();
                }
            }
        }

        public virtual async Task ClearMenu(bool setTime)
        {
            if (!NotifyUpdates)
                return;

            if (setTime)
                ClearInhibitTime = DateTime.Now + TimeSpan.FromMilliseconds(Config.DeckClearedMenuRefresh);
            await CallOnConnectors((connector) => connector.SetMenuTitle(""));
            await CallOnConnectors((connector) => connector.SetMenuLines([]));
        }

        protected virtual async Task OnAutomationStateChange(AutomationState state)
        {
            if (!NotifyUpdates)
                return;

            if (state == AutomationState.TurnAround)
            {
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
            }
        }

        protected virtual async Task BoardingOnStateChanged(IGsxService gsxService)
        {
            if (!NotifyUpdates)
                return;

            if (gsxService.State == GsxServiceState.Active)
            {
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(0));
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(0));
            }
            else
            {
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
            }
        }

        protected virtual async Task BoardingOnPaxChange(GsxServiceBoarding gsxService)
        {
            if (!NotifyUpdates)
                return;

            if (gsxService.State != GsxServiceState.Active)
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
            else
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(gsxService.PaxTotal));
        }

        protected virtual async Task BoardingOnCargoChange(GsxServiceBoarding gsxService)
        {
            if (!NotifyUpdates || gsxService.State != GsxServiceState.Active)
                return;

            if (gsxService.State != GsxServiceState.Active)
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
            else
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(gsxService.CargoPercent));
        }

        protected virtual async Task DeboardingOnStateChanged(IGsxService gsxService)
        {
            if (!NotifyUpdates || gsxService.State != GsxServiceState.Active)
                return;

            if (gsxService.State == GsxServiceState.Active)
            {
                var target = (gsxService as GsxServiceDeboarding).PaxTarget;
                if (target <= 0)
                    target = AppService.Instance.Flightplan.CountPax;
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(target));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(100));
            }
            else
            {
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
            }
        }

        protected virtual async Task DeboardingOnPaxChange(GsxServiceDeboarding gsxService)
        {
            if (!NotifyUpdates || gsxService.State != GsxServiceState.Active)
                return;

            if (gsxService.State != GsxServiceState.Active)
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
            else
            {
                var target = gsxService.PaxTarget;
                if (target <= 0)
                    target = AppService.Instance.Flightplan.CountPax;
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(target - gsxService.PaxTotal));
            }
        }

        protected virtual async Task DeboardingOnCargoChange(GsxServiceDeboarding gsxService)
        {
            if (!NotifyUpdates || gsxService.State != GsxServiceState.Active)
                return;

            if (gsxService.State != GsxServiceState.Active)
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
            else
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(100 - gsxService.CargoPercent));
        }
    }
}
