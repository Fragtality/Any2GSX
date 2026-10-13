using Any2GSX.Notifications;
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
    public class GsxServiceCleaning() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Cleaning;
        protected override int MenuSelectionService => 5;
        protected override string StateVarName => GsxConstants.VarServiceCleaning;
        protected override List<string> ServiceVariables => [GsxConstants.VarVehicleCleaning];
        protected virtual ISimResourceSubscription SubVehicleCleaning { get; set; }
        public override bool RequiresStair => true;
        public override bool IsActive => IsRunning && GetTruckState() == 3;

        public event Func<int, Task> OnVehicleChanged;

        protected override void DoInit()
        {
            SubVehicleCleaning = SimStore[GsxConstants.VarVehicleCleaning];
            SubVehicleCleaning?.OnReceived += OnVehicleChange;
        }

        protected override Task DoReset()
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubVehicleCleaning?.OnReceived -= OnVehicleChange;
        }

        protected override GsxServiceState ReadState(bool raw = false)
        {
            var state = base.ReadState(raw);
            if (EvaluateRunning(state) && GetTruckState() == 3 && !raw)
                return GsxServiceState.Active;
            else
                return state;
        }

        protected virtual int GetTruckState()
        {
            return (int)(SubVehicleCleaning?.GetNumber() ?? 0);
        }

        protected virtual Task OnVehicleChange(ISimResourceSubscription sub, object value)
        {
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            int state = GetTruckState();
            Logger.Debug($"Vehicle State Change for Cleaning: {state}");
            _ = TaskTools.RunPool(() => OnVehicleChanged?.Invoke(state), Controller.RequestToken);

            if (GetTruckState() == 3)
            {
                StatesReceived.TryAdd(GsxServiceState.Active, true);
                ActivationTime = DateTime.Now;
                NotifyStateChange(GsxServiceState.Active);
            }
            return Task.CompletedTask;
        }

        public override async Task ResetVehicleState(bool force = false)
        {
            InhibitStateUpdates = true;
            try
            {
                if (force || (SubStateVar.GetNumber() <= (int)GsxServiceState.Bypassed && GetTruckState() > 1) || Controller.Tracker.IsActive(AppNotification.GsxRestart))
                {
                    Logger.Debug($"Reset Vehicle State for {Type}");
                    await SubVehicleCleaning?.WriteValue(0);
                }
            }
            catch { }
            InhibitStateUpdates = false;
        }
    }
}
