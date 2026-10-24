using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseFlight() : AutomationPhase(AutomationState.Flight)
    {
        public override bool NeedsFlightplan => true;

        public override Task InitStore()
        {
            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            if (!IsOnGround)
                return Phase;

            if (AircraftState.Speed < Config.SpeedTresholdTaxiIn)
            {
                if (Config.DebugArrival)
                    await Task.Delay(1500, RequestToken);

                Logger.Debug("Entered Taxi-In phase Condition");
                return AutomationState.TaxiIn;
            }
            else if (AutomationController.Tick % 20 == 0)
            {
                Logger.Debug($"IsOnGround while in Flight: Speed {AircraftState.Speed} >= Config {Config.SpeedTresholdTaxiIn}");
            }

            return Phase;
        }

        public override Task Run()
        {
            return Task.CompletedTask;
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            Flightplan.ResetArrivalPayload();
            DepartureQueue.CountActivations();
            GsxMenu.ResetFlight();
            GsxGate.SetPushDirection("");
            Store.ResetFlight();
            EquipManager.Reset();
            await GsxController.ResetServices(Config.ResetGsxStateVarsFlight);

            if (AutomationController.ExternalServiceControl)
                DepartureQueue.SetExternalControl(false);
        }
    }
}
