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
    public class GsxServiceDeboarding(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Deboarding;
        public virtual ISimResourceSubscription SubDeboardService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDeboardService;
        public virtual int PaxTarget => (int)SubPaxTarget.GetNumber();
        public virtual ISimResourceSubscription SubPaxTarget { get; protected set; }
        public virtual bool WasTargetSet { get; protected set; } = false;
        public virtual int PaxTotal => (int)SubPaxTotal.GetNumber();
        public virtual ISimResourceSubscription SubPaxTotal { get; protected set; }
        public virtual int CargoPercent => (int)SubCargoPercent.GetNumber();
        public virtual double CargoScalar => CargoPercent / 100.0;
        public virtual ISimResourceSubscription SubCargoPercent { get; protected set; }
        public virtual Dictionary<GsxDoor, bool> CargoUnloadingState { get; } = new()
        {
            { GsxDoor.CargoDoor1, false },
            { GsxDoor.CargoDoor2, false },
            { GsxDoor.CargoDoor3Main, false },
        };

        public event Func<GsxServiceDeboarding, Task> OnPaxChange;
        public event Func<GsxServiceDeboarding, Task> OnCargoChange;
        public event Func<GsxDoor, bool, Task> OnUnloadingChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(1, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubDeboardService = SimStore.AddVariable(GsxConstants.VarServiceDeboarding);
            SubDeboardService.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalDeboard);
            SubPaxTotal.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentDeboard);
            SubCargoPercent.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarCargoUnloading1).OnReceived += NotifyUnloadingChange;
            SimStore.AddVariable(GsxConstants.VarCargoUnloading2).OnReceived += NotifyUnloadingChange;
            SimStore.AddVariable(GsxConstants.VarCargoUnloading3).OnReceived += NotifyUnloadingChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewDeboard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsDeboard);

            ReceiverStore.Get<MsgGsxCouatlStarted>().OnMessage += OnCouatlStarted;
        }

        protected virtual async void OnCouatlStarted(MsgGsxCouatlStarted msg)
        {
            if (WasTargetSet && Controller.AutomationController.State >= AutomationState.Flight && Controller.AutomationController.State < AutomationState.TurnAround)
                await SetPaxTarget(Flightplan.CountPax);
        }

        protected override void DoReset()
        {
            foreach (var door in CargoUnloadingState)
                CargoUnloadingState[door.Key] = false;

            WasTargetSet = false;
        }

        public override void FreeResources()
        {
            SubDeboardService.OnReceived -= OnStateChange;
            SubPaxTotal.OnReceived -= NotifyPaxChange;
            SubCargoPercent.OnReceived -= NotifyCargoChange;

            try
            {
                SimStore[GsxConstants.VarCargoUnloading1].OnReceived -= NotifyUnloadingChange;
                SimStore[GsxConstants.VarCargoUnloading2].OnReceived -= NotifyUnloadingChange;
                SimStore[GsxConstants.VarCargoUnloading3].OnReceived -= NotifyUnloadingChange;
            }
            catch { }

            SimStore.Remove(GsxConstants.VarServiceDeboarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalDeboard);
            SimStore.Remove(GsxConstants.VarCargoPercentDeboard);
            SimStore.Remove(GsxConstants.VarCargoUnloading1);
            SimStore.Remove(GsxConstants.VarCargoUnloading2);
            SimStore.Remove(GsxConstants.VarCargoUnloading3);
            SimStore.Remove(GsxConstants.VarNoCrewDeboard);
            SimStore.Remove(GsxConstants.VarNoPilotsDeboard);
        }

        public virtual async Task SetPaxTarget(int num)
        {
            if (Profile.RunAutomationService && Profile.SkipCrewDeboardQuestion)
            {
                Logger.Debug($"Skip Crew Deboarding");
                await SimStore[GsxConstants.VarNoCrewDeboard].WriteValue(1);
                await SimStore[GsxConstants.VarNoPilotsDeboard].WriteValue(1);
            }

            if (!Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 1)
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
            else if (!Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 1)
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
            if (pax < 0 || pax > Flightplan.CountPax)
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

            TaskTools.RunLogged(() => OnCargoChange?.Invoke(this), Controller.Token);
            Logger.Information($"Cargo Unloading Progress {(int)cargo}%");

            if (cargo == 100 && !Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 1)
            {
                Logger.Debug($"Supressing Menu Refresh");
                Controller.Menu.SuppressMenuRefresh = true;
            }
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            base.OnStateChange(sub, data);
            if (State == GsxServiceState.Completed || State == GsxServiceState.Callable)
                Controller.Menu.SuppressMenuRefresh = false;
        }

        protected virtual void NotifyUnloadingChange(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"Received Cargo Unloading Var '{sub.Name}': {sub.GetNumber()}");
            bool state = sub.GetNumber() > 0;
            GsxDoor door;
            if (sub.Name == GsxConstants.VarCargoUnloading1)
                door = GsxDoor.CargoDoor1;
            else if (sub.Name == GsxConstants.VarCargoUnloading2)
                door = GsxDoor.CargoDoor2;
            else
                door = GsxDoor.CargoDoor3Main;

            if ((state && !CargoUnloadingState[door])
                || (!state && (SubCargoPercent.GetNumber() > 0 || State < GsxServiceState.Active)))
            {
                CargoUnloadingState[door] = state;
                if (State >= GsxServiceState.Active)
                {
                    Logger.Debug($"Notify CargoUnloadingState {state} on Door {door}");
                    TaskTools.RunLogged(() => OnUnloadingChange?.Invoke(door, state), Controller.Token);
                }
            }
        }
    }
}
