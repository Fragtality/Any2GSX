using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceStairs(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Stairs;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubService;
        protected virtual ISimResourceSubscription SubOperating { get; set; }
        public virtual GsxServiceState OperatingState => (GsxServiceState)(int)(SubOperating?.GetNumber() ?? 0);
        protected virtual ConcurrentDictionary<GsxVehicleStair, ISimResourceSubscription> SubVehicleStairs { get; } = [];
        protected virtual ConcurrentDictionary<GsxVehicleStair, Func<ISimResourceSubscription, object, Task>> SubVehicleCallbacks { get; } = [];
        public virtual bool OverrideActive { get; protected set; } = false;

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3 || IsAnyStair((state) => state == GsxVehicleStairState.InPosition);
        public virtual bool IsOperating => (SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3 || IsAnyStair((state) => state > GsxVehicleStairState.Idle && state != GsxVehicleStairState.InPosition)) && !AllStairs((state) => state == GsxVehicleStairState.InPosition);
        public virtual bool IsConnectable => IsAvailable && !IsConnected && !CheckCalled();

        public event Func<GsxServiceState, Task> OnOperationChanged;
        public event Func<GsxVehicleStair, GsxVehicleStairState, Task> OnVehicleChanged;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(7, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(7, GsxConstants.MenuGate));

            return sequence;
        }

        public override void InitSubscriptions()
        {
            SubService = SimStore.AddVariable(GsxConstants.VarServiceStairs);
            SubOperating = SimStore.AddVariable(GsxConstants.VarServiceStairsOperation);
            SubService?.OnReceived += OnStateChange;
            SubOperating?.OnReceived += OnOperationChange;

            SubVehicleStairs.Add(GsxVehicleStair.Front, SimStore.AddVariable(GsxConstants.VarVehicleStairsFront));
            SubVehicleStairs.Add(GsxVehicleStair.Middle, SimStore.AddVariable(GsxConstants.VarVehicleStairsMiddle));
            SubVehicleStairs.Add(GsxVehicleStair.Rear, SimStore.AddVariable(GsxConstants.VarVehicleStairsRear));

            SubVehicleCallbacks.Add(GsxVehicleStair.Front, (s, v) => OnVehicleChange(GsxVehicleStair.Front, s?.GetNumber() ?? 0));
            SubVehicleCallbacks.Add(GsxVehicleStair.Middle, (s, v) => OnVehicleChange(GsxVehicleStair.Middle, s?.GetNumber() ?? 0));
            SubVehicleCallbacks.Add(GsxVehicleStair.Rear, (s, v) => OnVehicleChange(GsxVehicleStair.Rear, s?.GetNumber() ?? 0));

            SubVehicleStairs[GsxVehicleStair.Front]?.OnReceived += SubVehicleCallbacks[GsxVehicleStair.Front];
            SubVehicleStairs[GsxVehicleStair.Middle]?.OnReceived += SubVehicleCallbacks[GsxVehicleStair.Middle];
            SubVehicleStairs[GsxVehicleStair.Rear]?.OnReceived += SubVehicleCallbacks[GsxVehicleStair.Rear];
        }

        protected override Task DoReset()
        {
            OverrideActive = false;
            return ResetVehicleState();
        }

        public override void FreeResources()
        {
            SubVehicleStairs[GsxVehicleStair.Front]?.OnReceived -= SubVehicleCallbacks[GsxVehicleStair.Front];
            SubVehicleStairs[GsxVehicleStair.Middle]?.OnReceived -= SubVehicleCallbacks[GsxVehicleStair.Middle];
            SubVehicleStairs[GsxVehicleStair.Rear]?.OnReceived -= SubVehicleCallbacks[GsxVehicleStair.Rear];
            SubVehicleCallbacks.Clear();

            SimStore.Remove(GsxConstants.VarVehicleStairsFront);
            SimStore.Remove(GsxConstants.VarVehicleStairsMiddle);
            SimStore.Remove(GsxConstants.VarVehicleStairsRear);
            SubVehicleStairs.Clear();

            SubService?.OnReceived -= OnStateChange;
            SubOperating?.OnReceived -= OnOperationChange;
            SimStore.Remove(GsxConstants.VarServiceStairs);
            SimStore.Remove(GsxConstants.VarServiceStairsOperation);
        }

        protected override bool CheckCalled()
        {
            IsCalled = IsOperating || IsRunning || IsAnyStair((state) => state > GsxVehicleStairState.Idle);
            return IsCalled;
        }

        protected override Task<bool> DoCall()
        {
            if (IsAvailable)
                return base.DoCall();
            else
                return Task.FromResult(true);
        }

        public virtual Task Remove()
        {
            if (IsOperating || !IsConnected)
                return Task.CompletedTask;

            return base.DoCall();
        }

        protected virtual Task OnVehicleChange(GsxVehicleStair stair, double state)
        {
            if (OverrideActive)
                return Task.CompletedTask;

            Logger.Debug($"Vehicle State Change for {stair}: {(int)(state)}");
            CheckCalled();
            _ = TaskTools.RunPool(() => OnVehicleChanged?.Invoke(stair, (GsxVehicleStairState)(int)state), Controller.Token);
            return Task.CompletedTask;
        }

        protected virtual Task OnOperationChange(ISimResourceSubscription sub, object data)
        {
            if (OverrideActive)
                return Task.CompletedTask;

            int state = (int)(sub?.GetNumber() ?? 0);
            //GSX Workaround
            if (state == 5 && SubStateVar?.GetNumber() == 5 && AllStairs((state) => state == GsxVehicleStairState.InPosition))
            {
                Logger.Debug($"Applying GSX Stair Workaround on Operating");
                _ = TaskTools.RunDelayed(() => SubOperating.WriteValue(1), 250);
                return Task.CompletedTask;
            }
            //

            Logger.Debug($"Operation State Change for {Type}: {state}");
            CheckCalled();
            _ = TaskTools.RunPool(() => OnOperationChanged?.Invoke(OperatingState), Controller.Token);
            return Task.CompletedTask;
        }

        protected override Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (OverrideActive)
                return Task.CompletedTask;

            //GSX Workaround
            if (ReadState() == GsxServiceState.Callable && Controller.ServiceBoard.State == GsxServiceState.Requested)
            {
                Logger.Debug($"Applying GSX Stair Workaround on Board");
                _ = TaskTools.RunPool(async () =>
                {
                    OverrideActive = true;
                    await Task.Delay(250);
                    await SubStateVar.WriteValue(5);
                    await SubOperating.WriteValue(1);
                    await Task.Delay(250);
                    OverrideActive = false;
                });
                return Task.CompletedTask;
            }
            //

            CheckCalled();
            return base.OnStateChange(sub, data);
        }

        public virtual GsxVehicleStairState GetStairState(GsxVehicleStair stair)
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
            return Enum.GetValues<GsxVehicleStair>().Any((s) => func(GetStairState(s)));
        }

        public virtual bool AllStairs(Func<GsxVehicleStairState, bool> func)
        {
            return Enum.GetValues<GsxVehicleStair>().All((s) => func(GetStairState(s)));
        }

        public virtual bool StairsExtending()
        {
            return AllStairs((state) => state == GsxVehicleStairState.Extending || state == GsxVehicleStairState.Completing || state == GsxVehicleStairState.InPosition);
        }

        public virtual async Task ResetVehicleState()
        {
            OverrideActive = true;
            try
            {
                await SubVehicleStairs[GsxVehicleStair.Front]?.WriteValue(1);
                await SubVehicleStairs[GsxVehicleStair.Middle]?.WriteValue(1);
                await SubVehicleStairs[GsxVehicleStair.Rear]?.WriteValue(1);
            }
            catch { }
            OverrideActive = false;
        }
    }
}
