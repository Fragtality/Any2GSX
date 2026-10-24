using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonNone() : SmartButton(SmartButtonCall.None)
    {
        public override bool CheckActive()
        {
            return false;
        }

        public override bool CanExecute()
        {
            return false;
        }

        public override Task<bool> Run()
        {
            return Task.FromResult(true);
        }
    }
}
