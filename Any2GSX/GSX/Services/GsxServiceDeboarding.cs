using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
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

        public event Func<GsxServiceDeboarding, Task> OnPaxChange;
        public event Func<GsxServiceDeboarding, Task> OnCargoChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuGate));

            return sequence;
        }

        public override void InitSubscriptions()
        {
            SubDeboardService = SimStore.AddVariable(GsxConstants.VarServiceDeboarding);
            SubDeboardService?.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalDeboard);
            SubPaxTotal?.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentDeboard);
            SubCargoPercent?.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewDeboard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsDeboard);

            Controller.OnCouatlStarted += OnCouatlStarted;
        }

        protected virtual Task OnCouatlStarted(IGsxController gsxController)
        {
            if (WasTargetSet && Controller.AutomationController.State >= AutomationState.Flight && Controller.AutomationController.State < AutomationState.TurnAround)
                return SetPaxTarget(Flightplan.CountPax);

            return Task.CompletedTask;
        }

        protected override void DoReset()
        {
            WasTargetSet = false;
        }

        public override void FreeResources()
        {
            SubDeboardService?.OnReceived -= OnStateChange;
            SubPaxTotal?.OnReceived -= NotifyPaxChange;
            SubCargoPercent?.OnReceived -= NotifyCargoChange;

            SimStore.Remove(GsxConstants.VarServiceDeboarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalDeboard);
            SimStore.Remove(GsxConstants.VarCargoPercentDeboard);
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

            if (!await Controller.AircraftController.GetIsCargo() && Profile?.PluginId != SettingProfile.GenericId)
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

        protected virtual Task NotifyPaxChange(ISimResourceSubscription sub, object data)
        {
            if (!IsRunning)
            {
                Logger.Debug($"Ignoring Pax Change - Service not running");
                return Task.CompletedTask;
            }

            _ = TaskTools.RunPool(() => OnPaxChange?.Invoke(this), Controller.Token);
            return Task.CompletedTask;
        }

        protected virtual Task NotifyCargoChange(ISimResourceSubscription sub, object data)
        {
            if (!IsRunning)
            {
                Logger.Debug($"Ignoring Cargo Change - Service not running");
                return Task.CompletedTask;
            }

            var cargo = sub.GetNumber();
            _ = TaskTools.RunPool(() => OnCargoChange?.Invoke(this), Controller.Token);
            Logger.Information($"Cargo Unloading Progress {(int)cargo}%");

            if (cargo >= 75 && !Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 0)
                Controller.Menu.BlockMenuUpdates(true, "Crew Question");

            return Task.CompletedTask;
        }

        protected override void RunStateRequested()
        {
            base.RunStateRequested();
            WasActive = true;
        }

        protected override async Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            await base.OnStateChange(sub, data);
            if (State < GsxServiceState.Requested)
                Controller.Menu.BlockMenuUpdates(false);
            else if (ReadState() >= GsxServiceState.Completed)
            {
                Controller.Menu.BlockMenuUpdates(true, "Crew Question");
                _ = TaskTools.RunDelayed(() => Controller.Menu.BlockMenuUpdates(false), Controller.Config.OperatorSelectTimeout, Controller.Token);
            }
        }

        protected override void NotifyStateChange()
        {
            base.NotifyStateChange();

            if (State < GsxServiceState.Requested)
                Controller.Menu.BlockMenuUpdates(false);
        }
    }
}
