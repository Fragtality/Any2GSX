using Any2GSX.AppConfig;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Menu
{
    public class GsxMenu() : IGsxMenu
    {
        public virtual GsxController Controller => AppService.Instance.GsxController;
        protected virtual NotificationManager NotificationManager => AppService.Instance.NotificationManager;
        public virtual NotificationTracker Tracker => AppService.Instance?.NotificationTracker;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual Config Config => Controller.Config;
        protected virtual SettingProfile Profile => Controller.Profile;
        public virtual string PathMenu => Path.Join(Controller.PathInstallation, GsxConstants.RelativePathMenu);

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
        public virtual bool MenuCommandsAllowed => !IsSequenceActive && !IsCommandActive && !MenuUpdatesBlocked && !ExternalSequence;
        public virtual bool IsSequenceAllowed => !IsSequenceActive && !IsCommandActive;
        public virtual bool IsCommandAllowed => !IsCommandActive;
        public virtual bool IsToolbarEnabled { get; protected set; } = false;
        protected virtual bool WasOperatorSelected { get; set; }
        public virtual bool WasOperatorPreferred { get; protected set; } = false;
        public virtual bool WasOperatorHandlingSelected { get; protected set; } = false;
        public virtual bool WasOperatorCateringSelected { get; protected set; } = false;

        public virtual bool IsGateMenu => MatchTitle(GsxConstants.MenuGate);
        public virtual bool IsDeicePad => IsGateMenu && (MatchMenuLine(0, GsxConstants.MenuRequestDeice) || GetMenuLine(0).Contains("treatment", StringComparison.InvariantCultureIgnoreCase));
        public virtual bool IsSelectGateMenu => MatchTitle(GsxConstants.MenuParkingSelect);
        public virtual bool IsChangeGateMenu => MatchTitle(GsxConstants.MenuParkingChange);
        public virtual bool IsOperatorMenu => MatchTitle(GsxConstants.MenuOperatorHandling) || MatchTitle(GsxConstants.MenuOperatorCater);
        protected virtual ConcurrentDictionary<string, Func<GsxMenu, Task>> MenuCallbacks { get; } = [];

        protected virtual ISimResourceSubscription SubMenuEvent { get; set; }
        protected virtual ISimResourceSubscription SubMenuOpen { get; set; }
        protected virtual ISimResourceSubscription SubMenuChoice { get; set; }

        public virtual double LastMenuSelection { get; protected set; } = -2;
        public virtual DateTime LastSelectionTime { get; protected set; } = DateTime.MaxValue;
        public virtual DateTime LastTimeout { get; protected set; } = DateTime.MaxValue;
        public virtual bool DeiceGateQuestionAnswered { get; protected set; } = false;
        public virtual bool ExternalSequence { get; set; } = false;

        public event Func<string, Task> MenuTitleChanged;
        public event Func<IGsxMenu, Task> OnMenuReady;
        public event Func<IGsxMenu, Task> OnMenuReceived;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubMenuEvent = Controller.SimStore.AddEvent(GsxConstants.EventMenu);
                SubMenuEvent?.OnReceived += OnMenuEvent;
                SubMenuOpen = Controller.SimStore.AddVariable(GsxConstants.VarMenuOpen);
                SubMenuChoice = Controller.SimStore.AddVariable(GsxConstants.VarMenuChoice);
                SubMenuChoice?.OnReceived += OnMenuSelection;

                Controller.OnCouatlStopped += OnCouatlStopped;

                MenuCallbacks.Add(GsxConstants.MenuTugAttach, OnTugQuestion);
                MenuCallbacks.Add(GsxConstants.MenuPushbackRequest, OnPushRequestQuestion);
                MenuCallbacks.Add(GsxConstants.MenuFollowMe, OnFollowMeQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeiceOnPush, OnDeiceQuestion);
                MenuCallbacks.Add(GsxConstants.MenuParkingChange, OnParkingChange);
                MenuCallbacks.Add(GsxConstants.MenuParkingSelect, OnParkingSelect);
                MenuCallbacks.Add(GsxConstants.MenuBoardCrew, OnCrewBoardQuestion);
                MenuCallbacks.Add(GsxConstants.MenuDeboardCrew, OnCrewDeboardQuestion);
                MenuCallbacks.Add(GsxConstants.MenuOperatorHandling, OnOperatorSelection);
                MenuCallbacks.Add(GsxConstants.MenuOperatorCater, OnOperatorSelection);
                MenuCallbacks.Add(GsxConstants.MenuRefuelLevel, OnRefuelLevelQuestion);
                Controller.ServiceDeice.OnStateChanged += OnDeiceStateChanged;

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            try
            {
                Controller.ServiceDeice.OnStateChanged -= OnDeiceStateChanged;
                Controller.OnCouatlStopped -= OnCouatlStopped;

                Controller.SimStore.Remove(GsxConstants.EventMenu)?.OnReceived -= OnMenuEvent;
                Controller.SimStore.Remove(GsxConstants.VarMenuOpen);
                SubMenuChoice?.OnReceived -= OnMenuSelection;
                Controller.SimStore.Remove(GsxConstants.VarMenuChoice);

                MenuCallbacks.Clear();
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
            WasOperatorPreferred = false;
            WasOperatorHandlingSelected = false;
            WasOperatorCateringSelected = false;
            return Task.CompletedTask;
        }

        public virtual void ResetNotRunning()
        {
            FirstReadyReceived = false;
            LastReady = DateTime.Now;
            WasOperatorPreferred = false;
            WasOperatorHandlingSelected = false;
            WasOperatorCateringSelected = false;
            DeiceGateQuestionAnswered = false;
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
            DeiceGateQuestionAnswered = false;
            IsSequenceActive = false;
            IsCommandActive = false;
            LastCommandType = (GsxMenuCommandType)(-1);
            IsToolbarEnabled = false;
            WasOperatorPreferred = false;
            WasOperatorHandlingSelected = false;
            WasOperatorCateringSelected = false;
            ExternalSequence = false;
        }

        public virtual void ResetFlight()
        {
            DeiceGateQuestionAnswered = false;
            IsSequenceActive = false;
            LastCommandType = (GsxMenuCommandType)(-1);
            IsToolbarEnabled = false;
            LastReady = DateTime.Now;
            WasOperatorPreferred = false;
            WasOperatorHandlingSelected = false;
            WasOperatorCateringSelected = false;
            ExternalSequence = false;
        }

        public virtual void AddMenuCallback(string title, Func<IGsxMenu, Task> callback)
        {
            MenuCallbacks.TryAdd(title, callback);
        }

        public virtual void RemoveMenuCallback(string title)
        {
            MenuCallbacks.TryRemove(title, out _);
        }

        public virtual bool MatchTitle(string match)
        {
            return MenuTitle?.StartsWith(match, StringComparison.InvariantCultureIgnoreCase) == true;
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

        protected virtual async Task OnRefuelLevelQuestion(GsxMenu menu)
        {
            Logger.Debug($"Refuel Level Question active");
            if (!AppService.Instance.AircraftController.HasFuelDialog)
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

                await Select(i + 1);
                await Disable();
            }
        }

        protected virtual Task OnPushRequestQuestion(GsxMenu menu)
        {
            Logger.Debug($"Request Pushback Question active");
            return Select(1);
        }

        protected virtual async Task OnTugQuestion(GsxMenu menu)
        {
            Logger.Debug($"Tug Question active");
            if (Profile.RunAutomationService)
            {
                if (Profile.AttachTugDuringBoarding == 2)
                {
                    await Select(1);
                    Logger.Debug($"Skip: {Profile.SkipCrewBoardQuestion}");
                    Logger.Debug($"Answer: {Profile.AnswerCrewBoardQuestion}");
                    if (Profile.SkipCrewBoardQuestion || Profile.AnswerCrewBoardQuestion > 0)
                        await Disable();
                }
                else if (Profile.AttachTugDuringBoarding == 1)
                {
                    await Select(2);
                    if (Profile.SkipCrewBoardQuestion || Profile.AnswerCrewBoardQuestion > 0)
                        await Disable();
                }
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual Task OnFollowMeQuestion(GsxMenu menu)
        {
            Logger.Debug($"FollowMe Question active");
            if (Profile.RunAutomationService && Profile.SkipFollowMe)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(GsxMenuCommand.Select(2, GsxConstants.MenuFollowMe, ["No"]));
                sequence.Commands.Add(GsxMenuCommand.Operator());
                sequence.ResetMenuCheck = () => true;
                sequence.EnableMenuAfterResetCheck = () => Profile.EnableMenuForSelection;
                return RunSequence(sequence);
            }
            else
                return Task.CompletedTask;
        }

        protected virtual Task OnDeiceQuestion(GsxMenu menu)
        {
            Logger.Debug($"Deice Push Question active");
            if ((Profile.RunAutomationService || Profile.PilotsDeckIntegration) && Profile.KeepDirectionMenuOpen && Profile.AnswerDeiceOnReopen && DeiceGateQuestionAnswered)
                return Select(2);
            else
                return Task.CompletedTask;
        }

        protected virtual Task OnDeiceStateChanged(IGsxService gsxService)
        {
            if (!Controller.CouatlVarsValid)
                return Task.CompletedTask;

            if (gsxService.State == GsxServiceState.Requested)
                BlockMenuUpdates(true, "Deice Questions");
            else
                BlockMenuUpdates(false);

            return Task.CompletedTask;
        }

        protected virtual Task OnParkingSelect(GsxMenu menu)
        {
            Logger.Debug($"Select Parking active");

            if (IsSequenceAllowed)
            {
                Tracker.Track(AppNotification.GateSelect);
                Tracker.Clear(AppNotification.GateMove);
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnParkingChange(GsxMenu menu)
        {
            Logger.Debug($"Change Parking active");
            WasOperatorHandlingSelected = false;
            WasOperatorCateringSelected = false;

            Tracker.Clear(AppNotification.GateSelect);
            if (Controller.AutomationState != AutomationState.Flight)
                Tracker.Track(AppNotification.GateMove);

            return Tracker.CaptureGate(this);
        }

        protected virtual async Task OnCrewBoardQuestion(GsxMenu menu)
        {
            Logger.Debug($"Board Crew Question active");
            if (Profile.RunAutomationService && Profile.AnswerCrewBoardQuestion > 0)
            {
                await Select(Profile.AnswerCrewBoardQuestion);
                await Disable();
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual async Task OnCrewDeboardQuestion(GsxMenu menu)
        {
            Logger.Debug($"Deboard Crew Question active");
            if (Profile.RunAutomationService && Profile.AnswerCrewDeboardQuestion > 0)
            {
                await Select(Profile.AnswerCrewDeboardQuestion);
                await Disable();
            }

            _ = TaskTools.RunDelayed(() => this.BlockMenuUpdates(false), Config.OperatorSelectTimeout, RequestToken);
        }

        protected virtual async Task OnOperatorSelection(GsxMenu menu)
        {
            Logger.Debug($"Operator Question active");
            SetOperatorSelected(false);

            if (!Profile.RunAutomationService)
                return;

            var gsxOperator = GsxOperator.OperatorSelection(Profile, MenuLines);
            int doSelection = 0;
            if (gsxOperator != null)
            {
                if (gsxOperator.Preferred && (Profile.OperatorAutoSelect || Profile.OperatorPreferenceSelect))
                {
                    Logger.Information($"Selecting Operator '{gsxOperator.Title}' (Preferred)");
                    if (!WasOperatorPreferred)
                        WasOperatorPreferred = MatchTitle(GsxConstants.MenuOperatorHandling);
                    doSelection = gsxOperator.Number;
                }
                else if (Profile.OperatorAutoSelect)
                {
                    Logger.Information($"Selecting Operator '{gsxOperator.Title}' (GSX Choice)");
                    doSelection = gsxOperator.Number;
                }
            }
            else if (Profile.OperatorAutoSelect)
            {
                Logger.Warning($"Selecting Operator #1 - no Matches found");
                doSelection = 1;
            }

            if (doSelection > 0)
            {
                SetOperatorSelected(true);
                await Select(doSelection);
            }
        }

        protected virtual void SetOperatorSelected(bool value)
        {
            if (MatchTitle(GsxConstants.MenuOperatorHandling))
                WasOperatorHandlingSelected = value;
            else
                WasOperatorCateringSelected = value;
            WasOperatorSelected = value;
        }

        protected virtual Task OnMenuSelection(ISimResourceSubscription sub, object value)
        {
            double num = sub.GetNumber();
            if (num <= -2)
            {
                Logger.Debug($"Received Menu Selection: Clear ({num})");
            }
            else if (num == -1)
            {
                Logger.Debug($"Received Menu Selection: Timeout ({num})");
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
            }
            else if (MatchTitle(GsxConstants.MenuDeiceOnPush))
            {
                Logger.Debug($"Deice Push Question was answered: {num + 1} - '{GetMenuLine(num)}'");
                DeiceGateQuestionAnswered = true;
            }
            else if (IsChangeGateMenu && GetMenuLine(num).Contains("warp", StringComparison.InvariantCultureIgnoreCase))
            {
                Tracker.TrackTimeout(AppNotification.UpdatesBlocked, Config.MenuOpenTimeout, "Warp to Gate");
                if (Profile.RunAutomationService)
                {
                    Logger.Information($"Automation: Open Menu in {(Config.MenuOpenTimeout / 1000.0):F1}s for Gate/Pad Menu ...");
                    NotificationManager.MenuOpenDelayed = DateTime.Now + TimeSpan.FromMilliseconds(Config.MenuOpenTimeout);
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
            else if (IsSelectGateMenu && Controller.AutomationState == AutomationState.TaxiOut && Controller.IsDeiceAvail && GetMenuLine(num).Contains(GsxConstants.MenuLineDeice, StringComparison.InvariantCultureIgnoreCase))
                Tracker.Track(AppNotification.GateSelect);
            else if (!string.IsNullOrWhiteSpace(GetMenuLine(num)))
                Logger.Debug($"Received Menu Selection: {num + 1} - '{GetMenuLine(num)}'");
            else
                Logger.Debug($"Received Menu Selection: {num + 1}");

            if (num >= -1)
                Tracker.Clear(AppNotification.GsxQuestion);
            LastSelectionTime = num >= 0 || num == -2 ? DateTime.Now : DateTime.MaxValue;

            TaskTools.RunPool(() => NotificationManager.ClearMenu());
            ReadyReceived = false;
            LastMenuSelection = num;

            if (num >= 0 && IsToolbarEnabled &&
                ((!Profile.SkipCrewBoardQuestion && Profile.AnswerCrewBoardQuestion == 0 && MatchTitle(GsxConstants.MenuBoardCrew))
                || (!Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion == 0 && MatchTitle(GsxConstants.MenuDeboardCrew))))
                return Disable();
            else
                return Task.CompletedTask;
        }

        public virtual void OnToolbarEvent(string data)
        {
            IsToolbarEnabled = data?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true;
            Logger.Debug($"Toolbar {(IsToolbarEnabled ? "enabled" : "disabled")} by User");
            if (IsInitialized && Config.GsxToolbarFixes)
            {
                if (!IsToolbarEnabled)
                {
                    Logger.Debug("Close Menu on disabled");
                    _ = TaskTools.RunPool(async () =>
                    {
                        await SubMenuEvent.WriteValue((int)GsxMenuState.TIMEOUT);
                        await WaitInterval(0.5);
                        await SubMenuChoice?.WriteValue(-1);
                    });
                }
                else if ((DateTime.Now - LastReady).TotalMilliseconds >= Config.MenuCheckInterval)
                {
                    Logger.Debug("Open Menu on enabled-ready");
                    _ = TaskTools.RunPool(async () =>
                    {
                        await SubMenuEvent.WriteValue((int)GsxMenuState.TIMEOUT);
                        await WaitInterval(0.5);
                        await SubMenuOpen?.WriteValue(1);
                    });
                }
            }
        }

        protected virtual Task OnMenuEvent(ISimResourceSubscription sub, object value)
        {
            try
            {
                var state = sub.GetValue<int>();
                if (!Controller.IsActive || state > (int)GsxMenuState.DISABLED)
                    return Task.CompletedTask;

                MenuState = (GsxMenuState)state;
                Logger.Debug($"Received Menu Event: {TextMenuState}");

                if (!FirstReadyReceived && MenuState == GsxMenuState.READY)
                {
                    Logger.Debug($"First Menu Ready received");
                    FirstReadyReceived = true;
                }

                bool menuChanged = false;
                LastTitle = MenuTitle;
                if (MenuState == GsxMenuState.READY)
                {
                    menuChanged = UpdateMenu();
                    LastReady = DateTime.Now;

                    if (IsGateMenu)
                    {
                        Tracker.Clear(AppNotification.GateSelect);
                        Tracker.Clear(AppNotification.GateMove);
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
                ReadyReceived = MenuState == GsxMenuState.READY;

                if (ReadyReceived && menuChanged)
                {
                    var matchingCallbacks = MenuCallbacks.Where(c => MatchTitle(c.Key));
                    foreach (var callback in matchingCallbacks)
                    {
                        _ = TaskTools.RunPool(async () =>
                        {
                            await WaitInterval();
                            await callback.Value.Invoke(this);
                        }, RequestToken);
                    }
                }

                if (MenuState == GsxMenuState.READY)
                    _ = TaskTools.RunPool(() => OnMenuReady?.Invoke(this));

                _ = TaskTools.RunPool(() => OnMenuReceived?.Invoke(this));
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

        protected virtual bool UpdateMenu()
        {
            if (File.Exists(PathMenu))
            {
                MenuLines.Clear();

                var fileLines = File.ReadAllLines(PathMenu).ToArray();
                var fileIndex = 0;
                MenuTitle = fileLines[fileIndex++];
                while (fileIndex < fileLines.Length)
                    MenuLines.Add(fileLines[fileIndex++]);
                Logger.Verbose($"Read {MenuLineCount} Lines");
            }
            else
                Logger.Warning($"GSX Menu files does not exist! ({PathMenu})");

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
            if (!ReadyReceived)
                Logger.Debug($"Wait for Menu Ready ...");
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

        protected virtual async Task WriteMenuOpen(bool second = true)
        {
            Logger.Debug($"Open Menu ...");
            await SubMenuOpen.WriteValue(1);
            await WaitInterval(0.5);
            if (second)
                await SubMenuOpen.WriteValue(1);
        }

        protected virtual async Task<bool> Open(bool enableToolbar, int timeout = 0, bool forceEnable = false) //GSX Workaround (forcing enable on fail else not ready ...)
        {
            bool result;
            try
            {
                Logger.Debug($"Open - IsToolbarEnabled {IsToolbarEnabled} | enableToolbar {enableToolbar} | PilotsDeckIntegration {Profile.PilotsDeckIntegration} | forceEnable {forceEnable}");
                ReadyReceived = false;
                if (((IsToolbarEnabled && !enableToolbar) || (IsToolbarEnabled && Profile.PilotsDeckIntegration)) && !forceEnable)
                {
                    Logger.Debug("Disable GSX Toolbar ...");
                    if (Controller.IsWalkaround)
                        await AppService.Instance.CommBus.SendGsxMenu("Close"); //GSX Workaround - Menu not reacting to disable, hide or close (-1) in Walkaround
                    else
                        await SetMenuState(GsxMenuState.DISABLED);
                    IsToolbarEnabled = false;
                    await WaitInterval();
                    await WriteMenuOpen();
                }
                else if ((!IsToolbarEnabled && enableToolbar && Profile.EnableMenuForSelection && !Profile.PilotsDeckIntegration) || forceEnable)
                {
                    Logger.Information($"Enable GSX Toolbar ...");
                    await AppService.Instance.CommBus.SendGsxMenu("Open");
                    IsToolbarEnabled = true;
                    await WaitInterval(2);
                    if (!ReadyReceived)
                        await WriteMenuOpen(false);
                }
                else
                    await WriteMenuOpen(!IsToolbarEnabled);

                result = await WaitMenuReady(timeout == 0 ? Config.MenuOpenTimeout : timeout);
                if (!result)
                {
                    if (forceEnable)
                        await Close();
                    else
                        await Disable();
                }
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
            Logger.Debug($"Menu Select Item {number} => Value {number - 1}");
            ReadyReceived = false;
            if (await SubMenuChoice.WriteValue(number - 1))
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
            if (await SetMenuState(GsxMenuState.TIMEOUT))
            {
                await WaitInterval(0.5);
                await SubMenuChoice.WriteValue(-1);
                await WaitInterval(0.5);
                return true;
            }
            else
                return false;
        }

        protected virtual async Task<bool> Disable()
        {
            ReadyReceived = false;
            Logger.Debug($"Disable Menu");
            if (await SubMenuEvent.WriteValue((int)GsxMenuState.DISABLED))
            {
                await WaitInterval(0.5);
                await SubMenuChoice.WriteValue(-1);
                await WaitInterval(0.5);
                return true;
            }
            else
                return false;
        }

        protected virtual async Task<bool> SequenceWaiter(string type, Func<bool> allowed, int interval = 1000, int timeout = 30000)
        {
            bool result = allowed();
            if (!result)
            {
                Logger.Debug($"{type} Execution blocked - waiting ...");
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

            bool enableOperator = sequence.HasOperatorSelection && !Profile.OperatorAutoSelect && ((!sequence.IsHandlingOperator && !WasOperatorCateringSelected) || (sequence.IsHandlingOperator && !WasOperatorHandlingSelected));
            bool result = false;
            int counter = 0;
            LastCommandType = (GsxMenuCommandType)(-1);
            foreach (var command in sequence.Commands)
            {
                result = await RunCommand(command, enableOperator || sequence.EnableMenu);
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

            if (!ReadyReceived || (enableMenu && !IsToolbarEnabled))
            {
                if (!await Open(enableMenu))
                {
                    await WaitInterval();
                    if (await Open(enableMenu, Config.MenuOpenTimeout, Config.GsxMenuTimeoutFix))
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
            }
            else
            {
                await WaitInterval();
                return true;
            }
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
            await WaitInterval();
            int timeWaited = 0;
            if (!ReadyReceived && !IsOperatorMenu)
                Logger.Debug("Waiting for Operator Menu ...");
            while (timeWaited <= Config.OperatorWaitTimeout && !ReadyReceived && !IsOperatorMenu && !RequestToken.IsCancellationRequested)
                timeWaited += await WaitInterval(0.5);

            if (timeWaited > Config.OperatorWaitTimeout && !IsOperatorMenu)
            {
                Logger.Debug("No Operator Menu detected");
                WasOperatorSelected = true;
            }
            else if (IsOperatorMenu)
            {
                timeWaited = 0;
                if (!WasOperatorSelected)
                    Logger.Information($"Waiting for {(Profile.OperatorAutoSelect ? "automatic" : "manual")} Operator Selection (Timeout {Config.OperatorSelectTimeout / 1000}s) ... ");
                while (timeWaited <= Config.OperatorSelectTimeout && IsOperatorMenu && MenuState < GsxMenuState.TIMEOUT && !WasOperatorSelected && !RequestToken.IsCancellationRequested)
                    timeWaited += await WaitInterval(0.5);

                if (timeWaited > Config.OperatorSelectTimeout && !WasOperatorSelected)
                {
                    Logger.Warning($"{(Profile.OperatorAutoSelect ? "Automatic" : "Manual")} Operator Selection timed out{(MenuState < GsxMenuState.TIMEOUT ? " - closing Menu" : "")}");
                    if (MenuState < GsxMenuState.TIMEOUT)
                        await Disable();
                    await WaitInterval();
                }
            }
            return WasOperatorSelected;
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
    }
}
