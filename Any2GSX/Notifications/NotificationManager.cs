using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.GSX.Automation;
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public class NotificationManager() : ServiceController<Any2GSX, AppService, Config, Definition>(AppService.Instance.Config)
    {
        public virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        public virtual SimConnectManager SimConnect => AppService.Instance?.SimConnect;
        public virtual bool HasEfbApp => SimConnect?.GetSimVersion() == SimVersion.MSFS2024;
        public virtual GsxController GsxController => AppService.Instance?.GsxController;
        public virtual GsxMenu GsxMenu => GsxController?.Menu;
        public virtual NotificationTracker Tracker => AppService.Instance?.NotificationTracker;
        public virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        public virtual AircraftBase Aircraft => AppService.Instance?.AircraftController?.Aircraft;
        public virtual GsxAutomationController AutomationController => AppService.Instance?.GsxController?.AutomationController;
        public virtual DepartureServiceQueue DepartureQueue => AutomationController?.DepartureQueue;
        public virtual AutomationState AutomationState => AutomationController.State;
        protected virtual GsxServiceJetway ServiceJetway => GsxController.ServiceJetway;
        protected virtual GsxServiceStairs ServiceStairs => GsxController.ServiceStairs;
        protected virtual GsxServicePushback ServicePushBack => GsxController.ServicePushBack;
        protected virtual GsxServiceBoarding ServiceBoard => GsxController.ServiceBoard;
        protected virtual GsxServiceDeboarding ServiceDeboard => GsxController.ServiceDeboard;
        protected virtual GsxServiceRefuel ServiceRefuel => GsxController.ServiceRefuel;
        protected virtual GsxServiceDeice ServiceDeice => GsxController.ServiceDeice;

        public virtual ConcurrentDictionary<ExternalConnector, bool> Connectors { get; protected set; } = [];
        public virtual PilotsDeckConnector DeckConnector { get; protected set; } = null;
        protected virtual bool IsSessionRunning => SimConnect?.IsSessionRunning == true;
        protected virtual bool NotifyUpdates => IsSessionRunning && GsxController?.IsGsxRunning == true;
        public virtual bool MenuOpenQueued => MenuOpenDelayed != DateTime.MaxValue;
        public virtual DateTime MenuOpenDelayed { get; set; } = DateTime.MaxValue;

        public virtual bool ReportedAircraftConnected { get; protected set; } = false;
        public virtual string ReportedProfile { get; protected set; } = "";
        public virtual string ReportedCouatlVars { get; protected set; } = "false";
        public virtual int ReportedServicesCompleted { get; protected set; } = 0;
        public virtual int ReportedServicesRunning { get; protected set; } = 0;
        public virtual int ReportedServicesTotal { get; protected set; } = 0;
        public virtual string LastPushInfo { get; set; } = "";

        public static readonly Dictionary<GsxChangePark, string> ClearGateOptions = new()
        {
            { GsxChangePark.ChangeFacility, "Change Facility" },
            { GsxChangePark.FollowMe, "Request FollowMe" },
            { GsxChangePark.ProgTaxi, "Progressive Taxi" },
            { GsxChangePark.TowIn, "Push-In Towing" },
            { GsxChangePark.Revoke, "Revoke Services" },
            { GsxChangePark.ClearAI, "Remove AI" },
            { GsxChangePark.Warp, "Warp to Gate" },
            { GsxChangePark.ShowMe, "Show Gate" },
            { GsxChangePark.Map, "Moving Map" },
        };

        public static readonly Dictionary<GsxStopPush, string> StopPushOptions = new()
        {
            { GsxStopPush.Pause, "Pause" },
            { GsxStopPush.Stop, "Stop" },
            { GsxStopPush.Abort, "Abort" },
        };

        protected override Task DoInit()
        {
            ServiceBoard.OnStateChanged += OnBoardingStateChanged;
            ServiceBoard.OnPaxChange += OnBoardingPaxChanged;
            ServiceBoard.OnCargoChange += OnBoardingCargoChanged;
            ServiceDeboard.OnStateChanged += OnDeboardingStateChanged;
            ServiceDeboard.OnPaxChange += OnDeboardingPaxChanged;
            ServiceDeboard.OnCargoChange += OnDeboardingCargoChanged;
            ServiceDeice.OnStateChanged += OnDeiceStateChanged;
            ServicePushBack.OnStateChanged += OnPushbackStateChanged;
            ServicePushBack.OnPushStatus += OnPushStatusChanged;
            ServicePushBack.OnBypassPin += OnPushPinChanged;
            ServiceJetway.OnStateChanged += OnJetwayStateChanged;
            ServiceJetway.OnOperationChanged += OnJetwayOperationChanged;
            ServiceStairs.OnStateChanged += OnStairsStateChanged;
            ServiceStairs.OnOperationChanged += OnStairsOperationChanged;
            ServiceRefuel.OnStateChanged += OnRefuelStateChanged;
            ServiceRefuel.OnHoseConnection += OnRefuelHoseConnection;

            GsxMenu.OnMenuReceived += OnMenuReceived;
            GsxController.AutomationController.OnStateChange += OnAutomationStateChanged;

            return Task.CompletedTask;
        }

        protected override Task DoCleanup()
        {
            try
            {
                MenuOpenDelayed = DateTime.MaxValue;

                GsxController.AutomationController.OnStateChange -= OnAutomationStateChanged;
                GsxMenu.OnMenuReceived -= OnMenuReceived;

                ServiceBoard.OnStateChanged -= OnBoardingStateChanged;
                ServiceBoard.OnPaxChange -= OnBoardingPaxChanged;
                ServiceBoard.OnCargoChange -= OnBoardingCargoChanged;
                ServiceDeboard.OnStateChanged -= OnDeboardingStateChanged;
                ServiceDeboard.OnPaxChange -= OnDeboardingPaxChanged;
                ServiceDeboard.OnCargoChange -= OnDeboardingCargoChanged;
                ServiceDeice.OnStateChanged -= OnDeiceStateChanged;
                ServicePushBack.OnStateChanged -= OnPushbackStateChanged;
                ServicePushBack.OnPushStatus -= OnPushStatusChanged;
                ServicePushBack.OnBypassPin -= OnPushPinChanged;
                ServiceJetway.OnStateChanged -= OnJetwayStateChanged;
                ServiceJetway.OnOperationChanged -= OnJetwayOperationChanged;
                ServiceStairs.OnStateChanged -= OnStairsStateChanged;
                ServiceStairs.OnOperationChanged -= OnStairsOperationChanged;
                ServiceRefuel.OnStateChanged -= OnRefuelStateChanged;
                ServiceRefuel.OnHoseConnection -= OnRefuelHoseConnection;
            }
            catch { }

            return Task.CompletedTask;
        }

        protected override async Task DoRun()
        {
            try
            {
                if (Profile?.PilotsDeckIntegration == true && Sys.GetProcessRunning(PilotsDeckConnector.BinaryPlugin))
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

                await Task.Delay(1000, Token);
                Logger.Debug($"Notification Manager active");
                await ResetConnectors();

                Logger.Debug("Waiting for Automation Controller");
                while (IsExecutionAllowed && AutomationController?.IsStarted == false)
                    await Task.Delay(Config.StateMachineInterval, Token);
                if (!IsExecutionAllowed)
                    return;

                while (IsExecutionAllowed)
                {
                    if (IsSessionRunning)
                    {
                        Tracker.CheckNotifications();
                        await CallOnConnectors((connector) => connector.CheckState());
                        await UpdateState();
                        if (GsxController?.IsGsxRunning == true)
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

        protected virtual async Task ResetConnectors(bool checkAircraft = false)
        {
            Logger.Debug("Reset Connectors");
            ReportedProfile = Profile.Name;
            ReportedAircraftConnected = checkAircraft && AircraftController.IsConnected;
            ReportedCouatlVars = GsxController.CouatlVarsValid ? "true" : "false";
            ReportedServicesCompleted = 0;
            ReportedServicesRunning = 0;
            ReportedServicesTotal = 0;
            LastPushInfo = "";

            await CallOnConnectors((connector) => connector.SetConnected(true, ReportedProfile));
            await CallOnConnectors((connector) => connector.SetAircraftConnected(ReportedAircraftConnected));
            await CallOnConnectors((connector) => connector.SetCouatlVars(ReportedCouatlVars));
            await CallOnConnectors((connector) => connector.SetMenuTitle(""));
            await CallOnConnectors((connector) => connector.SetMenuLines([]));
            await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
            await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
            await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
            await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
            await CallOnConnectors((connector) => connector.SetSmartCall(SmartButtonCall.None, "", true));
            await Task.Delay(50);
            await CallOnConnectors((connector) => connector.SetState(AutomationState.SessionStart, new Notification(AppNotification.Loading)));
            await CallOnConnectors((connector) => connector.SetDepartureServices(ReportedServicesCompleted, ReportedServicesRunning, ReportedServicesTotal));
        }

        public override async Task Stop()
        {
            try
            {
                await ResetConnectors();
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
            var connectors = Connectors.Keys.ToArray();
            foreach (var connector in connectors)
            {
                try
                {
                    await action?.Invoke(connector);
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                        Logger.Error($"Error '{ex.GetType().Name}' on Connector {connector?.GetType()?.Name}: {ex.Message}");
                }
            }
        }

        protected virtual async Task UpdateState()
        {
            AutomationState phase = GsxController.AutomationState;

            //Phase & Status Display
            if (ServicePushBack.IsRunning && ServicePushBack.PushStatus > 0 && (Tracker.Current.Id != AppNotification.PushPhase || !Tracker.Current.HasMessage) && !string.IsNullOrWhiteSpace(LastPushInfo))
                Tracker.TrackMessage(AppNotification.PushPhase, LastPushInfo);
            await CallOnConnectors((connector) => connector.SetState(phase, Tracker.Current));

            //Couatl State
            string couatlVars;
            if (GsxController.CouatlVarsValid)
                couatlVars = "true";
            else
                couatlVars = "false";
            if (ReportedCouatlVars != couatlVars)
                await CallOnConnectors((connector) => connector.SetCouatlVars(couatlVars));
            ReportedCouatlVars = couatlVars;

            //SmartButton Info
            if (Profile.RunAutomationService)
            {
                if (Profile.RunAutomationService && Tracker.SmartButton == SmartButtonCall.NextService && AutomationController.DepartureQueue.HasNext)
                    await CallOnConnectors((connector) => connector.SetSmartCall(Tracker.SmartButton, AutomationController.NextType));
                else if (Profile.RunAutomationService && Tracker.SmartButton == SmartButtonCall.ClearGate)
                    await CallOnConnectors((connector) => connector.SetSmartCall(Tracker.SmartButton, Profile.ClearGateMenuOption));
                else if (Profile.RunAutomationService && Tracker.SmartButton == SmartButtonCall.Deice)
                {
                    string callInfo = (ServiceDeice.IsActive ? "Stop" : "Start");
                    if (ServiceDeice.IsCompleted)
                        callInfo = "Done";
                    await CallOnConnectors((connector) => connector.SetSmartCall(Tracker.SmartButton, callInfo));
                }
                else if (Profile.RunAutomationService && Tracker.SmartButton == SmartButtonCall.PushStop)
                    await CallOnConnectors((connector) => connector.SetSmartCall(Tracker.SmartButton, Profile.StopPushMenuOption.ToString()));
                else
                    await CallOnConnectors((connector) => connector.SetSmartCall(Tracker.SmartButton, ""));
            }

            //Departue Queue Info
            if (Profile.RunAutomationService)
            {
                if (AutomationState != AutomationState.Departure && AutomationState != AutomationState.Arrival)
                    await CallOnConnectors((connector) => connector.ClearDepartureServices());
                else if ((ReportedServicesCompleted != DepartureQueue.CountCompleted)
                        || (ReportedServicesRunning != DepartureQueue.CountRunning)
                        || (ReportedServicesTotal != DepartureQueue.CountTotal))
                    await CallOnConnectors((connector) => connector.SetDepartureServices(DepartureQueue.CountCompleted, DepartureQueue.CountRunning, DepartureQueue.CountTotal));
            }
            ReportedServicesCompleted = DepartureQueue.CountCompleted;
            ReportedServicesRunning = DepartureQueue.CountRunning;
            ReportedServicesTotal = DepartureQueue.CountTotal;

            //Connection and Profile
            if (ReportedProfile != (Profile?.Name ?? ""))
                await CallOnConnectors((connector) => connector.SetConnected(true, Profile?.Name ?? ""));
            ReportedProfile = Profile?.Name ?? "";

            //Aircraft Connection
            if (ReportedAircraftConnected != AircraftController.IsConnected)
                await CallOnConnectors((connector) => connector.SetAircraftConnected(AircraftController.IsConnected));
            ReportedAircraftConnected = AircraftController.IsConnected;

            await CallOnConnectors((connector) => { connector.NeedsRefresh = false; return Task.CompletedTask; });
        }

        protected virtual async Task CheckMenu()
        {
            try
            {
                var automation = AutomationController.State;
                var deckRefresh = (Profile.PilotsDeckRefresh || Config.RefreshMenuForEfb) && GsxMenu.MenuCommandsAllowed;
                var autoRefresh = (Profile.RunAutomationService || deckRefresh) && GsxMenu.MenuCommandsAllowed;
                var now = DateTime.Now;
                var isTimeout = GsxMenu.LastTimeout < DateTime.MaxValue && (Config.DeckClearedMenuRefresh == 0 || GsxMenu.LastTimeout + TimeSpan.FromMilliseconds(Config.DeckClearedMenuRefresh) <= now) && GsxMenu.MenuCommandsAllowed;
                var noInhibit = GsxMenu.LastSelectionTime < DateTime.MaxValue && (Config.DeckClearedMenuRefresh == 0 || GsxMenu.LastSelectionTime + TimeSpan.FromMilliseconds(Config.DeckClearedMenuRefresh) <= now) && GsxMenu.MenuCommandsAllowed && GsxMenu.LastMenuSelection == -2;

                //Request for delayed/timed Menu Open
                if (MenuOpenQueued)
                {
                    if (MenuOpenDelayed <= now)
                    {
                        if (!GsxMenu.ReadyReceived && GsxMenu.MenuCommandsAllowed)
                        {
                            Logger.Debug($"Menu Refresh: Open Menu delayed");
                            await GsxController.Menu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection || (GsxMenu.IsToolbarEnabled && !Config.DisableUserEnabledMenu));
                        }
                        MenuOpenDelayed = DateTime.MaxValue;
                    }
                }
                //Timeout Handling
                else if ((autoRefresh || deckRefresh) && isTimeout)
                {
                    //Keep Direction Menu open
                    if (autoRefresh && Profile.KeepDirectionMenuOpen && ServicePushBack.IsWaitingForDirection && ServicePushBack.State == GsxServiceState.Callable && automation == AutomationState.Pushback)
                    {
                        Logger.Debug($"Menu Refresh: Reopen Direction Menu");
                        await ServicePushBack.Call();
                        GsxMenu.ExternalSequence = true;
                        await GsxMenu.WaitInterval(5);

                        if (GsxMenu.IsReady && GsxMenu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && GsxMenu.MatchMenuLine(2, GsxConstants.MenuPushbackChange))
                            await GsxMenu.RunCommand(GsxMenuCommand.Select(3), Profile.EnableMenuForSelection || GsxController.IsDeiceAvail || GsxController.Menu.IsToolbarEnabled);

                        await GsxMenu.WaitInterval(2);
                        GsxMenu.ExternalSequence = false;
                    }
                    //Deck/EFB: Open after Timeout on Gate
                    else if (deckRefresh &&
                                (((GsxMenu.IsGateMenu || GsxMenu.LastTitle?.StartsWith(GsxConstants.MenuGate, StringComparison.InvariantCultureIgnoreCase) == true) && (automation <= AutomationState.Departure || automation >= AutomationState.Arrival))
                                || (automation == AutomationState.Pushback && !ServicePushBack.IsTugConnected && !ServicePushBack.IsRunning && !ServiceDeice.IsRunning)))
                    {
                        Logger.Debug($"Menu Refresh: After Timeout");
                        await GsxController.Menu.RunCommand(GsxMenuCommand.Open(), false);
                    }
                }
                //Deck/EFB: Open after Selection
                else if (deckRefresh && noInhibit && !ServiceDeice.IsRunning && !ServicePushBack.IsTugConnected && !ServicePushBack.IsRunning)
                {
                    Logger.Debug($"Menu Refresh: After Selection");
                    await GsxController.Menu.RunCommand(GsxMenuCommand.Open(), false);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual async Task OnMenuReceived(IGsxMenu menu)
        {
            try
            {
                if (!IsSessionRunning)
                    return;

                if (GsxMenu.MenuState == GsxMenuState.READY)
                {
                    await CallOnConnectors((connector) => connector.SetMenuTitle(GsxMenu.MenuTitle));
                    await CallOnConnectors((connector) => connector.SetMenuLines(GsxMenu.MenuLines));
                    await OnMenuTitle(menu);
                }
                else
                    await ClearMenu();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual async Task ClearMenu()
        {
            if (!IsSessionRunning)
                return;

            await CallOnConnectors((connector) => connector.SetMenuTitle(""));
            await CallOnConnectors((connector) => connector.SetMenuLines([]));
        }

        protected virtual Task OnMenuTitle(IGsxMenu menu)
        {
            if (ServicePushBack.IsWaitingForDirection && !Tracker.IsActive(AppNotification.PushReleaseBrake) && (menu.MatchTitle(GsxConstants.MenuPushbackDirection) || menu.MatchTitle(GsxConstants.MenuPushbackChange)))
            {
                LastPushInfo = "Direction?";
                Tracker.TrackMessage(AppNotification.PushPhase, LastPushInfo);
            }
            else if (Profile.AttachTugDuringBoarding == 0 && menu.MatchTitle(GsxConstants.MenuTugAttach))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Tug?");
            else if (!Profile.OperatorAutoSelect && (menu.MatchTitle(GsxConstants.MenuOperatorHandling) || menu.MatchTitle(GsxConstants.MenuOperatorCater)))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Operator?");
            else if (Profile.AnswerCrewBoardQuestion == 0 && menu.MatchTitle(GsxConstants.MenuBoardCrew))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Crew?");
            else if (Profile.AnswerCrewDeboardQuestion == 0 && menu.MatchTitle(GsxConstants.MenuDeboardCrew))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Crew?");
            else if (!menu.DeiceGateQuestionAnswered && menu.MatchTitle(GsxConstants.MenuDeiceOnPush))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "De-Ice?");
            else if (menu.MatchTitle(GsxConstants.MenuFollowMe))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Follow-Me?");
            else if (menu.MenuTitle.Contains("fluid", StringComparison.InvariantCultureIgnoreCase))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Fluid?");
            else if (menu.MenuTitle.Contains("concentration", StringComparison.InvariantCultureIgnoreCase))
                Tracker.TrackMessage(AppNotification.GsxQuestion, "Concentration?");
            else
                Tracker.Clear(AppNotification.GsxQuestion);

            return Task.CompletedTask;
        }

        protected virtual async Task OnAutomationStateChanged(AutomationState state)
        {
            if (!IsSessionRunning)
                return;

            if (state == AutomationState.SessionStart)
            {
                await ResetConnectors(true);
            }
            else if (state == AutomationState.Pushback)
            {
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
            }
            else if (state == AutomationState.TaxiOut)
            {
                Tracker.LastCapturedGate = "";
            }
            else if (state == AutomationState.Flight)
            {
                Tracker.Reset();
                if (Config.DebugArrival)
                    await ResetConnectors(true);
                else
                {
                    await CallOnConnectors((connector) => connector.SetMenuTitle(""));
                    await CallOnConnectors((connector) => connector.SetMenuLines([]));
                }
            }
            else if (state == AutomationState.TurnAround)
            {
                Tracker.LastCapturedGate = "";
                Tracker.Clear(AppNotification.UpdatesBlocked);
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
            }
        }

        protected virtual Task OnJetwayStateChanged(IGsxService gsxService)
        {
            if (!IsSessionRunning)
                return Task.CompletedTask;

            if (gsxService.State <= GsxServiceState.Bypassed || gsxService.State >= GsxServiceState.Active)
                Tracker.Clear(AppNotification.OperateJetway);

            return Task.CompletedTask;
        }

        protected virtual Task OnJetwayOperationChanged(GsxServiceState state)
        {
            if (!NotifyUpdates)
                return Task.CompletedTask;

            if (state == GsxServiceState.Active)
                Tracker.Track(AppNotification.OperateJetway);

            return Task.CompletedTask;
        }

        protected virtual Task OnStairsStateChanged(IGsxService gsxService)
        {
            if (!IsSessionRunning)
                return Task.CompletedTask;

            if (gsxService.State <= GsxServiceState.Bypassed || gsxService.State >= GsxServiceState.Active)
                Tracker.Clear(AppNotification.OperateStairs);

            return Task.CompletedTask;
        }

        protected virtual Task OnStairsOperationChanged(GsxServiceState state)
        {
            if (!NotifyUpdates)
                return Task.CompletedTask;

            if (state == GsxServiceState.Active)
                Tracker.Track(AppNotification.OperateStairs);

            return Task.CompletedTask;
        }

        protected virtual async Task OnBoardingStateChanged(IGsxService gsxService)
        {
            if (!IsSessionRunning)
                return;

            if (gsxService.IsRunning)
            {
                if (gsxService.State == GsxServiceState.Requested)
                {
                    await CallOnConnectors((connector) => connector.SetBoardPaxInfo(0));
                    await CallOnConnectors((connector) => connector.SetBoardCargoInfo(0));
                }

                string status = "Boarding ...";
                if (await AircraftController.GetIsCargo())
                    status = "Loading ...";
                Tracker.TrackMessage(AppNotification.ServiceBoard, status);
            }
            else
            {
                await CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
                Tracker.Clear(AppNotification.ServiceBoard);
            }
        }

        protected virtual Task OnBoardingPaxChanged(GsxServiceBoarding gsxService)
        {
            if (!NotifyUpdates || !gsxService.IsRunning)
                return Task.CompletedTask;

            if (!gsxService.IsRunning)
                return CallOnConnectors((connector) => connector.SetBoardPaxInfo(-1));
            else
                return CallOnConnectors((connector) => connector.SetBoardPaxInfo(gsxService.PaxTotal));
        }

        protected virtual Task OnBoardingCargoChanged(GsxServiceBoarding gsxService)
        {
            if (!NotifyUpdates || !gsxService.IsRunning)
                return Task.CompletedTask;

            if (!gsxService.IsRunning)
                return CallOnConnectors((connector) => connector.SetBoardCargoInfo(-1));
            else
                return CallOnConnectors((connector) => connector.SetBoardCargoInfo(gsxService.CargoPercent));
        }

        protected virtual async Task OnDeboardingStateChanged(IGsxService gsxService)
        {
            if (!IsSessionRunning)
                return;

            if (gsxService.IsRunning)
            {
                var target = AutomationController.PayloadArrival.CountPax;
                if (gsxService.State == GsxServiceState.Requested)
                {
                    await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(target));
                    await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(100));
                }

                string status = "Deboarding ...";
                if (await AircraftController.GetIsCargo())
                    status = "Unloading ...";
                Tracker.TrackMessage(AppNotification.ServiceDeboard, status);
            }
            else
            {
                await CallOnConnectors((connector) => connector.SetDeboardPaxInfo(-1));
                await CallOnConnectors((connector) => connector.SetDeboardCargoInfo(-1));
                Tracker.Clear(AppNotification.ServiceDeboard);
            }
        }

        protected virtual Task OnDeboardingPaxChanged(GsxServiceDeboarding gsxService)
        {
            if (!NotifyUpdates || !gsxService.IsRunning)
                return Task.CompletedTask;

            int paxOnBoard = AutomationController.PayloadArrival.CountPax - gsxService.PaxTotal;
            if (paxOnBoard < 0)
                paxOnBoard = 0;
            return CallOnConnectors((connector) => connector.SetDeboardPaxInfo(paxOnBoard));
        }

        protected virtual Task OnDeboardingCargoChanged(GsxServiceDeboarding gsxService)
        {
            if (!NotifyUpdates || !gsxService.IsRunning)
                return Task.CompletedTask;

            return CallOnConnectors((connector) => connector.SetDeboardCargoInfo(100 - gsxService.CargoPercent));
        }

        protected virtual Task OnRefuelStateChanged(IGsxService gsxService)
        {
            if (!IsSessionRunning)
                return Task.CompletedTask;

            if (gsxService.State == GsxServiceState.Active)
                Tracker.TrackMessage(AppNotification.ServiceRefuel, "Connecting Hose ...");
            else
                Tracker.Clear(AppNotification.ServiceRefuel);

            return Task.CompletedTask;
        }

        protected virtual Task OnRefuelHoseConnection(bool connected)
        {
            if (!NotifyUpdates)
                return Task.CompletedTask;

            if (ServiceRefuel.IsActive)
                if (connected)
                    Tracker.TrackMessage(AppNotification.ServiceRefuel, "Loading Fuel ...");
                else if (!connected && ServiceRefuel.WasHoseConnected)
                    Tracker.TrackTimeout(AppNotification.ServiceRefuel, 30000, "Removing Hose ...");

            return Task.CompletedTask;
        }

        protected virtual Task OnDeiceStateChanged(IGsxService service)
        {
            if (!IsSessionRunning)
                return Task.CompletedTask;

            if (service.State == GsxServiceState.Completed)
                Tracker.Clear(AppNotification.GateMove);

            return Task.CompletedTask;
        }

        protected virtual async Task OnPushbackStateChanged(IGsxService service)
        {
            if (!IsSessionRunning)
                return;

            if (service.State == GsxServiceState.Completed)
            {
                Tracker.Clear(AppNotification.PushPhase);
                Tracker.Clear(AppNotification.PushReleaseBrake);
                LastPushInfo = "";

                if (Profile.RunAutomationService && GsxMenu.MenuCommandsAllowed)
                {
                    GsxMenu.ExternalSequence = true;
                    Logger.Debug($"Refresh and disable Menu after Pushback ...");
                    await GsxMenu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection || (GsxMenu.IsToolbarEnabled && !Config.DisableUserEnabledMenu));
                    await GsxMenu.WaitInterval(2);
                    if (!GsxController.IsDeiceAvail)
                        await GsxMenu.RunCommand(GsxMenuCommand.State(GsxMenuState.DISABLED), false);
                    GsxMenu.ExternalSequence = false;
                    _ = TaskTools.RunDelayed(() => Tracker.Clear(AppNotification.GateSelect), 1000, Token);
                }
            }
            else if (service.State == GsxServiceState.Active && ServicePushBack.PushStatus == 0)
            {
                Tracker.Clear(AppNotification.PushPhase);
                Tracker.Clear(AppNotification.PushReleaseBrake);
            }
        }

        protected virtual Task OnPushStatusChanged(GsxServicePushback gsxService)
        {
            if (!IsSessionRunning)
                return Task.CompletedTask;

            string status = "Standby ...";
            int value = ServicePushBack.PushStatus;
            if (value == 1)
                status = "Insert Pin ...";
            else if (value == 2)
                status = "Locking Gear ...";
            else if (value == 6)
                status = "In Progress ...";
            else if (value == 7)
                status = "Set Brake!";
            else if (value == 8)
                status = "Confirm Start?";
            else if (value == 5 || value == 9)
                status = "Unlocking Gear ...";
            else if (value == 10)
                status = "Remove Pin ...";

            if (value > 0)
            {
                LastPushInfo = status;
                Tracker.TrackMessage(AppNotification.PushPhase, LastPushInfo);
                if (value > 5)
                    Tracker.Clear(AppNotification.PushReleaseBrake);
            }
            else
                Tracker.Clear(AppNotification.PushPhase);

            return Task.CompletedTask;
        }

        protected virtual Task OnPushPinChanged(GsxServicePushback gsxService)
        {
            if (!gsxService.IsPinInserted && gsxService.PushStatus == 10)
            {
                LastPushInfo = "Show Pin ...";
                Tracker.TrackMessage(AppNotification.PushPhase, LastPushInfo);
            }

            return Task.CompletedTask;
        }
    }
}
