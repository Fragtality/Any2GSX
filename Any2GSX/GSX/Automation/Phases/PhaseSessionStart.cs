using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseSessionStart() : AutomationPhase(AutomationState.SessionStart)
    {
        public override Task InitStore()
        {
            Store.Add(StateVar.ServiceQueueInit, false);

            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            //SessionStart -> Pushback
            if (AutomationStateValid && GsxMenu.IsGateMenu && AircraftState.ReadyDepartureServices && AircraftState.AvionicsPowered && AircraftState.IsBoardingCompleted)
            {
                LogReason(AutomationState.Pushback, "Plane already boarded", $"ReadyForDepartureServices {AircraftState.ReadyDepartureServices} | IsBoardingCompleted {AircraftState.IsBoardingCompleted} | AvionicsPowered {AircraftState.AvionicsPowered} | LightNav {AircraftState.LightNav}");

                return AutomationState.Pushback;
            }
            //SessionStart -> Departure
            else if (AutomationStateValid && GsxMenu.IsGateMenu && (DepartureServicesRunning || AircraftState.ReadyDepartureServices))
            {
                string vars = $"ReadyForDepartureServices {AircraftState.ReadyDepartureServices}  | AvionicsPowered {AircraftState.AvionicsPowered} | LightNav {AircraftState.LightNav}";
                if (DepartureServicesRunning)
                    LogReason(AutomationState.Departure, "Departure Services running", vars);
                else
                    LogReason(AutomationState.Departure, "Aircraft ready for Departure", vars);

                return AutomationState.Departure;
            }
            //Default: SessionStart -> Preparation
            else if (GsxMenu.IsGateMenu)
            {
                return AutomationState.Preparation;
            }

            return Phase;
        }

        public override Task Run()
        {
            return Task.CompletedTask;
        }
    }
}
