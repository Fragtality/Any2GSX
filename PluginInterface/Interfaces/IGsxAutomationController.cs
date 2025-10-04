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
        public bool GroundEquipmentPlaced { get; }
        public bool JetwayStairRemoved { get; }
        public bool IsFinalReceived { get; }
        public int FinalDelay { get; }
        public int ChockDelay { get; }
        public DateTime TimeNextTurnCheck { get; }
        public bool InitialTurnDelay { get; }
        public int OfpArrivalId { get; }
        public bool RunDepartureOnArrival { get; }

        public AutomationState State { get; }

        public event Func<AutomationState, Task> OnStateChange;
        public event Func<Task> OnFinalReceived;
    }
}
