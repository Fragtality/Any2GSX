using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX
{
    public class GsxGate
    {
        public virtual string AutoSelectionPushback { get; protected set; } = "";
        public virtual bool HasSelectionPushback => !string.IsNullOrWhiteSpace(AutoSelectionPushback);
        public virtual JsonNode AutoSelectionArrivalGate { get; protected set; } = null;
        public virtual bool HasSelectionArrivalGate => !string.IsNullOrWhiteSpace(AutoSelectionArrivalGate?.ToString());
        public virtual string LastGateSelection { get; protected set; } = "";
        public virtual bool HasGateSelection => !string.IsNullOrWhiteSpace(LastGateSelection);
        public virtual string LastGateActive { get; protected set; } = "";
        public virtual bool HasGateActive => !string.IsNullOrWhiteSpace(LastGateActive);

        protected virtual NotificationTracker Tracker => AppService.Instance.NotificationTracker;
        protected virtual CancellationToken RequestToken => AppService.Instance.RequestToken;

        public event Func<string, Task> OnPushDirectionChanged;
        public event Func<string, Task> OnArrivalGateChanged;

        public virtual void Init()
        {

        }

        public virtual void FreeResources()
        {

        }

        public virtual void Reset(bool resetArrival = true)
        {
            Tracker.Clear(AppNotification.GateDepart);
            Tracker.Clear(AppNotification.GateSelect);
            Tracker.Clear(AppNotification.GateMove);
            LastGateSelection = "";
            LastGateActive = "";
            SetPushDirection("");
            if (resetArrival)
                SetArrivalGate(null);
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
                                 .Replace("Ramp:", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Gate", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Stand", "", StringComparison.InvariantCultureIgnoreCase)
                                 .Replace("Ramp", "", StringComparison.InvariantCultureIgnoreCase)
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
                Tracker.Track(AppNotification.GateMove);
                Tracker.Clear(AppNotification.GateSelect);
            }
            else if (string.IsNullOrWhiteSpace(LastGateSelection) || AppService.Instance.GsxController.AutomationState == AutomationState.Flight)
                Tracker.Clear(AppNotification.GateMove);
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
                Tracker.Clear(AppNotification.GateMove);
                Tracker.Clear(AppNotification.GateSelect);
            }

            return Task.CompletedTask;
        }
    }
}
