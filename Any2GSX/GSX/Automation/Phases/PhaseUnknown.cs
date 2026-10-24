using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseUnknown() : AutomationPhase(AutomationState.Unknown)
    {
        public override Task InitStore()
        {
            return Task.CompletedTask;
        }

        public override Task<AutomationState> Evaluate()
        {
            return Task.FromResult(AutomationState.SessionStart);
        }

        public override Task Run()
        {
            return Task.CompletedTask;
        }
    }
}
