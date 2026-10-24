using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseDeparture() : AutomationPhase(AutomationState.Departure)
    {
        public override bool NeedsFlightplan => true;
        public override bool NeedsDepartureQueue => true;
        public override bool NeedsEquipPlaced => true;

        public override Task InitStore()
        {
            Store.Add(StateVar.IsFinalReceived, false);

            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            //Default: Departure -> Pushback
            if (DepartureQueue.ServicesCompleted || Store[StateVar.IsFinalReceived] == true)
            {
                return AutomationState.Pushback;
            }
            else if (CheckBoardingCompleted())
            {
                LogReason(AutomationState.Pushback, "Aircraft already boarded", $"WeightTotalKg {await Aircraft.GetWeightTotalKg()} | FuelOnBoardKg {await Aircraft.GetFuelOnBoardKg()} | ZeroFuelRampKg {Flightplan.ZeroFuelRampKg}");

                return AutomationState.Pushback;
            }
            return Phase;
        }

        protected virtual bool CheckBoardingCompleted()
        {
            return !DepartureQueue.ServicesCompleted && AircraftState.IsBoardingCompleted && Flightplan.IsLoaded && DepartureQueue.HasAllCalled(s => !s.Service.IsRunning);
        }

        public override async Task Run()
        {
            await EquipManager.RemoveDepartureEquip();

            //Eval next Service and apply Service Skips (Activation, Constraints, State)
            await DepartureQueue.EvaluateNextService();

            if (!AutomationController.ExternalServiceControl)
            {
                //Skip Departure on BoardingCompleted
                bool skip = false;
                if (CheckBoardingCompleted() && AutomationController.State == AutomationState.Departure)
                    skip = true;

                //Departure Queue finished
                if (DepartureQueue.ServicesCompleted)
                {
                    if (!skip)
                        Logger.Information($"Automation: All Departure Services completed");
                    skip = true;
                }

                if (skip)
                    return;
            }

            //Skip when Commands active
            if (!GsxMenu.MenuCommandsAllowed)
            {
                if (Tracker.IsActive(AppNotification.UpdatesBlocked) && ServiceBoard.IsRequested && ((GsxController.HasGateJetway && Aircraft.HasRearStair()) || !GsxController.HasGateJetway) && (ServiceStairs.IsConnectable || (Store[StateVar.WasStairRemoved] == true && !ServiceStairs.IsOperating)))
                {
                    Logger.Information($"Automation: Call Stairs during Departure (Board Requested)");
                    Store[StateVar.WasStairRemoved] = false;
                    await ServiceStairs.Call();
                }
                else if (AutomationController.Tick % 20 == 0)
                    Logger.Debug("Waiting for Menu Command to be allowed");
                return;
            }

            //Apply Service Cancellations
            await DepartureQueue.ApplyCancellations();

            //Jetway / Stairs during Departure
            if ((Profile.CallJetwayStairsDuringDeparture && AutomationController.State == AutomationState.Departure)
                || (Profile.CallJetwayStairsOnArrival && AutomationController.State == AutomationState.Arrival))
            {
                await EquipManager.CheckSetAirstairs(true);

                if (ServiceJetway.IsConnectable && !AircraftState.AirstairsState && DepartureQueue.NextType != GsxServiceType.Boarding)
                {
                    Logger.Information($"Automation: Call Jetway during {AutomationController.State}");
                    await ServiceJetway.Call();
                }

                if ((ServiceStairs.IsConnectable || (Store[StateVar.WasStairRemoved] == true && !ServiceStairs.IsOperating)) && !(ServiceBoard.IsCompleted || ServiceBoard.IsCompleting)
                    && !DepartureQueue.HasQueued(svc => svc.Service.HasStairConflict)
                    && !DepartureQueue.HasCalled(svc => svc.Service.HasStairConflict && svc.Service.IsRequested))
                {
                    Logger.Information($"Automation: Call Stairs during {AutomationController.State}{(Store[StateVar.WasStairRemoved] == true ? " (re-attach removed)" : "")}");
                    Logger.Debug($"Queued Conflicts: {DepartureQueue.HasQueued(svc => svc.Service.HasStairConflict)} | Called Conflicts requested {DepartureQueue.HasCalled(svc => svc.Service.HasStairConflict && svc.Service.IsRequested)}");
                    Store[StateVar.WasStairRemoved] = false;
                    await ServiceStairs.Call();
                }
            }

            //Run Service Queue
            if (DepartureQueue.HasNext)
            {
                if (DepartureQueue.NextType == GsxServiceType.Boarding)
                    await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                await DepartureQueue.CallNext();
            }
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            if (!ServiceBoard.IsRunning)
            {
                if (Profile.RunAutomationService && Profile.RefreshGsxOnDeparture)
                    await GsxController.ReloadSimbrief();

                await ServiceBoard.SetPaxTarget(Flightplan.CountPax);
            }
        }
    }
}
