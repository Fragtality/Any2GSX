using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonPushStop() : SmartButton(SmartButtonCall.PushStop)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.Pushback && ServicePushBack.PushStatus > 4 && ServicePushBack.PushStatus < 8 && (!ExternalServiceControl || (ExternalServiceControl && AutomationController.ExternalSmartButton));
        }

        public override bool CanExecute()
        {
            return CheckActive();
        }

        public override Task<bool> Run()
        {
            return ServicePushBack.EndPushback();
        }
    }
}
