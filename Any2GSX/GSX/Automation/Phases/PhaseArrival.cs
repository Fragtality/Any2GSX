using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseArrival() : AutomationPhase(AutomationState.Arrival)
    {
        public override bool NeedsDepartureQueue => true;

        public override Task InitStore()
        {
            Store.Add(StateVar.TimeNextTurnCheck, DateTime.MinValue);
            Store.Add(StateVar.RunDepartureOnArrival, false);
            Store.Add(StateVar.CockpitChocksNotified, false);

            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            if (Profile.RunDepartureOnArrival && Store[StateVar.RunDepartureOnArrival] == false && ServiceDeboard.IsRunning && AircraftState.ReadyDepartureServices)
            {
                if (!Flightplan.IsLoaded && Store[StateVar.TimeNextTurnCheck] <= DateTime.Now && await Flightplan.CheckNewOfp())
                    await Flightplan.Import();
                if (!Flightplan.IsLoaded && Store[StateVar.TimeNextTurnCheck] <= DateTime.Now)
                    Store[StateVar.TimeNextTurnCheck] = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnRecheckSeconds);

                if (Flightplan.IsLoaded)
                {
                    Logger.Information("Automation: Run Departure Services during Arrival");
                    Store[StateVar.RunDepartureOnArrival] = true;
                    DepartureQueue.BuildQueue();
                    Store[StateVar.ServiceQueueInit] = true;
                }
            }

            //Default: Arrival -> Turnaround
            if (ServiceDeboard.IsCompleted)
            {
                if (!Profile.RunDepartureOnArrival || Store[StateVar.RunDepartureOnArrival] == false)
                    return AutomationState.TurnAround;
                else
                    return AutomationState.Departure;
            }

            return Phase;
        }

        public override async Task Run()
        {
            //Equipment
            if (!EquipManager.EquipmentPlaced())
            {
                Tracker.Track(AppNotification.GateEquip);
                await EquipManager.PlaceGroundEquipment("Arrival");
                await Task.Delay(100, RequestToken);
                await AircraftState.Refresh();
            }

            //Chock Notification
            if (AircraftState.HasChocks && AircraftState.EquipmentChocks && Store[StateVar.CockpitChocksNotified] == false)
            {
                if (Profile.NotifyChocksPlaced)
                    await Aircraft.NotifyCockpit(CockpitNotification.ChocksPlaced);
                Store[StateVar.CockpitChocksNotified] = true;
            }

            //Deboard or Jetway/Stairs (and Airstair Handling)
            if (!ServiceDeboard.IsCalled && Profile.CallDeboardOnArrival)
            {
                await EquipManager.CheckSetAirstairs(true);

                Logger.Information("Automation: Call Deboard on Arrival");
                await ServiceDeboard.Call();
            }
            else if (!ServiceDeboard.IsCalled && !GsxController.IsGateConnected && Profile.CallJetwayStairsOnArrival)
            {
                await EquipManager.CheckSetAirstairs(true);

                if (ServiceJetway.IsConnectable && !AircraftState.AirstairsState)
                {
                    Logger.Information("Automation: Call Jetway on Arrival");
                    await ServiceJetway.Call();
                }

                if (ServiceStairs.IsConnectable)
                {
                    Logger.Information("Automation: Call Stairs on Arrival");
                    await ServiceStairs.Call();
                }
            }

            if (EquipManager.EquipmentPlaced())
                Tracker.Clear(AppNotification.GateEquip);
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            GsxGate.SetArrivalGate(null);
            EquipManager.SetRandomDelays();
            await Flightplan.Unload();

            if (Profile.RunAutomationService && GsxMenu.MenuCommandsAllowed)
                AutomationController.TimedMenuOpen.Queue("Automation: Refresh Menu for Arrival Services ...",
                                                         TimeSpan.FromMilliseconds(Config.StateMachineInterval),
                                                         () => Profile.EnableMenuForSelection &&
                                                         ((!Profile.CallDeboardOnArrival && !Profile.CallJetwayStairsOnArrival)
                                                         || (!Profile.OperatorAutoSelect && GsxMenu.OperatorSelectionState[GsxMenuCommandOperatorType.Handling] == false)));

            if (!ServiceDeboard.IsRunning)
                await ServiceDeboard.SetPaxTarget(Flightplan.PayloadArrival.CountPax);

            if (Profile.RunDepartureOnArrival)
            {
                Logger.Debug($"Setting inital Delay to delay DepartureOnArrival ({Profile.DelayTurnAroundSeconds}s)");
                Store[StateVar.TimeNextTurnCheck] = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnAroundSeconds);
            }
        }
    }
}
