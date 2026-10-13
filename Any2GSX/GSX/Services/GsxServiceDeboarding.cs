using Any2GSX.AppConfig;
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
    public class GsxServiceDeboarding() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Deboarding;
        protected override int MenuSelectionService => 1;
        protected override string StateVarName => GsxConstants.VarServiceDeboarding;
        protected override List<string> ServiceVariables => [GsxConstants.VarPaxTarget, GsxConstants.VarPaxTotalDeboard, GsxConstants.VarCargoPercentDeboard, GsxConstants.VarNoCrewDeboard, GsxConstants.VarNoPilotsDeboard];
        public virtual int PaxTarget => (int)(SubPaxTarget?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubPaxTarget { get; set; }
        public virtual bool WasTargetSet { get; protected set; } = false;
        public virtual int PaxTotal => (int)(SubPaxTotal?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubPaxTotal { get; set; }
        public virtual int CargoPercent => (int)(SubCargoPercent?.GetNumber() ?? 0);
        protected virtual ISimResourceSubscription SubCargoPercent { get; set; }
        public virtual double CargoScalar => CargoPercent / 100.0;
        public override bool RequiresStair => !Controller.HasGateJetway || (Controller.HasGateStair && Controller.Aircraft.HasRearStair());

        public event Func<GsxServiceDeboarding, Task> OnPaxChange;
        public event Func<GsxServiceDeboarding, Task> OnCargoChange;

        protected override void DoInit()
        {
            SubPaxTarget = SimStore[GsxConstants.VarPaxTarget];
            SubPaxTotal = SimStore[GsxConstants.VarPaxTotalDeboard];
            SubPaxTotal?.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore[GsxConstants.VarCargoPercentDeboard];
            SubCargoPercent?.OnReceived += NotifyCargoChange;
        }

        protected override Task OnCouatlStarted(IGsxController gsxController)
        {
            if (WasTargetSet && Controller.AutomationController.State >= AutomationState.Flight && Controller.AutomationController.State < AutomationState.TurnAround)
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
            if (Profile.RunAutomationService && Profile.SkipCrewDeboardQuestion)
            {
                Logger.Debug($"Skip Crew Deboarding");
                await SimStore[GsxConstants.VarNoCrewDeboard].WriteValue(1);
                await SimStore[GsxConstants.VarNoPilotsDeboard].WriteValue(1);
                Logger.Debug($"Crew Deboarding Variables set");
            }

            if (!Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 1)
            {
                Logger.Debug($"Resetting Crew Skip Variables");
                await SimStore[GsxConstants.VarNoCrewDeboard].WriteValue(0);
                await SimStore[GsxConstants.VarNoPilotsDeboard].WriteValue(0);
                Logger.Debug($"Setting Pilot Target to {Controller.Profile.DefaultPilotTarget}");
                await Controller?.SubPilotTarget?.WriteValue(Controller.Profile.DefaultPilotTarget);
                Logger.Debug($"Setting Crew Target to {Controller.Profile.DefaultCrewTarget}");
                await Controller?.SubCrewTarget?.WriteValue(Controller.Profile.DefaultCrewTarget);
            }

            Logger.Debug($"Eval Pax Target");
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
            _ = TaskTools.RunPool(() => OnCargoChange?.Invoke(this), Controller.RequestToken);
            Logger.Information($"Cargo Unloading Progress {(int)cargo}%");

            if (cargo >= 90 && !Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 0 && Controller.ApiClient.GetSetting(GsxConstants.SettingIgnoreCrew) == 0)
                Controller.Menu.BlockMenuUpdates(true, "Crew Question");

            return Task.CompletedTask;
        }

        protected override async Task DoStateChange(GsxServiceState state)
        {
            if (state >= GsxServiceState.Completed && !Profile.SkipCrewDeboardQuestion && Profile.AnswerCrewDeboardQuestion != 0 && Controller.ApiClient.GetSetting(GsxConstants.SettingIgnoreCrew) == 0)
            {
                Controller.Menu.BlockMenuUpdates(true, "Crew Question");
                _ = TaskTools.RunDelayed(() => Controller.Menu.BlockMenuUpdates(false), Controller.Config.OperatorSelectTimeout, Controller.RequestToken);
            }
            else
                Controller.Menu.BlockMenuUpdates(false);
        }
    }
}
