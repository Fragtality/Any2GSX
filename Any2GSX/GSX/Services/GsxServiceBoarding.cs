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

        public event Func<GsxServiceBoarding, Task> OnPaxChange;
        public event Func<GsxServiceBoarding, Task> OnCargoChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(4, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && ((!Profile.SkipCrewBoardQuestion && Profile.AnswerCrewBoardQuestion == 0) || Profile.AttachTugDuringBoarding == 0);
            sequence.ResetMenuCheck = () => false;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(4, GsxConstants.MenuGate));

            return sequence;
        }

        public override void InitSubscriptions()
        {
            SubBoardService = SimStore.AddVariable(GsxConstants.VarServiceBoarding);
            SubBoardService?.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalBoard);
            SubPaxTotal?.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentBoard);
            SubCargoPercent?.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewBoard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsBoard);

            Controller.OnCouatlStarted += OnCouatlStarted;
        }

        protected virtual Task OnCouatlStarted(IGsxController gsxController)
        {
            if (WasTargetSet && Controller.AutomationController.State > AutomationState.SessionStart && Controller.AutomationController.State < AutomationState.Flight)
                return SetPaxTarget(Flightplan.CountPax);

            return Task.CompletedTask;
        }

        protected override Task DoReset()
        {
            WasTargetSet = false;

            return Task.CompletedTask;
        }

        public override void FreeResources()
        {
            SubBoardService?.OnReceived -= OnStateChange;
            SubPaxTotal?.OnReceived -= NotifyPaxChange;
            SubCargoPercent?.OnReceived -= NotifyCargoChange;

            SimStore.Remove(GsxConstants.VarServiceBoarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalBoard);
            SimStore.Remove(GsxConstants.VarCargoPercentBoard);
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

            if (!await Controller.AircraftController.GetIsCargo() && Profile?.PluginId != SettingProfile.GenericId)
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
            if (cargo > 0)
                Controller.Menu.BlockMenuUpdates(false);

            _ = TaskTools.RunPool(() => OnCargoChange?.Invoke(this), Controller.Token);
            Logger.Information($"Cargo Loading Progress {(int)cargo}%");
            return Task.CompletedTask;
        }

        protected override void NotifyStateChange()
        {
            base.NotifyStateChange();

            if (State == GsxServiceState.Requested)
                Controller.Menu.BlockMenuUpdates(true, "Tug Question");
            else if (State < GsxServiceState.Requested || State >= GsxServiceState.Completed)
                Controller.Menu.BlockMenuUpdates(false);
        }
    }
}
