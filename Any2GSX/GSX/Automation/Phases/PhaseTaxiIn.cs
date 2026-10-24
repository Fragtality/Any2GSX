using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseTaxiIn() : AutomationPhase(AutomationState.TaxiIn)
    {
        public override Task InitStore()
        {
            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            bool switchArrival = false;
            if (!AircraftState.EnginesRunning && AutomationController.Tick % 20 == 0)
                Logger.Debug($"Engines shutdown while in Taxi-In - BrakeSet: {AircraftState.BrakeSet} | LightBeacon {AircraftState.LightBeacon}");

            //Default: TaxiIn -> Arrival
            if (!AircraftState.EnginesRunning && AircraftState.BrakeSet && !AircraftState.LightBeacon)
            {
                switchArrival = true;
            }
            //TaxiIn -> Arrival (Deboard)
            else if (ServiceDeboard.IsRunning)
            {
                LogReason(AutomationState.Arrival, "Deboard Service running", $"EnginesRunning {AircraftState.EnginesRunning} | BrakeSet {AircraftState.BrakeSet} | LightBeacon {AircraftState.LightBeacon}");
                switchArrival = true;
            }

            if (switchArrival)
                return AutomationState.Arrival;
            else
                return Phase;
        }

        public override Task Run()
        {
            return GsxMenu.RunGateSelection();
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            await Flightplan.ImportArrivalPayload();
            Logger.Debug($"OFP ID on Taxi-In: {Flightplan.PayloadArrival.Id}");

            if (Profile.RunAutomationService)
            {
                if (Config.RestartGsxOnTaxiIn)
                {
                    Logger.Information($"Automation: Restarting GSX on Taxi-In ({(Config.GsxUseHardReset ? "hard" : "soft")})");
                    await AppService.Instance.RestartGsx();
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }

                if (!GsxGate.HasSelectionArrivalGate && Config.OpenMenuOnTaxiIn && Config.DelayOpenTaxiInMenu > 0 && !GsxGate.HasGateSelection)
                    AutomationController.TimedMenuOpen.Queue($"Automation: Open Menu in {Config.DelayOpenTaxiInMenu}s for Gate Selection ...", TimeSpan.FromSeconds(Config.DelayOpenTaxiInMenu), () => Profile.EnableMenuForSelection || !Profile.RunAutomationService);
            }
        }
    }
}
