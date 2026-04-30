using Any2GSX.GSX;
using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.Notifications
{
    public class EfbAppConnector : ExternalConnector
    {
        protected virtual int CargoInfo { get; set; } = -1;
        protected virtual int PaxInfo { get; set; } = -1;

        public override Task FreeRessources()
        {
            return Task.CompletedTask;
        }

        public override Task CheckState()
        {
            return Task.CompletedTask;
        }

        public override Task Init()
        {
            return Task.CompletedTask;
        }

        public override Task SetConnected(bool connected, string profile)
        {
            var message = new EfbMessage() { ProfileName = profile };
            if (connected)
                message.AppConnectionState = "Connected";
            else
                message.AppConnectionState = "Disconnected";

            return CommBus.SendEfbUpdate(message);
        }

        public override Task SetAircraftConnected(bool connected)
        {
            var message = new EfbMessage();
            if (connected)
                message.AircraftConnectionState = "Connected";
            else
                message.AircraftConnectionState = "Disconnected";

            return CommBus.SendEfbUpdate(message);
        }

        public override Task SetCouatlVars(string state)
        {
            var message = new EfbMessage() { CouatlVarsValid = state };
            return CommBus.SendEfbUpdate(message);
        }

        public override Task SetBoardCargoInfo(int percent)
        {
            CargoInfo = percent;
            return UpdateProgress();
        }

        public override Task SetBoardPaxInfo(int pax)
        {
            PaxInfo = pax;
            return UpdateProgress();
        }

        public override Task SetDeboardCargoInfo(int percent)
        {
            CargoInfo = percent;
            return UpdateProgress();
        }

        public override Task SetDeboardPaxInfo(int pax)
        {
            PaxInfo = pax;
            return UpdateProgress();
        }

        protected virtual Task UpdateProgress()
        {
            string label = "";
            string info = "";
            bool isBoard = GsxController?.ServiceBoard?.IsActive == true;
            if (PaxInfo >= 0 || CargoInfo >= 0)
            {
                label = isBoard ? "Boarding Progress" : "Deboarding Progress";
                info = $"{PaxInfo} Pax / {CargoInfo}% Cargo (on Board)";
            }

            var message = new EfbMessage() { ProgressLabel = label, ProgressInfo = info };
            return CommBus.SendEfbUpdate(message);
        }

        public override Task SetDepartureServices(int completed, int running, int total)
        {
            string text = "";
            if (AutomationController.State == AutomationState.Departure)
                text = $"{completed + running} / {total}";

            var message = new EfbMessage() { DepartureServices = text };
            return CommBus.SendEfbUpdate(message);
        }

        public override Task ClearDepartureServices()
        {
            var message = new EfbMessage() { DepartureServices = "" };
            return CommBus.SendEfbUpdate(message);
        }

        public override Task SetMenuLines(List<string> menuLines)
        {
            string[] menuArray = new string[10];
            for (int i = 0; i < menuArray.Length; i++)
                menuArray[i] = (i < menuLines.Count ? menuLines[i] : "");

            var message = new EfbMessage() { MenuLines = menuArray };
            return CommBus.SendEfbUpdate(message);
        }

        protected override Task SetMenuLine(int index, string text)
        {
            return Task.CompletedTask;
        }

        public override Task SetMenuTitle(string title)
        {
            if (GsxController.Menu.IsGateMenu)
                title = title.Replace(GsxConstants.MenuGate, "", System.StringComparison.InvariantCultureIgnoreCase);

            var message = new EfbMessage() { MenuTitle = title };
            return CommBus.SendEfbUpdate(message);
        }

        public override async Task SetSmartCall(SmartButtonCall call, string callInfo, bool force = false)
        {
            string text = call switch
            {
                SmartButtonCall.None => "",
                SmartButtonCall.Connect => "Connect Jetway/Stairs",
                SmartButtonCall.NextService => $"Call Next Service: {callInfo}",
                SmartButtonCall.PushCall => "Call Pushback",
                SmartButtonCall.PushStop => $"{callInfo} Pushback",
                SmartButtonCall.PushConfirm => "Confirm Engine Start",
                SmartButtonCall.Deice => $"{callInfo} DeIce",
                SmartButtonCall.ClearGate => callInfo,
                SmartButtonCall.Deboard => "Call Deboard",
                SmartButtonCall.SkipTurn => "Skip Turnaround",
                SmartButtonCall.WarpGate => "Warp Gate",
                _ => "",
            };

            if (ReportedCall != call || ReportedCallInfo != text || force)
            {
                var message = new EfbMessage() { SmartCall = text };
                await CommBus.SendEfbUpdate(message);
            }
            ReportedCall = call;
            ReportedCallInfo = text;
        }

        public override async Task SetState(AutomationState phase, Notification notification)
        {
            string status = "";
            switch (notification.Id)
            {
                case AppNotification.UpdatesBlocked:
                    status = $"Waiting for: {notification.Message}";
                    break;
                case AppNotification.GsxRestart:
                    status = "GSX Restart in Progress ...";
                    break;
                case AppNotification.GsxRefresh:
                    status = "Refresh SimBrief in GSX ...";
                    break;
                case AppNotification.GsxQuestion:
                    if (notification.HasMessage)
                        status = $"GSX Question: {notification.Message}";
                    break;
                case AppNotification.ServiceCall:
                    if (notification.HasMessage)
                        status = $"Call {notification.Message} Service ...";
                    break;
                case AppNotification.MenuCommand:
                    if (notification.Message.StartsWith("Open", StringComparison.InvariantCultureIgnoreCase))
                        status = $"Open Menu ...";
                    break;
                case AppNotification.MenuSequence:
                    status = $"Menu Call in Progress ...";
                    break;
                case AppNotification.GateSelect:
                    if (GsxController.IsDeiceAvail && GsxController.AutomationState == AutomationState.TaxiOut)
                        status = $"Select Pad!";
                    else
                        status = $"Select Gate!";
                    break;
                case AppNotification.GateMove:
                    if (Tracker.HasCapture)
                    {
                        if (phase <= AutomationState.Departure)
                            status = "Move to Gate!";
                        else if (phase == AutomationState.TaxiOut)
                            status = $"Deice Pad: {Tracker.LastCapturedGate}";
                        else if (phase == AutomationState.TaxiIn)
                            status = $"Selected Gate: {Tracker.LastCapturedGate}";
                    }
                    else
                        status = "Move to Gate!";
                    break;
                case AppNotification.GateEquip:
                    int countdown = AutomationController.EquipManager.GetCountdown();
                    if (countdown > 0)
                        status = $"Equipment in {countdown}s ...";
                    break;
                case AppNotification.GateDepart:
                    status = "Aircraft not ready for Departure!";
                    break;
                case AppNotification.OfpImported:
                    status = "Departure triggered - OFP Imported";
                    break;
                case AppNotification.OfpCheck:
                    if (notification.ClearTime > DateTime.Now)
                        status = $"Next Simbrief Check in {(int)((notification.ClearTime - DateTime.Now).TotalSeconds)}s ...";
                    break;
                case AppNotification.OperateJetway:
                    status = "Jetway Operating ...";
                    break;
                case AppNotification.OperateStairs:
                    status = "Stairs Operating ...";
                    break;
                case AppNotification.ServiceComplete:
                    if (notification.HasMessage)
                        status = $"{notification.Message} Service completed";
                    break;
                case AppNotification.ServiceActive:
                    if (notification.HasMessage)
                        status = $"{notification.Message} Service is active";
                    break;
                case AppNotification.ServiceDeboard:
                    if (notification.HasMessage)
                        status = notification.Message;
                    break;
                case AppNotification.ServiceBoard:
                    if (notification.HasMessage)
                        status = notification.Message;
                    break;
                case AppNotification.ServiceRefuel:
                    if (notification.HasMessage)
                        status = notification.Message;
                    break;
                case AppNotification.SheetFinal:
                    if (notification.ClearTime > DateTime.Now)
                        status = $"Final Loadsheet in {(int)((notification.ClearTime - DateTime.Now).TotalSeconds)}s ...";
                    break;
                case AppNotification.PushPhase:
                    if (notification.HasMessage)
                        status = $"Push: {notification.Message}";
                    break;
                default:
                    if (notification.Id != AppNotification.None)
                        status = notification.Id.ToString();
                    break;
            }

            if (ReportedPhase != phase || ReportedStatusMessage != status)
            {
                var message = new EfbMessage()
                {
                    FlightPhase = phase.ToString().Replace("Taxi", "Taxi-"),
                    PhaseStatus = status
                };
                await CommBus.SendEfbUpdate(message);
            }
            ReportedPhase = phase;
            ReportedStatusMessage = status;
        }
    }
}
