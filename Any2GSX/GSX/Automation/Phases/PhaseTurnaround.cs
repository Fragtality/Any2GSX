using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseTurnaround() : AutomationPhase(AutomationState.TurnAround)
    {
        public override Task InitStore()
        {
            Store.Add(StateVar.SkippedToTurn, false);

            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            AutomationState newPhase = AutomationState.Unknown;
            //Default: Turnaround -> Arrival
            if (AircraftState.ReadyDepartureServices && Flightplan.IsLoaded)
            {
                newPhase = AutomationState.Departure;
            }
            else if (DepartureServicesRunning)
            {
                newPhase = AutomationState.Departure;
                LogReason(newPhase, "Departure Services running");
            }
            else if (ServicePushBack.IsRunning)
            {
                newPhase = AutomationState.Pushback;
                LogReason(newPhase, "Pushback Service running");
            }

            if (newPhase != AutomationState.Unknown)
                return newPhase;
            else
                return Phase;
        }

        public override async Task Run()
        {
            if (AircraftState.ReadyDepartureServices && !Flightplan.IsLoaded && (Store[StateVar.SkippedToTurn] || (Store[StateVar.TimeNextTurnCheck] <= DateTime.Now && await Flightplan.CheckNewOfp())))
                await Flightplan.Import();

            if (!Flightplan.IsLoaded && Store[StateVar.TimeNextTurnCheck] <= DateTime.Now)
                SetTimeout();
        }

        protected virtual void SetTimeout()
        {
            Store[StateVar.TimeNextTurnCheck] = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnRecheckSeconds);
            Tracker.TrackTimeout(AppNotification.OfpCheck, Profile.DelayTurnRecheckSeconds * 1000);
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            SetTimeout();
            GsxController.Menu.BlockMenuUpdates(false);

            if (Profile.RunAutomationService && Profile.NotifyTurnReady)
                await Aircraft.NotifyCockpit(CockpitNotification.TurnReady);
        }
    }
}
