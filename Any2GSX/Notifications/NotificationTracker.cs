using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public enum AppNotification
    {
        None = 0,
        Stopping = 1,
        Starting = 2,
        Loading = 3,
        GsxRestart = 10,
        UpdatesBlocked = 20,
        GsxRefresh = 40,
        GsxQuestion = 45,
        ServiceCall = 50,
        ServiceCancel = 51,
        MenuSequence = 70,
        MenuCommand = 80,
        GateSelect = 110,
        GateMove = 120,
        GateEquip = 130,
        OperateStairs = 135,
        OperateJetway = 136,
        GateDepart = 140,
        OfpImported = 145,
        OfpCheck = 150,
        ServiceComplete = 218,
        ServiceActive = 219,
        ServiceDeboard = 220,
        ServiceBoard = 221,
        ServiceRefuel = 222,
        SheetFinal = 300,
        PushPhase = 310,
        PushReleaseBrake = 320,
        PushDirectionDisplayed = 330,
        PushDirectionSelected = 331,
    }

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
        WarpGate = 10,
    }

    public class NotificationTracker()
    {
        protected virtual ConcurrentDictionary<AppNotification, Notification> Notifications { get; } = [];
        public virtual bool HasNotification => !Notifications.IsEmpty;
        public virtual AppNotification CurrentType => GetCurrentKey();
        public virtual Notification Current => GetCurrent();
        public virtual SmartButtonCall SmartButton { get; set; } = SmartButtonCall.None;
        public virtual string LastGateSelection { get; protected set; } = "";
        public virtual bool HasGateSelection => !string.IsNullOrWhiteSpace(LastGateSelection);
        public virtual string LastGateActive { get; protected set; } = "";
        public virtual bool HasGateActive => !string.IsNullOrWhiteSpace(LastGateActive);


        public virtual void Reset()
        {
            Notifications.Clear();
            ResetGate();
            SmartButton = SmartButtonCall.None;
        }

        public virtual void ResetGate()
        {
            Clear(AppNotification.GateDepart);
            Clear(AppNotification.GateSelect);
            Clear(AppNotification.GateMove);
            LastGateSelection = "";
            LastGateActive = "";
        }

        public virtual void Track(AppNotification call)
        {
            if (call == AppNotification.None)
                return;

            if (!Notifications.ContainsKey(call))
            {
                Notifications.Add(call, new Notification(call));
                Logger.Debug($"Tracking {call}");
            }
        }

        public virtual void TrackTimeout(AppNotification call, int ms = 0, string message = "")
        {
            if (call == AppNotification.None)
                return;

            if (!Notifications.TryGetValue(call, out var notification))
            {
                Notifications.Add(call, new Notification(call, message, ms));
                Logger.Debug($"Tracking Timeout {call} ({ms}ms)");
            }
            else
            {
                notification.SetTime(ms);
                notification.Message = message;
            }
        }

        public virtual void TrackMessage(AppNotification call, string message = "")
        {
            if (call == AppNotification.None)
                return;

            if (!Notifications.TryGetValue(call, out var notification))
            {
                Notifications.Add(call, new Notification(call, message));
                Logger.Debug($"Tracking Message {call}: '{message}'");
            }
            else
            {
                notification.ResetTime();
                notification.Message = message;
            }
        }

        public virtual void Clear(AppNotification call)
        {
            if (Notifications.TryRemove(call, out _))
                Logger.Debug($"Cleared {call}");
        }

        public virtual void CheckNotifications()
        {
            List<AppNotification> clearList = [];
            foreach (var call in Notifications)
            {
                if (call.Value.ClearTime <= DateTime.Now)
                    clearList.Add(call.Key);
            }

            foreach (var call in clearList)
                Clear(call);
        }

        public virtual bool IsActive(AppNotification call)
        {
            return Notifications.ContainsKey(call);
        }

        protected virtual AppNotification GetCurrentKey()
        {
            if (!Notifications.IsEmpty)
                return Notifications.Keys.Where(s => s != AppNotification.PushDirectionDisplayed && s != AppNotification.PushDirectionSelected).Min();
            else
                return AppNotification.None;
        }

        public virtual Notification GetCurrent()
        {
            if (HasNotification)
                return Notifications[GetCurrentKey()];
            else
                return new Notification(AppNotification.None);
        }

        public virtual Notification Get(AppNotification call)
        {
            if (Notifications.TryGetValue(call, out var notification))
                return notification;
            else
                return new Notification(AppNotification.None);
        }

        protected static string ParseGate(string parking)
        {
            string capture = "";
            try
            {
                if (parking == null)
                    return capture;

                Logger.Debug($"Trying to get Gate/Pad from Line '{parking}'");

                if (AppService.Instance.GsxController.AutomationState == AutomationState.TaxiOut)
                {
                    capture = parking
                              .Replace("deice:", "", StringComparison.InvariantCultureIgnoreCase)
                              .Replace("deice", "", StringComparison.InvariantCultureIgnoreCase)
                              .Replace("de-ice:", "", StringComparison.InvariantCultureIgnoreCase)
                              .Replace("de-ice", "", StringComparison.InvariantCultureIgnoreCase)
                              .Trim();
                }
                else
                {
                    var matches = GsxConstants.MenuRegexGate.Matches(parking);
                    if (matches.Count > 0)
                        capture = matches[0].Value.Trim()
                                 .Replace("Gate:", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Stand:", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Gate", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Stand", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Trim();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                capture = "";
            }

            return capture;
        }

        public virtual void CaptureSelectedGate(string line)
        {
            string capture = ParseGate(line);

            if (capture != LastGateSelection)
            {
                Logger.Debug($"Last selected Gate changed: '{capture}'");
                LastGateSelection = capture;
            }

            if (!string.IsNullOrWhiteSpace(LastGateSelection) && LastGateSelection != LastGateActive && AppService.Instance.GsxController.AutomationState != AutomationState.Flight)
            {
                Track(AppNotification.GateMove);
                Clear(AppNotification.GateSelect);
            }
            else if (string.IsNullOrWhiteSpace(LastGateSelection) || AppService.Instance.GsxController.AutomationState == AutomationState.Flight)
                Clear(AppNotification.GateMove);
        }

        public virtual Task CaptureActiveGate(string name)
        {
            string capture = ParseGate(name);

            if (capture != LastGateActive)
            {
                Logger.Information($"GSX Gate Selection changed to '{capture}'");
                LastGateActive = capture;
            }

            if (!string.IsNullOrWhiteSpace(LastGateActive))
            {
                Clear(AppNotification.GateMove);
                Clear(AppNotification.GateSelect);
            }

            return Task.CompletedTask;
        }
    }

    public class Notification
    {
        public virtual AppNotification Id { get; }
        public virtual DateTime ClearTime { get; set; } = DateTime.MaxValue;
        public virtual bool HasTime => ClearTime < DateTime.MaxValue;
        public virtual string Message { get; set; } = "";
        public virtual bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public Notification(AppNotification notification)
        {
            Id = notification;
        }

        public Notification(AppNotification notification, int ms)
        {
            Id = notification;
            SetTime(ms);
        }

        public Notification(AppNotification notification, string message)
        {
            Id = notification;
            Message = message;
        }

        public Notification(AppNotification notification, string message, int ms) : this(notification, ms)
        {
            Message = message;
        }

        public virtual void SetTime(int ms = 0)
        {
            if (ms <= 0)
                ms = AppService.Instance.Config.StatusTimeoutDefault;
            ClearTime = DateTime.Now + TimeSpan.FromMilliseconds(ms);
        }

        public virtual void ResetTime()
        {
            ClearTime = DateTime.MaxValue;
        }
    }
}
