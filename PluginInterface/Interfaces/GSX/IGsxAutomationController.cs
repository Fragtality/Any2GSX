using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces.GSX
{
    public interface IGsxAutomationController
    {
        public AutomationState State { get; }
        public SmartButtonCall SmartButtonCurrent { get; }
        public bool ExternalServiceControl { get; }
        public bool ExternalSmartButton { get; }
        public bool IsStarted { get; }
        public bool AutomationStateValid { get; }

        public event Func<AutomationState, Task> OnStateChange;

        public void SendLoadsheet();

        public void SetExternalSmartButton();
    }
}
