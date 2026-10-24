using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhasePushback() : AutomationPhase(AutomationState.Pushback)
    {
        public override bool NeedsFlightplan => true;

        public override Task InitStore()
        {
            Store.Add(StateVar.JetwayStairRemoved, false);

            return Task.CompletedTask;
        }

        public override Task<AutomationState> Evaluate()
        {
            //Default: Pushback -> TaxiOut
            if (AutomationStateValid && (ServicePushBack.IsCompleted || ((ServicePushBack.PushStatus == 0 || ServicePushBack.PushStatus >= 11) && AircraftState.Speed >= Config.SpeedTresholdTaxiOut)))
                return Task.FromResult(AutomationState.TaxiOut);

            return Task.FromResult(Phase);
        }

        public override async Task Run()
        {
            //PCA Handling
            if (AircraftState.HasPca && AircraftState.ApuRunning && AircraftState.ApuBleed && AircraftState.EquipmentPca)
            {
                Logger.Information($"Automation: Disconnecting PCA on APU Bleed on");
                await Aircraft.SetEquipmentPca(false, ServicePushBack.PushStatus >= 3 || ServicePushBack.IsActive);
            }

            //Gradual Equipment
            if (Profile.GradualGroundEquipRemoval && !EquipManager.EquipmentRemoved())
            {
                await EquipManager.RemoveGroundEquip("Pushback Phase (Gradual)");
            }

            //Beacon <> Equip Handling
            if (!AircraftState.PowerConnected && AircraftState.BrakeSet && AircraftState.LightBeacon && !AircraftState.EnginesRunning)
            {
                if (Profile.ClearGroundEquipOnBeacon)
                    await EquipManager.RemoveGroundEquip("Beacon");

                if (Profile.CallPushbackOnBeacon && !ServicePushBack.IsCalled && !ServicePushBack.IsRunning && !AutomationController.ExternalServiceControl)
                {
                    Logger.Information($"Automation: Call Pushback (Beacon / Prepared for Push)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }
                else if ((GsxController.IsGateConnected || AircraftState.AirstairsState) && Profile.ClearGroundEquipOnBeacon)
                {
                    Store[StateVar.JetwayStairRemoved] = true;

                    await EquipManager.CheckSetAirstairs(false);

                    if (GsxController.IsGateConnected)
                    {
                        Logger.Information($"Automation: Remove Jetway/Stairs on Beacon");
                        if (!AircraftState.AirstairsState)
                            await ServiceJetway.Remove();
                        await ServiceStairs.Remove();
                        await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                    }
                }
            }

            //Pushback Call handling
            if (((ServicePushBack.TugAttachedOnBoarding && Profile.CallPushbackWhenTugAttached > 0) || Profile.CallPushbackOnFinal) && !ServicePushBack.IsCalled && ServicePushBack.IsRunning && !AutomationController.ExternalServiceControl)
            {
                string reason = "";
                if (Profile.CallPushbackOnFinal && Store[StateVar.IsFinalReceived] == true)
                    reason = "after Final LS received";
                else if (Profile.CallPushbackWhenTugAttached == 1)
                    reason = "after Departure Services (Tug already attached)";
                else if (Profile.CallPushbackWhenTugAttached == 2 && Store[StateVar.IsFinalReceived] == true)
                    reason = "after Final LS (Tug already attached)";

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    Logger.Information($"Automation: Call Pushback {reason}");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
            }

            //Expedited Equip removal & Door Closure
            if (ServicePushBack.IsRunning || ServiceDeice.IsRunning || AircraftState.EnginesRunning)
            {
                string reason = "Pushback Running";
                if (ServiceDeice.IsRunning)
                    reason = "De-Ice";
                else if (AircraftState.EnginesRunning)
                    reason = "Engines Running";

                if (await Aircraft.GetHasOpenDoors())
                {
                    Logger.Information($"Automation: Close Doors on {reason}");
                    await Aircraft.DoorsAllClose();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                    if (!AircraftController.PluginController.GetPluginCapabilities(Profile.PluginId).HasDoorSync)
                        await GsxController.ApiClient.GetState();
                }

                if (!EquipManager.EquipmentRemoved())
                    await EquipManager.RemoveGroundEquip(reason, AircraftState.EnginesRunning);
            }
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            if (Profile.RunAutomationService && (ServiceStairs.IsConnected || AircraftState.AirstairsState) && ((ServiceJetway.IsAvailable && Profile.RemoveStairsAfterDepature >= 1) || (!ServiceJetway.IsAvailable && Profile.RemoveStairsAfterDepature == 1)))
            {
                await EquipManager.CheckSetAirstairs(false);

                if (ServiceStairs.IsConnected)
                {
                    Logger.Information($"Automation: Remove Stairs after Departure Services");
                    await ServiceStairs.Remove();
                }
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }

            if (Profile.RunAutomationService && Profile.CancelServicesOnPushPhase && DepartureQueue.HasCalled(s => s.Service.IsRunning))
            {
                await DepartureQueue.CancelRunningServices("Pushback Phase");
            }
            else if (DepartureQueue.HasServiceQueued)
            {
                Logger.Information($"Skip queued Services due to Pushback Phase");
                DepartureQueue.FinishServices();
            }

            if (Store[StateVar.IsFinalReceived] == false)
                SendLoadsheet();
        }

        public virtual void SendLoadsheet()
        {
            _ = TaskTools.RunPool(() => FinalLoadsheet(), RequestToken);
        }

        public virtual async Task FinalLoadsheet()
        {
            try
            {
                if (Store[StateVar.IsFinalReceived] == true)
                    return;

                if (!AutomationController.ExternalServiceControl)
                {
                    int finalDelay = new Random().Next(Profile.FinalDelayMin, Profile.FinalDelayMax);
                    Logger.Information($"Waiting for Final Loadsheet (ETA {finalDelay}s) ...");
                    Tracker.TrackTimeout(AppNotification.SheetFinal, finalDelay * 1000);
                    while (finalDelay > 0)
                    {
                        await Task.Delay(1000, RequestToken);
                        finalDelay--;
                    }

                    if (AutomationController.State >= AutomationState.Flight)
                    {
                        Logger.Debug($"Final cancelled due to State {AutomationController.State}");
                        Tracker.Clear(AppNotification.SheetFinal);
                        Store[StateVar.IsFinalReceived] = false;
                        return;
                    }
                }

                Logger.Debug("Generate Final LS ...");
                await Aircraft.GenerateLoadsheetFinal();
                Logger.Information($"Final Loadsheet received!");
                Tracker.Clear(AppNotification.SheetFinal);

                if (Profile.RunAutomationService && Store[StateVar.JetwayStairRemoved] == false && (GsxController.IsGateConnected || AircraftState.AirstairsState) && Profile.RemoveJetwayStairsOnFinal)
                {
                    Store[StateVar.JetwayStairRemoved] = true;

                    await EquipManager.CheckSetAirstairs(false);

                    if (GsxController.IsGateConnected)
                    {
                        Logger.Information($"Automation: Remove Jetway/Stairs on Final Loadsheet");
                        if (!AircraftState.AirstairsState)
                            await GsxController.ServiceJetway.Remove();
                        await GsxController.ServiceStairs.Remove();
                        await Task.Delay(Config.StateMachineInterval * 6, RequestToken);
                    }
                }

                if (Profile.RunAutomationService && Profile.CloseDoorsOnFinal && await Aircraft.GetHasOpenDoors())
                {
                    Logger.Information($"Automation: Close Doors on Final Loadsheet");
                    await AircraftController.Aircraft.DoorsAllClose();
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                    if (!AircraftController.PluginController.GetPluginCapabilities(Profile.PluginId).HasDoorSync)
                        await GsxController.ApiClient.GetState();
                }

                if (Profile.RunAutomationService && Profile.NotifyFinalReceived)
                    _ = Aircraft.NotifyCockpit(CockpitNotification.FinalReceived);

                Store[StateVar.IsFinalReceived] = true;
                Logger.Debug($"Final LS handled");
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }
    }
}
