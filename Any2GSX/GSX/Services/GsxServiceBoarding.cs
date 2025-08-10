using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceBoarding(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Boarding;
        public virtual ISimResourceSubscription SubBoardService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubBoardService;
        public virtual int PaxTarget => (int)SubPaxTarget.GetNumber();
        public virtual ISimResourceSubscription SubPaxTarget { get; protected set; }
        public virtual bool WasTargetSet { get; protected set; } = false;
        public virtual int PaxTotal => (int)SubPaxTotal.GetNumber();
        public virtual ISimResourceSubscription SubPaxTotal { get; protected set; }
        public virtual int CargoPercent => (int)SubCargoPercent.GetNumber();
        public virtual ISimResourceSubscription SubCargoPercent { get; protected set; }
        public virtual double CargoScalar => CargoPercent / 100.0;
        public virtual Dictionary<GsxDoor, bool> CargoLoadingState { get; } = new()
        {
            { GsxDoor.CargoDoor1, false },
            { GsxDoor.CargoDoor2, false },
            { GsxDoor.CargoDoor3Main, false },
        };

        public event Func<GsxServiceBoarding, Task> OnPaxChange;
        public event Func<GsxServiceBoarding, Task> OnCargoChange;
        public event Func<GsxDoor, bool, Task> OnLoadingChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(4, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubBoardService = SimStore.AddVariable(GsxConstants.VarServiceBoarding);
            SubBoardService.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalBoard);
            SubPaxTotal.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentBoard);
            SubCargoPercent.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarCargoLoading1).OnReceived += NotifyLoadingChange;
            SimStore.AddVariable(GsxConstants.VarCargoLoading2).OnReceived += NotifyLoadingChange;
            SimStore.AddVariable(GsxConstants.VarCargoLoading3).OnReceived += NotifyLoadingChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewBoard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsBoard);

            ReceiverStore.Get<MsgGsxCouatlStarted>().OnMessage += OnCouatlStarted;
        }

        protected virtual async void OnCouatlStarted(MsgGsxCouatlStarted msg)
        {
            if (WasTargetSet && Controller.AutomationController.State > AutomationState.SessionStart && Controller.AutomationController.State < AutomationState.Flight)
                await SetPaxTarget(Flightplan.CountPax);
        }

        protected override void DoReset()
        {
            foreach (var door in CargoLoadingState)
                CargoLoadingState[door.Key] = false;

            WasTargetSet = false;
        }

        public override void FreeResources()
        {
            SubBoardService.OnReceived -= OnStateChange;
            SubPaxTotal.OnReceived -= NotifyPaxChange;
            SubCargoPercent.OnReceived -= NotifyCargoChange;

            try
            {
                SimStore[GsxConstants.VarCargoLoading1].OnReceived -= NotifyLoadingChange;
                SimStore[GsxConstants.VarCargoLoading2].OnReceived -= NotifyLoadingChange;
                SimStore[GsxConstants.VarCargoLoading3].OnReceived -= NotifyLoadingChange;
            }
            catch { }

            SimStore.Remove(GsxConstants.VarServiceBoarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalBoard);
            SimStore.Remove(GsxConstants.VarCargoPercentBoard);
            SimStore.Remove(GsxConstants.VarCargoLoading1);
            SimStore.Remove(GsxConstants.VarCargoLoading2);
            SimStore.Remove(GsxConstants.VarCargoLoading3);
            SimStore.Remove(GsxConstants.VarNoCrewBoard);
            SimStore.Remove(GsxConstants.VarNoPilotsBoard);
        }

        public virtual async Task SetPaxTarget(int num)
        {
            if (Profile.RunAutomationService && Profile.SkipCrewBoardQuestion)
            {
                Logger.Debug($"Skip Crew Boarding");
                await SimStore[GsxConstants.VarNoCrewBoard].WriteValue(1);
                await SimStore[GsxConstants.VarNoPilotsBoard].WriteValue(1);
            }

            if (!Profile.SkipCrewBoardQuestion && Profile.AnswerCrewBoardQuestion != 1)
            {
                Logger.Debug($"Resetting Crew Skip Variables");
                await SimStore[GsxConstants.VarNoCrewBoard].WriteValue(0);
                await SimStore[GsxConstants.VarNoPilotsBoard].WriteValue(0);
                Logger.Debug($"Setting Pilot Target to {Controller.Profile.DefaultPilotTarget}");
                await Controller?.SubPilotTarget?.WriteValue(Controller.Profile.DefaultPilotTarget);
                Logger.Debug($"Setting Crew Target to {Controller.Profile.DefaultCrewTarget}");
                await Controller?.SubCrewTarget?.WriteValue(Controller.Profile.DefaultCrewTarget);
            }

            if (Controller?.AircraftController?.Aircraft?.IsCargo == false && Profile?.PluginId != SettingProfile.GenericId)
            {
                WasTargetSet = true;
                Logger.Debug($"Setting Pax Target to {num}");
                await SubPaxTarget.WriteValue(num);
            }
            else if (!Profile.SkipCrewBoardQuestion && Profile.AnswerCrewBoardQuestion != 1)
            {
                WasTargetSet = true;
                Logger.Debug($"Setting PaxTarget as Crew Target to {num} (Cargo Aircraft)");
                await Controller?.SubCrewTarget?.WriteValue(num);
            }
        }

        protected virtual void NotifyPaxChange(ISimResourceSubscription sub, object data)
        {
            if (State != GsxServiceState.Active)
            {
                Logger.Debug($"Ignoring Pax Change - Service not active");
                return;
            }

            var pax = sub.GetNumber();
            var target = Flightplan.CountPax;
            if (target <= 0 && PaxTarget > 0)
                target = PaxTarget;
            if (pax < 0 || pax > target)
            {
                if (Profile.RunAutomationService)
                    Logger.Warning($"Ignoring Pax Change - Value received: {pax} (Targets: {PaxTarget} | {Flightplan.CountPax})");
                return;
            }

            TaskTools.RunLogged(() => OnPaxChange?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyCargoChange(ISimResourceSubscription sub, object data)
        {
            if (State != GsxServiceState.Active)
            {
                Logger.Debug($"Ignoring Cargo Change - Service not active");
                return;
            }

            var cargo = sub.GetNumber();
            if (cargo < 0 || cargo > 100)
            {
                if (Profile.RunAutomationService)
                    Logger.Warning($"Ignoring Cargo Change - Value received: {cargo}");
                return;
            }
            
            if (Controller.Menu.SuppressMenuRefresh && cargo > 0)
                Controller.Menu.SuppressMenuRefresh = false;

            TaskTools.RunLogged(() => OnCargoChange?.Invoke(this), Controller.Token);
            Logger.Information($"Cargo Loading Progress {(int)cargo}%");
        }

        protected virtual void NotifyLoadingChange(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"Received Cargo Loading Var '{sub.Name}': {sub.GetNumber()}");
            bool state = sub.GetNumber() > 0;
            GsxDoor door;
            if (sub.Name == GsxConstants.VarCargoLoading1)
                door = GsxDoor.CargoDoor1;
            else if (sub.Name == GsxConstants.VarCargoLoading2)
                door = GsxDoor.CargoDoor2;
            else
                door = GsxDoor.CargoDoor3Main;

            if ((state && !CargoLoadingState[door])
                || (!state && (SubCargoPercent.GetNumber() > 0 || State < GsxServiceState.Active)))
            {
                CargoLoadingState[door] = state;
                if (State >= GsxServiceState.Active)
                {
                    Logger.Debug($"Notify CargoLoadingState {state} on Door {door}");
                    TaskTools.RunLogged(() => OnLoadingChange?.Invoke(door, state), Controller.Token);
                }
            }
        }

        protected override void NotifyStateChange()
        {
            base.NotifyStateChange();

            if (State == GsxServiceState.Active || State == GsxServiceState.Requested)
            {
                Logger.Debug($"Supressing Menu Refresh");
                Controller.Menu.SuppressMenuRefresh = true;
            }
            else if (State == GsxServiceState.Callable || State == GsxServiceState.Completed)
                Controller.Menu.SuppressMenuRefresh = false;
        }
    }
}
