using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonGateAction() : SmartButton(SmartButtonCall.GateAction)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.TaxiIn && Tracker.IsActive(AppNotification.GateMove);
        }

        public override bool CanExecute()
        {
            return CheckActive();
        }

        public override Task<bool> Run()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select((int)Profile.ClearGateMenuOption, GsxConstants.MenuParkingChange));
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && Profile.ClearGateMenuOption < GsxChangePark.ClearAI;
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => false;

            return GsxMenu.RunSequence(sequence);
        }
    }
}
