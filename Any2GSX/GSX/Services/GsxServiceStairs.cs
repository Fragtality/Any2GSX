using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceStairs() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Stairs;
        protected override int MenuSelectionService => 7;
        protected override string StateVarName => GsxConstants.VarServiceStairs;
        protected override List<string> ServiceVariables => [GsxConstants.VarServiceStairsOperation, GsxConstants.VarVehicleStairsFront, GsxConstants.VarVehicleStairsMiddle, GsxConstants.VarVehicleStairsRear];
        protected virtual ISimResourceSubscription SubOperating { get; set; }
        public virtual GsxServiceState OperatingState => ReadOperatingState();
        protected virtual ConcurrentDictionary<GsxVehicleLocation, ISimResourceSubscription> SubVehicleStairs { get; } = [];
        protected virtual ConcurrentDictionary<GsxVehicleLocation, Func<ISimResourceSubscription, object, Task>> SubVehicleCallbacks { get; } = [];
        public override bool IsAvailable => Controller.GateServicesValid && State != GsxServiceState.NotAvailable && State != GsxServiceState.Unknown;
        public virtual bool IsConnected => ReadState() == GsxServiceState.Active && ReadOperatingState() < GsxServiceState.Bypassed || IsAnyStair((state) => state == GsxVehicleStairState.InPosition);
        public virtual bool IsOperating => (ReadState() == GsxServiceState.Requested || ReadOperatingState() > GsxServiceState.Bypassed || IsAnyStair((state) => state > GsxVehicleStairState.Idle && state != GsxVehicleStairState.InPosition)) && !AllStairs((state) => state == GsxVehicleStairState.InPosition);
        public virtual bool IsConnectable => IsAvailable && !IsConnected && !EvaluateCalled();

        public event Func<GsxServiceState, Task> OnOperationChanged;
        public event Func<GsxVehicleLocation, GsxVehicleStairState, Task> OnVehicleChanged;

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void DoInit()
        {
            SubOperating = SimStore[GsxConstants.VarServiceStairsOperation];
            SubOperating?.OnReceived += OnOperationChange;

            SubVehicleStairs.Add(GsxVehicleLocation.Front, SimStore[GsxConstants.VarVehicleStairsFront]);
            SubVehicleStairs.Add(GsxVehicleLocation.Middle, SimStore[GsxConstants.VarVehicleStairsMiddle]);
            SubVehicleStairs.Add(GsxVehicleLocation.Rear, SimStore[GsxConstants.VarVehicleStairsRear]);

            SubVehicleCallbacks.Add(GsxVehicleLocation.Front, (s, v) => OnVehicleChange(GsxVehicleLocation.Front, s?.GetNumber() ?? 0));
            SubVehicleCallbacks.Add(GsxVehicleLocation.Middle, (s, v) => OnVehicleChange(GsxVehicleLocation.Middle, s?.GetNumber() ?? 0));
            SubVehicleCallbacks.Add(GsxVehicleLocation.Rear, (s, v) => OnVehicleChange(GsxVehicleLocation.Rear, s?.GetNumber() ?? 0));

            SubVehicleStairs[GsxVehicleLocation.Front]?.OnReceived += SubVehicleCallbacks[GsxVehicleLocation.Front];
            SubVehicleStairs[GsxVehicleLocation.Middle]?.OnReceived += SubVehicleCallbacks[GsxVehicleLocation.Middle];
            SubVehicleStairs[GsxVehicleLocation.Rear]?.OnReceived += SubVehicleCallbacks[GsxVehicleLocation.Rear];
        }

        protected override Task DoReset()
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubVehicleStairs[GsxVehicleLocation.Front]?.OnReceived -= SubVehicleCallbacks[GsxVehicleLocation.Front];
            SubVehicleStairs[GsxVehicleLocation.Middle]?.OnReceived -= SubVehicleCallbacks[GsxVehicleLocation.Middle];
            SubVehicleStairs[GsxVehicleLocation.Rear]?.OnReceived -= SubVehicleCallbacks[GsxVehicleLocation.Rear];
            SubVehicleCallbacks.Clear();
            SubVehicleStairs.Clear();

            SubOperating?.OnReceived -= OnOperationChange;
        }

        protected virtual GsxServiceState ReadOperatingState()
        {
            try
            {
                return (GsxServiceState)(SubOperating?.GetNumber() ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected override bool EvaluateCalled()
        {
            return base.EvaluateCalled() || IsOperating;
        }

        protected override Task<bool> DoCall()
        {
            if (IsAvailable)
                return base.DoCall();
            else
                return Task.FromResult(IsConnected || IsOperating);
        }

        public virtual Task<bool> Remove()
        {
            if (IsOperating || !IsConnected)
            {
                Logger.Debug($"Stair Remove call blocked");
                return Task.FromResult(false);
            }

            return base.DoCall();
        }

        public override Task<bool> Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            return Remove();
        }

        protected virtual Task OnVehicleChange(GsxVehicleLocation stair, double state)
        {
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            Logger.Debug($"Vehicle State Change for Stair {stair}: {(GsxVehicleStairState)(int)(state)}");
            _ = TaskTools.RunPool(() => OnVehicleChanged?.Invoke(stair, (GsxVehicleStairState)(int)state), Controller.RequestToken);
            return Task.CompletedTask;
        }

        protected virtual Task OnOperationChange(ISimResourceSubscription sub, object data)
        {
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            Logger.Debug($"Operation State Change for {Type}: {OperatingState}");
            _ = TaskTools.RunPool(() => OnOperationChanged?.Invoke(OperatingState), Controller.RequestToken);
            return Task.CompletedTask;
        }

        public virtual GsxVehicleStairState GetStairState(GsxVehicleLocation stair)
        {
            try
            {
                return (GsxVehicleStairState)(int)SubVehicleStairs[stair].GetNumber();
            }
            catch
            {
                return GsxVehicleStairState.Unknown;
            }
        }

        public virtual bool IsAnyStair(Func<GsxVehicleStairState, bool> func)
        {
            return Enum.GetValues<GsxVehicleLocation>().Any((s) => func(GetStairState(s)));
        }

        public virtual bool AllStairs(Func<GsxVehicleStairState, bool> func)
        {
            return Enum.GetValues<GsxVehicleLocation>().All((s) => func(GetStairState(s)));
        }

        public override async Task ResetVehicleState(bool force = false)
        {
            InhibitStateUpdates = true;
            try
            {
                if (force || (SubStateVar.GetNumber() <= (int)GsxServiceState.Bypassed && IsAnyStair((state) => state > GsxVehicleStairState.Idle)) || Controller.Tracker.IsActive(AppNotification.GsxRestart))
                {
                    Logger.Debug($"Reset Vehicle State for {Type}");
                    await SubVehicleStairs[GsxVehicleLocation.Front]?.WriteValue(0);
                    await SubVehicleStairs[GsxVehicleLocation.Middle]?.WriteValue(0);
                    await SubVehicleStairs[GsxVehicleLocation.Rear]?.WriteValue(0);
                }
            }
            catch { }
            InhibitStateUpdates = false;
        }
    }
}
