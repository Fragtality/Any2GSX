using Any2GSX.GSX;
using Any2GSX.PluginInterface.Interfaces;
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

        public override Task Init()
        {
            return Task.CompletedTask;
        }

        public override async Task SetConnected(bool connected, string profile)
        {
            var message = new EfbMessage() { ProfileName = profile };
            if (connected)
                message.ConnectionState = "Connected";
            else
                message.ConnectionState = "Disconnected";

            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetCouatlVars(string state)
        {
            var message = new EfbMessage() { CouatlVarsValid = state };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetBoardCargoInfo(int percent)
        {
            CargoInfo = percent;
            await UpdateProgress();
        }

        public override async Task SetBoardPaxInfo(int pax)
        {
            PaxInfo = pax;
            await UpdateProgress();
        }

        public override async Task SetDeboardCargoInfo(int percent)
        {
            CargoInfo = percent;
            await UpdateProgress();
        }

        public override async Task SetDeboardPaxInfo(int pax)
        {
            PaxInfo = pax;
            await UpdateProgress();
        }

        protected virtual async Task UpdateProgress()
        {
            string label = "";
            string info = "";
            bool isBoard = GsxController?.ServiceBoard?.IsActive == true;
            if (PaxInfo >= 0 && CargoInfo >= 0)
            {
                label = isBoard ? "Boarding Progress" : "Deboarding Progress";
                info = $"{PaxInfo} Pax / {CargoInfo}% Cargo (on Board)";
            }

            var message = new EfbMessage() { ProgressLabel = label, ProgressInfo = info };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetDepartureServices(int completed, int running, int total)
        {
            string text = "";
            if (AutomationController.State == AutomationState.Departure)
                text = $"{completed + running} / {total}";

            var message = new EfbMessage() { DepartureServices = text };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task ClearDepartureServices()
        {
            var message = new EfbMessage() { DepartureServices = "" };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetMenuLines(List<string> menuLines)
        {
            string[] menuArray = new string[10];
            for (int i = 0; i < menuArray.Length; i++)
                menuArray[i] = (i < menuLines.Count ? menuLines[i] : "");

            var message = new EfbMessage() { MenuLines = menuArray };
            await CommBus.SendEfbUpdate(message);
        }

        protected override Task SetMenuLine(int index, string text)
        {
            return Task.CompletedTask;
        }

        public override async Task SetMenuTitle(string title)
        {
            if (GsxController.Menu.IsGateMenu)
                title = title.Replace(GsxConstants.MenuGate, "", System.StringComparison.InvariantCultureIgnoreCase);

            var message = new EfbMessage() { MenuTitle = title };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetSmartCall(SmartButtonCall call, string callInfo)
        {
            string text = call switch
            {
                SmartButtonCall.None => "",
                SmartButtonCall.Connect => "Connect Jetway/Stairs",
                SmartButtonCall.NextService => $"Call Next Service: {callInfo}",
                SmartButtonCall.PushCall => "Call Pushback",
                SmartButtonCall.PushStop => "Stop Pushback",
                SmartButtonCall.PushConfirm => "Confirm Engine Start",
                SmartButtonCall.Deice => "Start De-icing",
                SmartButtonCall.ClearGate => "Clear Gate",
                SmartButtonCall.Deboard => "Call Deboard",
                SmartButtonCall.SkipTurn => "Skip Turnaround",
                _ => "",
            };

            var message = new EfbMessage() { SmartCall = text };
            await CommBus.SendEfbUpdate(message);
        }

        public override async Task SetState(AutomationState phase, string status)
        {
            string text = phase.ToString().Replace("Taxi", "Taxi-");
            if (!string.IsNullOrEmpty(status))
                text = $"{text} - {status}";

            var message = new EfbMessage() { PhaseStatus = text };
            await CommBus.SendEfbUpdate(message);
        }
    }
}