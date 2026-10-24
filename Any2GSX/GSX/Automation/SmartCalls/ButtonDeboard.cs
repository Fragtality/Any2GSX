using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonDeboard() : SmartButton(SmartButtonCall.Deboard)
    {
        public override bool CheckActive()
        {
            return !Profile.CallDeboardOnArrival && !GsxController.ServiceDeboard.IsRunning;
        }

        public override bool CanExecute()
        {
            return !GsxController.ServiceDeboard.IsRunning;
        }

        public override Task<bool> Run()
        {
            return GsxController.ServiceDeboard.Call();
        }
    }
}
