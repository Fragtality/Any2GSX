using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxAutomationController
    {
        public bool IsGateConnected { get; }
        public bool HasDepartBypassed { get; }
        public bool HasGateJetway { get; }
        public bool HasGateStair { get; }
        public bool ServicesValid { get; }
        public bool ExecutedReposition { get; }
        public bool DepartureServicesCompleted { get; }
        public bool JetwayStairRemoved { get; }
        public bool IsFinalReceived { get; }
        public int FinalDelay { get; }
        public DateTime TimeNextTurnCheck { get; }
        public PayloadReport PayloadArrival { get; }
        public long OfpArrivalId { get; }
        public bool RunDepartureOnArrival { get; }

        public AutomationState State { get; }

        public event Func<AutomationState, Task> OnStateChange;
    }
}
