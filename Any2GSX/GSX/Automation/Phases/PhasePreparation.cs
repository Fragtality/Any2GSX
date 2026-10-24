using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using Any2GSX.Tools;
using CFIT.AppLogger;
using CFIT.AppTools;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhasePreparation() : AutomationPhase(AutomationState.Preparation)
    {

        public override Task InitStore()
        {
            Store.Add(StateVar.ExecutedReposition, false, false);
            Store.Add(StateVar.InitialFuelPayloadSet, false, false, false);
            Store.Add(StateVar.InitialFuelPayloadQueued, false, false, false);
            Store.Add(StateVar.CockpitPrepNotified, false);
            Store.Add(StateVar.WalkaroundBeforeNotified, false, false);
            Store.Add(StateVar.WalkaroundAfterNotified, false, false);

            return Task.CompletedTask;
        }

        public override async Task<AutomationState> Evaluate()
        {
            //Preparation -> Pushback
            if (AutomationStateValid && GsxMenu.IsGateMenu && AircraftState.ReadyDepartureServices && AircraftState.AvionicsPowered && AircraftState.IsBoardingCompleted)
            {
                LogReason(AutomationState.Pushback, "Plane already boarded", $"ReadyForDepartureServices {AircraftState.ReadyDepartureServices} | IsBoardingCompleted {AircraftState.IsBoardingCompleted} | AvionicsPowered {AircraftState.AvionicsPowered} | LightNav {AircraftState.LightNav}");

                return AutomationState.Pushback;
            }
            //Default: Preparation -> Departure (triggered by ReadyDeparture)
            else if (AutomationStateValid && (!Profile.RunAutomationService || Store[StateVar.ExecutedReposition] == true) && AircraftState.ReadyDepartureServices && EquipManager.EquipmentPlaced())
            {
                return AutomationState.Departure;
            }
            //Preparation -> Departure (Services)
            else if (AutomationStateValid && GsxMenu.IsGateMenu && (DepartureServicesRunning || AircraftState.ReadyDepartureServices))
            {
                string vars = $"ReadyForDepartureServices {AircraftState.ReadyDepartureServices}  | AvionicsPowered {AircraftState.AvionicsPowered} | LightNav {AircraftState.LightNav}";
                if (DepartureServicesRunning)
                    LogReason(AutomationState.Departure, "Departure Services running", vars);
                else
                    LogReason(AutomationState.Departure, "Aircraft ready for Departure", vars);

                return AutomationState.Departure;
            }

            return Phase;
        }

        protected virtual bool CheckReposition()
        {
            return ServiceReposition.IsCompleted || GsxController.IsGateConnected || !Profile.CallReposition || AircraftState.ReadyDepartureServices || ServiceReposition.IsCalled || ServiceReposition.WasRepoReceived || GsxController.ApiClient.GetSetting(GsxConstants.SettingAutoReposition) != 0;
        }

        public override async Task Run()
        {
            if (Tracker.IsActive(AppNotification.GateSelect) || Tracker.IsActive(AppNotification.GateMove) || ServicePushBack.IsRunning)
                return;

            if (Store[StateVar.ExecutedReposition] == false)
                Store[StateVar.ExecutedReposition] = CheckReposition();

            //Reposition
            if (Store[StateVar.ExecutedReposition] == false && Profile.CallReposition && (!IsWalkaround || (IsWalkaround && Profile.CallJetwayStairsInWalkaround)))
            {
                Logger.Information("Automation: Reposition Aircraft on Gate");
                await ServiceReposition.Call();
                await Task.Delay(Config.StateMachineInterval * 3, RequestToken);
                Store[StateVar.ExecutedReposition] = CheckReposition();
                return;
            }
            if (Store[StateVar.ExecutedReposition] == false && !IsWalkaround)
                return;

            //Initial FOB/Payload
            if (AircraftController.IsAircraftConnected && !GsxController.IsWalkaround && GsxController.IsSessionReady && Store[StateVar.InitialFuelPayloadSet] == false && Store[StateVar.InitialFuelPayloadQueued] == false)
            {
                Store[StateVar.InitialFuelPayloadQueued] = true;
                _ = TaskTools.RunDelayed(async () =>
                {
                    await AircraftController.SetInitialFuelPayload();
                    Store[StateVar.InitialFuelPayloadSet] = true;
                }, 2000, RequestToken);
            }

            bool walkaroundSkipped = Store[StateVar.WalkaroundAfterNotified];
            //Jetway/Stairs
            //True Walkaround with Custom Stairs avail & preferred
            if (GsxController.IsWalkaround && !walkaroundSkipped && Profile.CallJetwayStairsInWalkaround && AircraftController.IsAircraftConnected && AircraftState.HasCustomStairs && Profile.PreferCustomStairsInWalkaround)
            {
                if (!AircraftState.CustomStairsFront)
                {
                    Logger.Information($"Automation: Call Aircraft Custom Front Stairs in Walkaround");
                    await Aircraft.SetCustomStairsFront(true);
                }

                if (Aircraft.HasRearStair() && !AircraftState.CustomStairsRear)
                {
                    Logger.Information($"Automation: Call Aircraft Custom Rear Stairs in Walkaround");
                    await Aircraft.SetCustomStairsRear(true);
                }
            }
            //Walkaround/Ready with Jetway/Stairs allowed (use Custom Stair in true Walkaround instead of Jetway)
            else if (IsWalkaround && !walkaroundSkipped && Profile.CallJetwayStairsInWalkaround)
            {
                if (ServiceJetway.IsConnectable && (!AircraftState.HasAirStairs || (AircraftState.HasAirStairs && !Profile.DoorPreferAirstair)))
                {
                    if (!GsxController.IsWalkaround)
                    {
                        Logger.Information($"Automation: Call Jetway in Ready-to-Fly");
                        await ServiceJetway.Call();
                    }
                    else if (AircraftController.IsAircraftConnected && AircraftState.HasCustomStairs && !AircraftState.CustomStairsFront)
                    {
                        Logger.Information($"Automation: Call Aircraft Custom Front Stairs in Walkaround (Jetway not available yet)");
                        await Aircraft.SetCustomStairsFront(true);
                    }
                }
                else if (AircraftState.HasAirStairs && Profile.DoorPreferAirstair)
                {
                    await EquipManager.CheckSetAirstairs(true);
                }

                if (ServiceStairs.IsConnectable)
                {
                    Logger.Information($"Automation: Call GSX Stairs in {(GsxController.IsWalkaround ? "Walkaround" : "Ready-to-Fly")}");
                    await ServiceStairs.Call();
                }
            }
            //In Cockpit with Jetway/Stairs on Preparation
            else if (!IsWalkaround && Profile.CallJetwayStairsOnPrep)
            {
                await EquipManager.CheckSetAirstairs(true);

                if (ServiceJetway.IsConnectable && !AircraftState.AirstairsState)
                {
                    Logger.Information($"Automation: Call Jetway on Preparation");
                    await ServiceJetway.Call();
                }

                if (ServiceStairs.IsConnectable)
                {
                    Logger.Information($"Automation: Call Stairs on Preparation");
                    await ServiceStairs.Call();
                }
            }

            //Walkaround Handling
            if (IsWalkaround)
            {
                if (Store[StateVar.WalkaroundBeforeNotified] == false)
                {
                    if (AircraftController.IsAircraftConnected)
                    {
                        await Aircraft.BeforeWalkaroundSkip();
                        await Task.Delay(Config.StateMachineInterval, RequestToken);
                    }
                    Store[StateVar.WalkaroundBeforeNotified] = true;
                }

                if (Profile.SkipWalkAround && Profile.RunAutomationService && GsxController.IsWalkaround && !walkaroundSkipped)
                {
                    Logger.Information("Automation: Skip Walkaround");
                    await ToggleWalkaround();
                    if (GsxController.IsWalkaround)
                        await Task.Delay(Config.TimerGsxCheck, RequestToken);
                }
            }
            if (Store[StateVar.WalkaroundAfterNotified] == false && !IsWalkaround)
            {
                if (AircraftController.IsAircraftConnected)
                    await Aircraft.AfterWalkaroundSkip();
                Store[StateVar.WalkaroundAfterNotified] = true;
            }

            //Equipment
            if (AircraftController.IsAircraftConnected)
            {
                if (((IsWalkaround && Profile.PlaceGroundEquipWalkaround) || !IsWalkaround) && !EquipManager.EquipmentPlaced())
                {
                    Tracker.Track(AppNotification.GateEquip);
                    await EquipManager.PlaceGroundEquipment("Preparation");
                    await Task.Delay(100, RequestToken);
                    await AircraftState.Refresh();
                }
            }
            if (IsWalkaround && Profile.PlaceGroundEquipWalkaround && !walkaroundSkipped)
            {
                await Aircraft.HandleWalkaroundEquipment();
                await Task.Delay(100, RequestToken);
                await AircraftState.Refresh();
            }

            //Cockpit Notification
            if (AutomationStateValid && Store[StateVar.CockpitPrepNotified] == false && Store[StateVar.ExecutedReposition] == true && Store[StateVar.InitialFuelPayloadSet] == true && EquipManager.EquipmentPlaced())
            {
                Tracker.Clear(AppNotification.GateEquip);
                if (Profile.NotifyPrepFinished)
                    await Aircraft.NotifyCockpit(CockpitNotification.PrepFinished);
                Store[StateVar.CockpitPrepNotified] = true;
            }
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            GsxController.Menu.BlockMenuUpdates(false);
        }

        protected virtual async Task ToggleWalkaround()
        {
            Logger.Debug("Toggling Walkaround ...");
            string title = WindowTools.GetMsfsWindowTitle();
            Sys.SetForegroundWindow(title);
            await Task.Delay(Config.DelayForegroundChange, RequestToken);
            string active = Sys.GetActiveWindowTitle();
            if (active == title)
            {
                Logger.Debug($"Sending Keystrokes");
                WindowTools.SendWalkaroundKeystroke();
                await Task.Delay(Config.DelayAircraftModeChange, RequestToken);
            }
            else
                Logger.Debug($"Active Window did not match to '{title}'");
        }
    }
}
