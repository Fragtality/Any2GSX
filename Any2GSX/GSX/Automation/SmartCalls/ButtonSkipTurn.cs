using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonSkipTurn() : SmartButton(SmartButtonCall.SkipTurn)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.TurnAround && !Flightplan.IsLoaded;
        }

        public override bool CanExecute()
        {
            return CheckActive();
        }

        public override async Task<bool> Run()
        {
            await AutomationController.StateChange(AutomationState.Departure);
            return true;
        }
    }
}
