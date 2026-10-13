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
    public class GsxServiceRefuel() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Refuel;
        protected override int MenuSelectionService => 3;
        protected override string StateVarName => GsxConstants.VarServiceRefuel;
        protected override List<string> ServiceVariables => [GsxConstants.VarServiceRefuelHose];
        public override GsxMenuCommandOperatorType OperatorType => GsxMenuCommandOperatorType.Refuel;
        public override bool HasStairConflict => Controller.HasGateStair && Controller.Aircraft.HasRearStair() && Controller.IsRefuelOnStairSide && !IsUnderground;
        protected virtual ISimResourceSubscription SubRefuelHose { get; set; }
        public virtual bool IsHoseConnected => IsActive && SubRefuelHose?.GetNumber() == 1;
        public virtual bool IsUnderground => Controller.ApiData?.HandlerData?.gate?.undergroundRefueling ?? false;
        public virtual bool WasHoseConnected { get; protected set; } = false;

        public event Func<bool, Task> OnHoseConnection;

        protected override void DoInit()
        {
            SubRefuelHose = SimStore[GsxConstants.VarServiceRefuelHose];
            SubRefuelHose?.OnReceived += OnHoseChange;
        }

        protected override Task DoReset()
        {
            WasHoseConnected = false;
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubRefuelHose?.OnReceived -= OnHoseChange;
        }

        protected override Task DoStateChange(GsxServiceState state)
        {
            if (state == GsxServiceState.Requested)
                WasHoseConnected = false;
            else if (state == GsxServiceState.Active)
            {
                try
                {
                    WasHoseConnected = false;
                    if (Controller.HasRefuelDialog)
                        Controller.Menu.BlockMenuUpdates(true, "Fuel Dialog");
                    _ = TaskTools.RunDelayed(() => Controller.Tracker.Clear(AppNotification.UpdatesBlocked), Controller.Config.OperatorSelectTimeout, Controller.RequestToken);
                }
                catch { }
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnHoseChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.IsGsxRunning || InhibitStateUpdates)
                return Task.CompletedTask;

            if (sub.GetNumber() == 1 && State != GsxServiceState.Unknown && State != GsxServiceState.Completed)
            {
                Logger.Information($"Fuel Hose connected");
                if (State == GsxServiceState.Active)
                {
                    WasHoseConnected = true;
                    ActivationTime = DateTime.Now;
                    _ = TaskTools.RunPool(() => OnHoseConnection?.Invoke(true), Controller.RequestToken);
                }
            }
            else if (sub.GetNumber() == 0 && State != GsxServiceState.Unknown && WasRunning)
            {
                Logger.Information($"Fuel Hose disconnected");
                _ = TaskTools.RunPool(() => OnHoseConnection?.Invoke(false), Controller.RequestToken);
            }

            return Task.CompletedTask;
        }
    }
}
