using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceBoarding() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Boarding;
        protected override int MenuSelectionService => 4;
        protected override string StateVarName => GsxConstants.VarServiceBoarding;
        protected override List<string> ServiceVariables => [GsxConstants.VarPaxTarget, GsxConstants.VarPaxTotalBoard, GsxConstants.VarCargoPercentBoard, GsxConstants.VarNoCrewBoard, GsxConstants.VarNoPilotsBoard];
        public virtual int PaxTarget => (int)(SubPaxTarget?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubPaxTarget { get; set; }
        public virtual bool WasTargetSet { get; protected set; } = false;
        public virtual int PaxTotal => (int)(SubPaxTotal?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubPaxTotal { get; set; }
        public virtual int CargoPercent => (int)(SubCargoPercent?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubCargoPercent { get; set; }
        public virtual double CargoScalar => CargoPercent / 100.0;
        public override bool RequiresStair => !Controller.HasGateJetway || (Controller.HasGateStair && Controller.Aircraft.HasRearStair());

        public event Func<GsxServiceBoarding, Task> OnPaxChange;
        public event Func<GsxServiceBoarding, Task> OnCargoChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = base.InitCallSequence();
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection &&
                                             ((Controller.ApiClient.GetSetting(GsxConstants.SettingIgnoreCrew) == 0 && !Profile.SkipCrewBoardQuestion && Profile.AnswerCrewBoardQuestion == 0)
                                             || (Profile.AttachTugDuringBoarding == 0 && Controller.ApiClient.GetSetting(GsxConstants.SettingPushOnBoarding) == 0)
                                             || (!Profile.OperatorAutoSelect && !Controller.Menu.OperatorSelectionState[OperatorType]));
            sequence.ResetMenuCheck = () => false;

            return sequence;
        }

        protected override void DoInit()
        {
            SubPaxTarget = SimStore[GsxConstants.VarPaxTarget];
            SubPaxTotal = SimStore[GsxConstants.VarPaxTotalBoard];
            SubPaxTotal?.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore[GsxConstants.VarCargoPercentBoard];
            SubCargoPercent?.OnReceived += NotifyCargoChange;
        }

        protected override Task OnCouatlStarted(IGsxController gsxController)
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

        protected override void DoRelease()
        {
            SubPaxTotal?.OnReceived -= NotifyPaxChange;
            SubCargoPercent?.OnReceived -= NotifyCargoChange;
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
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            _ = TaskTools.RunPool(() => OnPaxChange?.Invoke(this), Controller.RequestToken);
            return Task.CompletedTask;
        }

        protected virtual Task NotifyCargoChange(ISimResourceSubscription sub, object data)
        {
            if (!IsRunning)
            {
                Logger.Debug($"Ignoring Cargo Change - Service not running");
                return Task.CompletedTask;
            }
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            var cargo = sub.GetNumber();
            if (cargo > 0)
                Controller.Menu.BlockMenuUpdates(false);

            _ = TaskTools.RunPool(() => OnCargoChange?.Invoke(this), Controller.RequestToken);
            Logger.Information($"Cargo Loading Progress {(int)cargo}%");
            return Task.CompletedTask;
        }

        protected override Task DoStateChange(GsxServiceState state)
        {
            if (State == GsxServiceState.Requested && Controller.ApiClient.GetSetting(GsxConstants.SettingPushOnBoarding) == 0)
                Controller.Menu.BlockMenuUpdates(true, "Tug Question");
            else if (State < GsxServiceState.Requested || State >= GsxServiceState.Completed)
                Controller.Menu.BlockMenuUpdates(false);
            return Task.CompletedTask;
        }
    }
}
