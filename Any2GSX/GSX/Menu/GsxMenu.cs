using Any2GSX.AppConfig;
using Any2GSX.GSX.API;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Menu
{
    public class GsxMenu() : IGsxMenu
    {
        protected virtual GsxController Controller => AppService.Instance.GsxController;
        protected virtual StateStore StateStore => Controller.StateStore;
        protected virtual GsxGate Gate => Controller.Gate;
        protected virtual GsxApiClient ApiClient => Controller.ApiClient;
        protected virtual GsxApiData ApiData => ApiClient.ApiData;
        protected virtual NotificationManager NotificationManager => AppService.Instance.NotificationManager;
        public virtual NotificationTracker Tracker => AppService.Instance?.NotificationTracker;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual Config Config => Controller.Config;
        protected virtual SettingProfile Profile => Controller.Profile;

        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual GsxMenuState MenuState { get; protected set; } = GsxMenuState.DISABLED;
        public virtual string TextMenuState => GetStateString();
        public virtual string MenuTitle { get; protected set; }
        public virtual string LastTitle { get; protected set; }
        public bool HasTitle => !string.IsNullOrWhiteSpace(MenuTitle);
        public virtual int MenuLineCount => MenuLines.Count;
        public virtual List<string> MenuLines { get; } = [];

        public virtual bool IsReady => MenuState == GsxMenuState.READY || MenuState == GsxMenuState.HIDE;
        public virtual bool ReadyReceived { get; protected set; } = false;
        public virtual bool FirstReadyReceived { get; protected set; } = false;
        public virtual DateTime LastReady { get; protected set; } = DateTime.Now;
        public virtual bool IsSequenceActive { get; protected set; } = false;
        public virtual bool IsCommandActive { get; protected set; } = false;
        public virtual GsxMenuCommandType LastCommandType { get; protected set; } = (GsxMenuCommandType)(-1);
        public virtual bool MenuUpdatesBlocked => Tracker?.IsActive(AppNotification.UpdatesBlocked) == true;
        public virtual bool ExternalSequence { get; set; } = false;
        public virtual bool MenuCommandsAllowed => !IsSequenceActive && !IsCommandActive && !MenuUpdatesBlocked && !ExternalSequence && !SettingMenuDetected && !SearchActive && ApiData?.MenuLoading == false;
        public virtual bool IsSequenceAllowed => !IsSequenceActive && !IsCommandActive && !SettingMenuDetected;
        public virtual bool IsCommandAllowed => !IsCommandActive && !SettingMenuDetected;
        public virtual bool IsToolbarEnabled { get; protected set; } = false;
        public virtual bool SettingMenuDetected { get; protected set; } = false;
        public virtual bool SearchActive => Controller?.ApiData?.SearchActive ?? false;
        public virtual int SearchSession => Controller?.ApiData?.SearchSession ?? -1;
        public virtual ConcurrentDictionary<GsxMenuCommandOperatorType, bool> OperatorSelectionState { get; } = new()
        {
            { GsxMenuCommandOperatorType.Unknown, false },
            { GsxMenuCommandOperatorType.Handling, false },
            { GsxMenuCommandOperatorType.Refuel, false },
            { GsxMenuCommandOperatorType.Catering, false },
        };

        public virtual bool IsGateMenu => ApiData?.HandlerData?.gate?.uiGateName != null;
        public virtual bool IsDeicePad => IsActivateServicesMenu && (MatchMenuLine(0, GsxConstants.MenuRequestDeice) || GetMenuLine(0).Contains("treatment", StringComparison.InvariantCultureIgnoreCase));
        public virtual bool IsActivateServicesMenu => MatchTitle(GsxConstants.MenuGate);
        public virtual bool IsSelectGateMenu => MatchTitle(GsxConstants.MenuParkingSelect);
        public virtual bool IsChangeGateMenu => MatchTitle(GsxConstants.MenuParkingChange);
        public virtual bool IsOperatorMenu => MatchTitle(GsxConstants.MenuOperatorHandling) || MatchTitle(GsxConstants.MenuOperatorFuel) || MatchTitle(GsxConstants.MenuOperatorCater);
        protected virtual ConcurrentDictionary<string, Func<IGsxMenu, Task>> MenuCallbacks { get; } = [];
        protected virtual ConcurrentDictionary<string, bool> MenuCallbackActive { get; } = [];

        protected virtual ISimResourceSubscription SubMenuEvent { get; set; }
        protected virtual ISimResourceSubscription SubMenuChoice { get; set; }

        public virtual double LastMenuSelection { get; protected set; } = -2;
        public virtual DateTime LastSelectionTime { get; protected set; } = DateTime.MaxValue;
        public virtual DateTime LastTimeout { get; protected set; } = DateTime.MaxValue;

        public event Func<string, Task> MenuTitleChanged;
        public event Func<IGsxMenu, Task> OnMenuReady;
        public event Func<IGsxMenu, Task> OnMenuReceived;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubMenuEvent = Controller.SimStore.AddEvent(GsxConstants.EventMenu);
                SubMenuEvent?.OnReceived += OnMenuSimEvent;
                ApiClient.MenuDataReceived += OnMenuApiEvent;
                SubMenuChoice = Controller.SimStore.AddVariable(GsxConstants.VarMenuChoice);
                SubMenuChoice?.OnReceived += OnMenuSelection;

                Controller.OnCouatlStopped += OnCouatlStopped;

                AddMenuCallback(GsxConstants.MenuTugAttach, OnTugQuestion);
                AddMenuCallback(GsxConstants.MenuPushbackRequest, OnPushRequestQuestion);
                AddMenuCallback(GsxConstants.MenuFollowMe, OnFollowMeQuestion);
                AddMenuCallback(GsxConstants.MenuDeiceOnPush, OnDeiceQuestion);
                AddMenuCallback(GsxConstants.MenuParkingChange, OnParkingChange);
                AddMenuCallback(GsxConstants.MenuParkingSelect, OnParkingSelect);
                AddMenuCallback(GsxConstants.MenuBoardCrew, OnCrewBoardQuestion);
                AddMenuCallback(GsxConstants.MenuDeboardCrew, OnCrewDeboardQuestion);
                AddMenuCallback(GsxConstants.MenuOperatorHandling, OnOperatorSelection);
                AddMenuCallback(GsxConstants.MenuOperatorFuel, OnOperatorSelection);
                AddMenuCallback(GsxConstants.MenuOperatorCater, OnOperatorSelection);
                AddMenuCallback(GsxConstants.MenuRefuelLevel, OnRefuelLevelQuestion);
                AddMenuCallback(GsxConstants.MenuRemoveStairs, OnRemoveStairQuestion);
                AddMenuCallback(GsxConstants.MenuUseAirStairs, OnUseAirStairQuestion);
                AddMenuCallback(GsxConstants.MenuPushbackDirection, OnPushDirectionSelection);
                AddMenuCallback(GsxConstants.MenuPushbackInterrupt, OnPushInterruptQuestion);
                Controller.ServiceDeice.OnStateChanged += OnDeiceStateChanged;

                StateStore.Add(StateVar.NoJetwayDetected, false, true, true);
                StateStore.Add(StateVar.WasFollowMeAnswered, false, true, true);
                StateStore.Add(StateVar.WasOperatorPreferred, false, true, false);
                StateStore.Add(StateVar.WasStairRemoved, false, true, true);
                StateStore.Add(StateVar.DeiceGateQuestionAnswered, false, true, true);

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            try
            {
                Controller.ServiceDeice.OnStateChanged -= OnDeiceStateChanged;
                Controller.OnCouatlStopped -= OnCouatlStopped;

                Controller.SimStore.Remove(GsxConstants.EventMenu)?.OnReceived -= OnMenuSimEvent;
                ApiClient.MenuDataReceived -= OnMenuApiEvent;
                SubMenuChoice?.OnReceived -= OnMenuSelection;
                Controller.SimStore.Remove(GsxConstants.VarMenuChoice);

                MenuCallbacks.Clear();
                MenuCallbackActive.Clear();
            }
            catch { }

            IsInitialized = false;
        }

        protected virtual string GetStateString()
        {
            try
            {
                return MenuState.ToString();
            }
            catch
            {
                return GsxMenuState.UNKNOWN.ToString();
            }
        }

        protected virtual Task OnCouatlStopped(IGsxController gsxController)
        {
            ReadyReceived = false;
            ResetOperatorSelections();
            return Task.CompletedTask;
        }

        protected virtual void ResetOperatorSelections()
        {
            StateStore[StateVar.WasOperatorPreferred] = false;
            foreach (var op in Enum.GetValues<GsxMenuCommandOperatorType>())
                OperatorSelectionState[op] = false;
        }

        public virtual void ResetNotRunning()
        {
            FirstReadyReceived = false;
            LastReady = DateTime.Now;
            ResetOperatorSelections();
            foreach (var key in MenuCallbackActive.Keys)
                MenuCallbackActive[key] = false;
        }

        public virtual void Reset()
        {
            LastMenuSelection = -2;
            LastSelectionTime = DateTime.MaxValue;
            MenuTitle = "";
            LastTitle = "";
            MenuLines.Clear();
            MenuState = GsxMenuState.UNKNOWN;
            FirstReadyReceived = false;
            LastReady = DateTime.Now;
            ReadyReceived = false;
            IsSequenceActive = false;
            IsCommandActive = false;
            LastCommandType = (GsxMenuCommandType)(-1);
            IsToolbarEnabled = false;
            ResetOperatorSelections();
            ExternalSequence = false;
            SettingMenuDetected = false;
            foreach (var key in MenuCallbackActive.Keys)
                MenuCallbackActive[key] = false;
        }

        public virtual void ResetFlight()
        {
            IsSequenceActive = false;
            IsCommandActive = false;
            LastCommandType = (GsxMenuCommandType)(-1);
            IsToolbarEnabled = false;
            LastReady = DateTime.Now;
            ResetOperatorSelections();
            ExternalSequence = false;
            SettingMenuDetected = false;
            foreach (var key in MenuCallbackActive.Keys)
                MenuCallbackActive[key] = false;
        }

        public virtual void AddMenuCallback(string title, Func<IGsxMenu, Task> callback)
        {
            MenuCallbacks.TryAdd(title, callback);
            MenuCallbackActive.TryAdd(title, false);
        }

        public virtual void RemoveMenuCallback(string title)
        {
            MenuCallbacks.TryRemove(title, out _);
            MenuCallbackActive.TryRemove(title, out _);
        }

        public virtual bool MatchTitle(string match)
        {
            return MenuTitle?.StartsWith(match, StringComparison.InvariantCultureIgnoreCase) == true || MenuTitle?.EndsWith(match, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        public virtual void BlockMenuUpdates(bool active, string reason = "", [CallerMemberName] string name = "")
        {
            if (active)
            {
                Logger.Debug($"Block Menu Updates for: {reason}");
                Tracker.TrackMessage(AppNotification.UpdatesBlocked, reason);
            }
            else
            {
                if (Tracker.IsActive(AppNotification.UpdatesBlocked))
                    Logger.Debug($"Menu Updates unblocked (@{name})");
                Tracker.Clear(AppNotification.UpdatesBlocked);
            }
        }

        protected virtual async Task OnRefuelLevelQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Refuel Level Question active");
            if (!Controller.HasRefuelDialog)
                return;

            int i = 0;
            bool found = false;
            foreach (var line in MenuLines)
            {
                found = line.Contains("Simbrief", StringComparison.InvariantCultureIgnoreCase);
                if (found)
                    break;
                i++;
            }

            if (!found)
            {
                i = 0;
                foreach (var line in MenuLines)
                {
                    found = line.StartsWith(GsxConstants.MenuRefuelLevelCustom, StringComparison.InvariantCultureIgnoreCase);
                    if (found)
                        break;
                    i++;
                }
            }

            if (found)
            {
                Tracker.Clear(AppNotification.UpdatesBlocked);

                Logger.Information($"Automation: Answering Fuel Amount Question with '{GetMenuLine(i)}'");
                await Select(i + 1);
                await WaitInterval();
                await Disable();
            }
        }

        protected virtual Task OnPushRequestQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Request Pushback Question active");
            return Select(1);
        }

        protected virtual async Task OnRemoveStairQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Remove Stair Question active");
            if (Profile.RunAutomationService)
            {
                Logger.Information($"Automation: Answering Remove Stair Question with '{GetMenuLine(1 - 1)}'");
                await Select(1);
                await Disable();
            }
            StateStore[StateVar.WasStairRemoved] = true;
            if (Tracker.IsActive(AppNotification.UpdatesBlocked) && !Controller.HasRefuelDialog && !Controller.ServiceWater.IsRequested && !Controller.ServiceLavatory.IsRequested)
                _ = TaskTools.RunDelayed(() => Tracker.Clear(AppNotification.UpdatesBlocked), Config.MenuOpenTimeout);
        }

        protected virtual async Task OnUseAirStairQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Aircraft Airstair Question active");
            if (Profile.RunAutomationService)
            {
                int num = 2;
                if (Profile.DoorPreferAirstair)
                    num = 3;

                Logger.Information($"Automation: Answering Airstair Question with '{GetMenuLine(num - 1)}'");
                await Select(num);
            }
        }

        protected virtual async Task OnTugQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Tug Question active");
            if (Profile.RunAutomationService)
            {
                if (Profile.AttachTugDuringBoarding == 2)
                {
                    Logger.Information($"Automation: Answering Tug Question with '{GetMenuLine(0)}'");
                    await Select(1);
                    Logger.Debug($"Skip: {Profile.SkipCrewBoardQuestion}");
                    Logger.Debug($"Answer: {Profile.AnswerCrewBoardQuestion}");
                    if (Profile.SkipCrewBoardQuestion || Profile.AnswerCrewBoardQuestion > 0 || ApiClient.GetSetting(GsxConstants.SettingIgnoreCrew) != 0)
                        await Disable();
                }
                else if (Profile.AttachTugDuringBoarding == 1)
                {
                    Logger.Information($"Automation: Answering Tug Question with '{GetMenuLine(1)}'");
                    await Select(2);
                    if (Profile.SkipCrewBoardQuestion || Profile.AnswerCrewBoardQuestion > 0 || ApiClient.GetSetting(GsxConstants.SettingIgnoreCrew) != 0)
                        await Disable();
                }
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual async Task OnFollowMeQuestion(IGsxMenu menu)
        {
            Logger.Debug($"FollowMe Question active");
            StateStore[StateVar.WasFollowMeAnswered] = false;
            await WaitInterval(1.5);

            if (Profile.RunAutomationService && Profile.SkipFollowMe && StateStore[StateVar.WasFollowMeAnswered] == false)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(GsxMenuCommand.Select(2, GsxConstants.MenuFollowMe, ["No"]));
                sequence.Commands.Add(GsxMenuCommand.Operator(GsxMenuCommandOperatorType.Handling));
                sequence.ResetMenuCheck = () => OperatorSelectionState[GsxMenuCommandOperatorType.Handling] == true;
                sequence.EnableMenuAfterResetCheck = () => Profile.EnableMenuForSelection && (Controller.AutomationState < AutomationState.Pushback || Controller.AutomationState > AutomationState.Arrival);
                Logger.Information($"Automation: Answering Follow-Me Question");
                if (await RunSequence(sequence))
                    StateStore[StateVar.WasFollowMeAnswered] = true;
            }
            else if (StateStore[StateVar.WasFollowMeAnswered] == true)
                Logger.Debug($"Skipped Follow Me Sequence");
        }

        protected virtual Task OnDeiceQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Deice Push Question active");
            if ((Profile.RunAutomationService || Profile.PilotsDeckIntegration) && Profile.KeepDirectionMenuOpen && Profile.AnswerDeiceOnReopen && StateStore[StateVar.DeiceGateQuestionAnswered] == true)
            {
                Logger.Information($"Automation: Answering Push Deice Question with '{GetMenuLine(1)}'");
                return Select(2);
            }
            else
                return Task.CompletedTask;
        }

        protected virtual async Task OnPushDirectionSelection(IGsxMenu menu)
        {
            Logger.Debug($"Push Direction Selection active");
            if (Profile.RunAutomationService && Gate.HasSelectionPushback)
            {
                int line = FindMenuLine(Gate.AutoSelectionPushback);
                if (line >= 0)
                {
                    if (await Select(line + 1))
                    {
                        Logger.Information($"Automation: Selecting Pushback Direction '{Gate.AutoSelectionPushback}'");
                        Gate.SetPushDirection("");
                        await ApiClient.MenuClose();
                    }
                }
            }
            else
                Tracker.Track(AppNotification.PushDirectionDisplayed);
        }

        protected virtual async Task OnPushInterruptQuestion(IGsxMenu menu)
        {
            if (IsCommandActive || ExternalSequence || !Profile.KeepDirectionMenuOpen)
                return;

            Logger.Debug($"Push Interrupt Question active");
            if (MatchMenuLine(2, GsxConstants.MenuPushbackDirection) && !Tracker.IsActive(AppNotification.PushDirectionSelected))
            {
                Logger.Debug($"Open Push Direction Selection");
                await Select(3);
            }
        }

        protected virtual Task OnDeiceStateChanged(IGsxService gsxService)
        {
            if (!Controller.IsGsxRunning)
                return Task.CompletedTask;

            if (gsxService.IsRequested)
                BlockMenuUpdates(true, "Deice Questions");
            else
                BlockMenuUpdates(false);

            return Task.CompletedTask;
        }

        protected virtual Task OnParkingSelect(IGsxMenu menu)
        {
            Logger.Debug($"Select Parking active");
            ResetOperatorSelections();

            if (IsSequenceAllowed)
            {
                if (!Tracker.IsActive(AppNotification.GateSelect) && ((Controller.AutomationState != AutomationState.Pushback && Controller.AutomationState != AutomationState.TaxiOut) || Controller.IsDeiceAvail))
                    Tracker.Track(AppNotification.GateSelect);
                Tracker.Clear(AppNotification.GateMove);
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnParkingChange(IGsxMenu menu)
        {
            Logger.Debug($"Change Parking active");
            Tracker.Clear(AppNotification.GateSelect);

            if (GsxConstants.MenuRegexFacility.GroupMatches(GetMenuLine(0), 1, out var group) || GsxConstants.MenuRegexFacility.GroupMatches(GetMenuLine(2), 1, out group))
            {
                string parking = Regex.Replace(group, @"\([^\)]+\)", "").Trim();
                parking = Regex.Replace(parking, @"\[[^\]]+\]", "").Trim();
                Gate.CaptureSelectedGate(parking);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task OnCrewBoardQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Board Crew Question active");
            if (Profile.RunAutomationService && Profile.AnswerCrewBoardQuestion > 0)
            {
                Logger.Information($"Automation: Answering Crew Board Question with '{GetMenuLine(Profile.AnswerCrewBoardQuestion - 1)}'");
                await Select(Profile.AnswerCrewBoardQuestion);
                await Disable();
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual async Task OnCrewDeboardQuestion(IGsxMenu menu)
        {
            Logger.Debug($"Deboard Crew Question active");
            if (Profile.RunAutomationService && Profile.AnswerCrewDeboardQuestion > 0)
            {
                Logger.Information($"Automation: Answering Crew Deboard Question with '{GetMenuLine(Profile.AnswerCrewDeboardQuestion - 1)}'");
                await Select(Profile.AnswerCrewDeboardQuestion);
                await Disable();
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual async Task OnOperatorSelection(IGsxMenu menu)
        {
            try
            {
                GsxMenuCommandOperatorType type = SetOperatorSelected(false);

                Logger.Debug($"{type} Operator Question active");
                if (!Profile.RunAutomationService)
                    return;

                var gsxOperator = GsxOperator.OperatorSelection(Profile, MenuLines);
                int doSelection = 0;
                if (gsxOperator != null)
                {
                    if (gsxOperator.Preferred && (Profile.OperatorAutoSelect || Profile.OperatorPreferenceSelect))
                    {
                        Logger.Information($"Automation: Selecting {type} Operator '{gsxOperator.Title}' (Preferred)");
                        if (StateStore[StateVar.WasOperatorPreferred] == false)
                            StateStore[StateVar.WasOperatorPreferred] = MatchTitle(GsxConstants.MenuOperatorHandling);
                        doSelection = gsxOperator.Number;
                    }
                    else if (Profile.OperatorAutoSelect)
                    {
                        Logger.Information($"Automation: Selecting {type} Operator '{gsxOperator.Title}' (GSX Choice)");
                        doSelection = gsxOperator.Number;
                    }
                }
                else if (Profile.OperatorAutoSelect)
                {
                    Logger.Warning($"Automation: Selecting {type} Operator #1 - no Matches found");
                    doSelection = 1;
                }

                if (doSelection > 0)
                {
                    SetOperatorSelected(true);
                    bool apiSetting = ApiClient.GetSetting(GsxConstants.SettingAutoSingleOperator) == 0;
                    if (MenuLineCount > 1 || apiSetting)
                    {
                        await Select(doSelection);
                        if (IsToolbarEnabled && !Controller.ServicePushBack.IsRunning)
                            await Disable();
                    }
                    else
                        Logger.Debug($"Automatic {type} Operator Selection skipped - Count: {MenuLineCount} | State: {MenuState} | GSX Setting: {apiSetting}");
                }
                else if (!IsSequenceActive)
                {
                    _ = TaskTools.RunDelayed(() =>
                    {
                        if (!OperatorSelectionState[type])
                        {
                            Logger.Debug($"Timeout on manual {type} Operator Selection - fix State");
                            SetOperatorSelected(true, type);
                        }
                    }, Config.OperatorSelectTimeout);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual GsxMenuCommandOperatorType GetMenuOperatorType()
        {
            if (MatchTitle(GsxConstants.MenuOperatorHandling))
                return GsxMenuCommandOperatorType.Handling;
            else if (MatchTitle(GsxConstants.MenuOperatorFuel))
                return GsxMenuCommandOperatorType.Refuel;
            else if (MatchTitle(GsxConstants.MenuOperatorCater))
                return GsxMenuCommandOperatorType.Catering;
            else
                return GsxMenuCommandOperatorType.Unknown;
        }

        protected virtual GsxMenuCommandOperatorType SetOperatorSelected(bool value, GsxMenuCommandOperatorType type = GsxMenuCommandOperatorType.Unknown)
        {
            if (type == GsxMenuCommandOperatorType.Unknown)
                type = GetMenuOperatorType();

            if (type > GsxMenuCommandOperatorType.Unknown)
            {
                if (OperatorSelectionState[type] != value)
                    Logger.Debug($"Operator Selection Status for {type} set to: {value}");
                OperatorSelectionState[type] = value;
            }

            return type;
        }

        protected virtual Task OnMenuSelection(ISimResourceSubscription sub, object value)
        {
            double num = sub.GetNumber();
            if (num <= -2)
            {
                Logger.Debug($"Received Menu Selection: Clear ({num})");
                SettingMenuDetected = false;
            }
            else if (num == -1)
            {
                Logger.Debug($"Received Menu Selection: Timeout ({num})");
                SettingMenuDetected = false;
                LastTimeout = DateTime.Now;
            }
            else if (num == 0 && MatchTitle(GsxConstants.MenuPushbackInterrupt) && Controller.ServicePushBack.PushStatus > 0)
            {
                Logger.Debug($"Pushback Question was answered: {num + 1} - '{GetMenuLine(num)}'");
            }
            else if (num >= 0 && MatchTitle(GsxConstants.MenuPushbackDirection) && Controller.ServicePushBack.IsWaitingForDirection && !string.IsNullOrWhiteSpace(GetMenuLine(num)))
            {
                Logger.Debug($"Pushback Direction selected: {num + 1} - '{GetMenuLine(num)}'");
                Tracker.Track(AppNotification.PushReleaseBrake);
                Tracker.Track(AppNotification.PushDirectionSelected);
            }
            else if (MatchTitle(GsxConstants.MenuDeiceOnPush))
            {
                Logger.Debug($"Deice Push Question was answered: {num + 1} - '{GetMenuLine(num)}'");
                StateStore[StateVar.DeiceGateQuestionAnswered] = true;
            }
            else if (IsChangeGateMenu && GetMenuLine(num).Contains("warp", StringComparison.InvariantCultureIgnoreCase))
            {
                Tracker.TrackTimeout(AppNotification.UpdatesBlocked, Config.MenuOpenTimeout, "Warp to Gate");
                if (Profile.RunAutomationService)
                {
                    if (IsToolbarEnabled)
                        SetMenuState(GsxMenuState.HIDE);
                    Controller.AutomationController.TimedMenuOpen.Queue($"Automation: Open Menu in {(Config.MenuOpenTimeout / 1000.0):F1}s for Gate/Pad Menu", TimeSpan.FromMilliseconds(Config.MenuOpenTimeout), () => Profile.EnableMenuForSelection);
                }
            }
            else if (IsOperatorMenu)
            {
                if (!Profile.OperatorAutoSelect)
                {
                    Logger.Information($"Manual Operator Selection: {num + 1} - '{GetMenuLine(num)}'");
                    SetOperatorSelected(true);
                }
            }
            else if (MatchTitle(GsxConstants.MenuFollowMe) && (num == 0 || num == 1))
            {
                if (MenuCallbackActive[GsxConstants.MenuFollowMe] == false)
                    Logger.Debug("Follow Me answered externally");
                StateStore[StateVar.WasFollowMeAnswered] = true;
            }
            else if (!string.IsNullOrWhiteSpace(GetMenuLine(num)))
                Logger.Debug($"Received Menu Selection: {num + 1} - '{GetMenuLine(num)}'");
            else
                Logger.Debug($"Received Menu Selection: {num + 1}");

            if (num == 12)
            {
                Logger.Debug("Setting Menu detected");
                SettingMenuDetected = true;
            }

            if (num >= -1)
                Tracker.Clear(AppNotification.GsxQuestion);
            LastSelectionTime = num >= 0 || num == -2 ? DateTime.Now : DateTime.MaxValue;

            TaskTools.RunPool(() => NotificationManager.ClearMenu(), RequestToken);
            ReadyReceived = false;
            LastMenuSelection = num;

            return Task.CompletedTask;
        }

        public virtual void OnToolbarEvent(string data)
        {
            if (!ApiClient.IsConnected)
                return;

            IsToolbarEnabled = data?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true;
            Logger.Debug($"Toolbar {(IsToolbarEnabled ? "enabled" : "disabled")} by User");
            if (IsInitialized && Config.GsxToolbarFixes)
            {
                if (!IsToolbarEnabled)
                {
                    Logger.Debug("Close Menu on disabled");
                    _ = TaskTools.RunPool(Close, RequestToken);
                }
                else if ((DateTime.Now - LastReady).TotalMilliseconds >= Config.MenuCheckInterval)
                {
                    Logger.Debug("Open Menu on enabled-ready");
                    _ = TaskTools.RunPool(async () =>
                    {
                        await SetMenuState(GsxMenuState.TIMEOUT);
                        await WaitInterval(0.5);
                        await ApiClient.MenuOpen();
                    }, RequestToken);
                }
            }
        }

        protected virtual Task OnMenuSimEvent(ISimResourceSubscription sub, object value)
        {
            var state = sub.GetValue<int>();
            if (!Controller.IsActive || state <= (int)GsxMenuState.HIDE || state > (int)GsxMenuState.DISABLED)
                return Task.CompletedTask;
            else if (state == 3 && MenuState != GsxMenuState.TIMEOUT)
            {
                Logger.Debug($"Menu Event from SimEvent");
                return OnUpdateMenu((GsxMenuState)state, "", []);
            }
            else
                return Task.CompletedTask;
        }

        protected virtual Task OnMenuApiEvent(GsxMenuState state, string title, string[] entries)
        {
            if (!Controller.IsActive)
                return Task.CompletedTask;
            else
            {
                Logger.Debug($"Menu Event from ApiEvent");
                return OnUpdateMenu(state, title, entries);
            }
        }

        protected virtual Task OnUpdateMenu(GsxMenuState state, string title, string[] entries)
        {
            try
            {
                MenuState = state;
                Logger.Debug($"Received Menu Event: {TextMenuState}");

                if (!FirstReadyReceived && MenuState == GsxMenuState.READY && !string.IsNullOrWhiteSpace(title))
                {
                    Logger.Debug($"First Menu Ready received");
                    FirstReadyReceived = true;
                }

                bool menuChanged = false;
                LastTitle = MenuTitle;
                if (MenuState == GsxMenuState.READY)
                {
                    menuChanged = UpdateMenu(title, entries);
                    LastReady = DateTime.Now;

                    if (MatchTitle(GsxConstants.MenuGate))
                    {
                        Tracker.Clear(AppNotification.GateMove);
                        Tracker.Clear(AppNotification.GateSelect);
                    }


                    if (GetMenuLine(5).Contains(GsxConstants.MenuNoJetway, StringComparison.InvariantCultureIgnoreCase) && StateStore[StateVar.NoJetwayDetected] == false)
                    {
                        StateStore[StateVar.NoJetwayDetected] = true;
                        Logger.Debug($"No Jetway detected flag set");
                    }
                }
                else if (MenuState >= GsxMenuState.TIMEOUT)
                {
                    if (MenuState == GsxMenuState.TIMEOUT)
                        MenuTitle = "";

                    Tracker.Clear(AppNotification.GsxQuestion);

                    if (MenuState == GsxMenuState.DISABLED)
                        IsToolbarEnabled = false;
                }

                LastSelectionTime = DateTime.MaxValue;
                LastTimeout = MenuState == GsxMenuState.TIMEOUT ? DateTime.Now : DateTime.MaxValue;
                ReadyReceived = MenuState == GsxMenuState.READY && !string.IsNullOrWhiteSpace(title);

                if (ReadyReceived && menuChanged)
                {
                    var matchingCallbacks = MenuCallbacks.Where(c => MatchTitle(c.Key));
                    foreach (var callback in matchingCallbacks)
                    {
                        _ = TaskTools.RunPool(async () =>
                        {
                            MenuCallbackActive[callback.Key] = true;
                            await WaitInterval();
                            await callback.Value.Invoke(this);
                            MenuCallbackActive[callback.Key] = false;
                        }, RequestToken);
                    }
                }

                if (MenuState == GsxMenuState.READY && !string.IsNullOrWhiteSpace(title))
                    _ = TaskTools.RunPool(() => OnMenuReady?.Invoke(this), RequestToken);

                _ = TaskTools.RunPool(() => OnMenuReceived?.Invoke(this), RequestToken);
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    ReadyReceived = false;
                }
            }

            return Task.CompletedTask;
        }

        public virtual string GetMenuLine(double line)
        {
            return GetMenuLine((int)line);
        }

        public virtual string GetMenuLine(int line)
        {
            if (line < 0 || line >= MenuLines.Count)
                return "";
            else
                return MenuLines[line];
        }

        public virtual bool MatchMenuLine(int line, string match)
        {
            return GetMenuLine(line).Contains(match, StringComparison.InvariantCultureIgnoreCase);
        }

        public virtual bool MatchMenuLine(double line, string match)
        {
            return MatchMenuLine((int)line, match);
        }

        public virtual int FindMenuLine(string match)
        {
            for (int i = 0; i < MenuLines.Count; i++)
            {
                if (MatchMenuLine(i, match))
                    return i;
            }

            Logger.Debug($"No Menu Line matched '{match}'");
            return -1;
        }

        protected virtual bool UpdateMenu(string title, string[] entries)
        {
            MenuLines.Clear();

            MenuTitle = title;
            foreach (string entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry))
                    MenuLines.Add(entry);
            }
            Logger.Debug($"Received {MenuLineCount} Lines");

            if (LastTitle != MenuTitle)
            {
                Logger.Debug($"Menu Title changed: '{MenuTitle}'");
                _ = TaskTools.RunPool(() => MenuTitleChanged?.Invoke(MenuTitle), RequestToken);
            }

            return LastTitle != MenuTitle;
        }

        public virtual async Task<int> WaitInterval(double factor = 1.0)
        {
            int interval = (int)(factor * Config.MenuCheckInterval);
            if (interval > 0)
                await Task.Delay(interval, RequestToken);

            return interval;
        }

        public virtual async Task<bool> WaitMenuReady(int timeout = 0)
        {
            if (timeout == 0)
                timeout = Config.MenuOpenTimeout;

            int waitTime = 0;

            Logger.Debug($"Wait for Menu Ready ...");
            await WaitInterval(1.5);

            while (!ReadyReceived && !RequestToken.IsCancellationRequested && waitTime <= timeout)
                waitTime += await WaitInterval(0.5);

            if (waitTime < timeout)
            {
                if (waitTime > 0)
                    Logger.Debug($"Menu was Ready after {waitTime}ms");
                return true;
            }
            else
            {
                if (timeout == Config.MenuOpenTimeout)
                    Logger.Debug($"Menu Open timed out");
                return false;
            }
        }

        protected virtual Task<bool> SetMenuState(GsxMenuState state)
        {
            Logger.Debug($"Setting Menu State to {state}");
            return SubMenuEvent.WriteValue((int)state);
        }

        protected virtual async Task<bool> Open(bool enableToolbar, int timeout = 0) //GSX Workaround (forcing enable on fail else not ready ...)
        {
            bool result;
            try
            {
                Logger.Debug($"Open - IsToolbarEnabled {IsToolbarEnabled} | enableToolbar {enableToolbar} | PilotsDeckIntegration {Profile.PilotsDeckIntegration}");
                ReadyReceived = false;
                GsxMessageResult apiResult = null;
                if ((IsToolbarEnabled && !enableToolbar) || (IsToolbarEnabled && Profile.PilotsDeckIntegration))
                {
                    Logger.Debug("Disable GSX Toolbar ...");
                    await ApiClient.MenuClose();
                    if (Controller.IsWalkaround)
                        await AppService.Instance.CommBus.SendGsxMenu("Close"); //GSX Workaround - Menu not reacting to disable, hide or close (-1) in Walkaround
                    else
                        await SetMenuState(GsxMenuState.DISABLED);
                    IsToolbarEnabled = false;
                    await WaitInterval();
                    apiResult = await ApiClient.MenuOpen();
                }
                else if (!IsToolbarEnabled && enableToolbar && Profile.EnableMenuForSelection && !Profile.PilotsDeckIntegration)
                {
                    Logger.Information($"Enable GSX Toolbar ...");
                    await ApiClient.MenuClose();
                    await WaitInterval(0.5);
                    await AppService.Instance.CommBus.SendGsxMenu("Open");
                    IsToolbarEnabled = true;
                    await WaitInterval();
                    apiResult = await ApiClient.MenuOpen();
                }
                else
                    apiResult = await ApiClient.MenuOpen();

                result = apiResult?.HasError == false && await WaitMenuReady(timeout == 0 ? Config.MenuOpenTimeout : timeout);
                if (!result)
                    await Disable();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        protected virtual async Task<bool> Select(int number)
        {
            Logger.Debug($"Menu Select Item '{number}'");
            ReadyReceived = false;
            var result = await ApiClient.MenuSelect(number - 1);
            if (!result.HasError)
            {
                await WaitInterval();
                return true;
            }
            else
                return false;
        }

        protected virtual async Task<bool> Close()
        {
            ReadyReceived = false;
            Logger.Debug($"Close Menu");
            var apiResult = await ApiClient.MenuClose();
            await WaitInterval(0.5);
            var stateResult = await SetMenuState(GsxMenuState.TIMEOUT);

            return apiResult?.HasError == false && stateResult;
        }

        protected virtual async Task<bool> Disable()
        {
            ReadyReceived = false;
            Logger.Debug($"Disable Menu");
            var apiResult = await ApiClient.MenuClose();
            await WaitInterval(0.5);
            var stateResult = await SetMenuState(GsxMenuState.DISABLED);

            return apiResult?.HasError == false && stateResult;
        }

        protected virtual async Task<bool> SequenceWaiter(string type, Func<bool> allowed, int interval = 1000, int timeout = 30000)
        {
            bool result = allowed();
            if (!result)
            {
                Logger.Debug($"{type} Execution blocked - waiting ... (IsSequenceActive {IsSequenceActive} | IsCommandActive {IsCommandActive})");
                do
                {
                    await Task.Delay(1000, RequestToken);
                    timeout -= 1000;
                }
                while (timeout > 0 && !allowed());

                result = timeout > 0 || allowed();
                if (!result)
                    Logger.Debug($"Wait for {type} timed out!");
            }

            return result;
        }

        public virtual async Task RunGateSelection()
        {
            if (Gate.HasSelectionArrivalGate)
            {
                ExternalSequence = true;
                bool skipSelection = false;
                var gateName = GsxPayloadHandlerGateInfo.NormalizeName(Gate.AutoSelectionArrivalGate.ToString());
                Logger.Information($"Automation: Attempting to select Gate '{gateName}'");
                await Open(Profile.EnableMenuForSelection && (!Profile.OperatorAutoSelect || !Profile.SkipFollowMe));
                await WaitInterval(2);

                GsxMessageResult result = new();
                if (Gate.AutoSelectionArrivalGate.GetValueKind() == JsonValueKind.String)
                {
                    Logger.Debug($"Attempting API Select Method for '{gateName}'");
                    result = await ApiClient.GateSelect(gateName);
                }
                else if (Gate.AutoSelectionArrivalGate.GetValueKind() == JsonValueKind.Number)
                {
                    Logger.Debug($"Attempting API Select Method for #{Gate.AutoSelectionArrivalGate.GetValue<int>()}");
                    result = await ApiClient.GateSelect(Gate.AutoSelectionArrivalGate.GetValue<int>());
                }


                if ((result?.ok == true && result?.HasPayload == true) || result?.MatchCode("already_selected") == true)
                {
                    await WaitInterval(5);
                    await RunCommand(GsxMenuCommand.Operator(GsxMenuCommandOperatorType.Handling), Profile.EnableMenuForSelection && (!Profile.OperatorAutoSelect || !Profile.SkipFollowMe));
                    await WaitInterval(6);
                    Logger.Debug($"Arrival Gate Selection cleared");
                    await Open(false);
                    await WaitInterval(3);
                }
                else if (result?.HasError == true && result?.MatchCode("not_found") == true)
                {
                    Logger.Debug($"Attempting Menu Search Method for '{Gate.AutoSelectionArrivalGate?.ToString()}'");
                    await RunCommandSelection(GsxMenuCommand.Select(2, GsxConstants.MenuParkingSelect, ["search"]));
                    await WaitInterval(5);

                    if (ApiData.SearchActive)
                    {
                        await ApiClient.MenuSearch(Gate.AutoSelectionArrivalGate?.ToString());
                        await WaitInterval(5);
                        if (MenuLineCount >= 2)
                        {
                            await RunCommand(GsxMenuCommand.Select(1), false);
                            int delay = Config.OperatorSelectTimeout / 2;
                            if (!Profile.OperatorAutoSelect)
                                delay += Config.OperatorSelectTimeout;
                            if (!Profile.SkipFollowMe)
                                delay += Config.OperatorSelectTimeout;
                            await Task.Delay(delay, RequestToken);
                            await Open(false);
                            await WaitInterval(3);
                        }
                        else
                        {
                            Logger.Information($"Gate Search failed - Manual Selection required");
                            skipSelection = true;
                        }
                    }
                    else
                    {
                        Logger.Information($"Gate Search failed - Manual Selection required");
                        skipSelection = true;
                    }
                }

                if (Gate.HasGateSelection)
                {
                    Logger.Information($"Automatic Arrival Gate Selection successful for '{Gate.LastGateSelection}'");
                    Gate.SetArrivalGate(null);
                }
                else if (skipSelection)
                    Gate.SetArrivalGate(null);
                ExternalSequence = false;
            }
        }

        public virtual async Task<bool> RunSequence(GsxMenuSequence sequence)
        {
            if (!await SequenceWaiter("Sequence", () => IsSequenceAllowed))
                return false;

            if ((!Controller.IsGsxRunning && !sequence.IgnoreGsxState) || RequestToken.IsCancellationRequested)
            {
                Logger.Debug("Sequence skipped - GSX is not ready");
                return false;
            }

            IsSequenceActive = true;
            sequence.IsExecuting = true;
            Tracker.Track(AppNotification.MenuSequence);

            bool result = false;
            int counter = 0;
            LastCommandType = (GsxMenuCommandType)(-1);
            foreach (var command in sequence.Commands)
            {
                result = await RunCommand(command, sequence.EnableMenu);
                LastCommandType = command.Type;

                if (result)
                    counter++;
                else
                    break;
            }
            result = result && counter == sequence.Commands.Count;

            if (sequence.ResetMenu)
            {
                await Disable();
                if (result)
                {
                    Logger.Debug($"Reset/Open Menu after Sequence");
                    await Open(sequence.EnableMenuAfterReset, Config.OperatorWaitTimeout);
                }
            }

            sequence.IsExecuting = false;
            sequence.IsSuccess = result;
            if (!result)
                await WaitInterval(4);
            IsSequenceActive = false;
            Tracker.Clear(AppNotification.MenuSequence);
            Logger.Debug($"Sequence finished with Result: {result}");
            return result;
        }

        public virtual async Task<bool> RunCommand(GsxMenuCommand command, bool enableMenu)
        {
            Logger.Debug($"Run Cmd Type: {command.Type}");

            if (!await SequenceWaiter("Command", () => IsCommandAllowed))
                return false;

            if (!IsSequenceActive)
                ExternalSequence = true;

            IsCommandActive = true;
            Controller.Tracker.TrackMessage(AppNotification.MenuCommand, command.Type.ToString());

            bool result = false;
            if (command.Type == GsxMenuCommandType.Open)
            {
                result = await RunCommandOpen(command, enableMenu);
            }
            else if (command.Type == GsxMenuCommandType.Wait)
            {
                await WaitInterval(command.Parameter <= 0 ? 1 : command.Parameter);
                result = true;
            }
            else if (command.Type == GsxMenuCommandType.Select)
            {
                result = await RunCommandSelection(command);
            }
            else if (command.Type == GsxMenuCommandType.Operator)
            {
                result = await RunCommandOperator(command);
            }
            else if (command.Type == GsxMenuCommandType.Close)
            {
                result = await RunCommandCloseDisable(command);
            }
            else if (command.Type == GsxMenuCommandType.Disable)
            {
                result = await RunCommandCloseDisable(command);
            }
            else if (command.Type == GsxMenuCommandType.Service)
            {
                result = await RunCommandServiceCall(command, enableMenu);
            }

            IsCommandActive = false;
            Tracker.Clear(AppNotification.MenuCommand);

            if (!IsSequenceActive)
                ExternalSequence = false;

            return result;
        }

        protected virtual bool CheckCommandTitle(GsxMenuCommand command, bool suppressWarning = false)
        {
            if (command.HasTitle)
            {
                bool isMatch = MatchTitle(command.Title);
                if ((!command.ExcludeTitle && isMatch) || (command.ExcludeTitle && !isMatch))
                    return true;
                else
                {
                    if (!suppressWarning)
                        Logger.Warning($"Menu Command '{command.Type}' ({command.Parameter}) failed - Menu Title did not match");
                    Logger.Debug($"Title '{command.Title}' did not match Menu '{MenuTitle}' (Exclude {command.ExcludeTitle})");
                    return false;
                }
            }
            else
                return true;
        }

        protected virtual async Task<bool> CheckCommandWait(GsxMenuCommand command)
        {
            if (!command.WaitReady || ReadyReceived)
                return true;
            else if (await WaitMenuReady())
                return true;
            else
            {
                Logger.Warning($"Menu Command '{command.Type}' ({(command.Parameter == 99 ? "x" : command.Parameter)}) failed - Menu was not Ready");
                return false;
            }
        }

        protected virtual async Task<bool> RunCommandOpen(GsxMenuCommand command, bool enableMenu)
        {
            if (!CheckCommandTitle(command))
                return false;

            if (ReadyReceived)
            {
                await Close();
                await WaitInterval(0.5);
            }

            if (!await Open(enableMenu))
            {
                await WaitInterval();
                if (await Open(enableMenu, Config.MenuOpenTimeout))
                {
                    await WaitInterval();
                    return true;
                }
                else
                {
                    Logger.Warning($"Menu Command '{command.Type}' failed - Timed out");
                    return false;
                }
            }
            else
                return true;

            //if (!ReadyReceived || (enableMenu && !IsToolbarEnabled))
            //{
            //    if (!await Open(enableMenu))
            //    {
            //        await WaitInterval();
            //        if (await Open(enableMenu, Config.MenuOpenTimeout))
            //        {
            //            await WaitInterval();
            //            return true;
            //        }
            //        else
            //        {
            //            Logger.Warning($"Menu Command '{command.Type}' failed - Timed out");
            //            return false;
            //        }
            //    }
            //    else
            //        return true;
            //}
            //else
            //{
            //    await WaitInterval();
            //    return true;
            //}
        }

        protected virtual async Task<bool> RunCommandSelection(GsxMenuCommand command)
        {
            if (!await CheckCommandWait(command))
                return false;

            if (!CheckCommandTitle(command))
                return false;

            int selection = command.Parameter;
            if (command.HasTextSelect)
            {
                foreach (var text in command.TextSelect)
                {
                    selection = FindMenuLine(text);
                    if (selection != -1)
                        break;
                }
                if (selection == -1)
                    return false;
                else
                    selection++;
            }

            await WaitInterval(LastCommandType == GsxMenuCommandType.Select ? 2 : 1);
            if (await Select(selection))
            {
                await WaitInterval();
                return true;
            }
            else
                return false;
        }

        protected virtual async Task<bool> RunCommandOperator(GsxMenuCommand command)
        {
            var type = command.OperatorType;
            int timeWaited = 0;
            int maxWait = Config.OperatorWaitTimeout;
            if (Controller.AutomationController.AircraftState.HasAirStairs)
                maxWait += maxWait / 2;

            if (!ReadyReceived && !IsOperatorMenu)
                Logger.Debug($"Waiting for {type} Operator Menu ...");
            while (timeWaited <= maxWait && !ReadyReceived && !IsOperatorMenu && !RequestToken.IsCancellationRequested)
                timeWaited += await WaitInterval();

            if (timeWaited > maxWait && !IsOperatorMenu)
            {
                Logger.Debug($"No Operator Menu for {type} detected");
                SetOperatorSelected(true, type);
            }
            else if (IsOperatorMenu)
            {
                if (!OperatorSelectionState[type])
                {
                    Logger.Information($"Waiting for {(Profile.OperatorAutoSelect ? "automatic" : "manual")} {type} Operator Selection (Timeout {Config.OperatorSelectTimeout / 1000}s) ... ");
                    while (timeWaited <= Config.OperatorSelectTimeout && IsOperatorMenu && !OperatorSelectionState[type] && !RequestToken.IsCancellationRequested)
                        timeWaited += await WaitInterval(0.5);

                    if (timeWaited > Config.OperatorSelectTimeout && !OperatorSelectionState[type])
                    {
                        Logger.Warning($"{(Profile.OperatorAutoSelect ? "Automatic" : "Manual")} Operator Selection timed out{(MenuState < GsxMenuState.TIMEOUT ? " - closing Menu" : "")}");
                        await WaitInterval(1.5);
                    }

                    if (!OperatorSelectionState[type])
                        SetOperatorSelected(true, type);
                }
                else
                    Logger.Debug($"{type} Operator was already selected");

            }
            return OperatorSelectionState[type];
        }

        protected virtual async Task<bool> RunCommandCloseDisable(GsxMenuCommand command)
        {
            if (!await CheckCommandWait(command))
                return false;

            if (!CheckCommandTitle(command, true))
                return true;

            bool result;
            if (command.Type == GsxMenuCommandType.Close)
                result = await Close();
            else
                result = await Disable();

            if (result)
            {
                await WaitInterval();
                return true;
            }
            else
                return false;
        }

        protected virtual async Task<bool> RunCommandServiceCall(GsxMenuCommand command, bool enableMenu)
        {
            GsxServiceType type = (GsxServiceType)command.Parameter;
            var result = await ApiClient.CallService(type);

            return !result.HasError;
        }
    }
}
