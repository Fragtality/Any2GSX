using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceJetway(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Jetway;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubService;
        protected virtual ISimResourceSubscription SubOperating { get; set; }
        public virtual GsxServiceState OperatingState => (GsxServiceState)(int)(SubOperating?.GetNumber() ?? 0);

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3;
        public virtual bool IsOperating => SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3;
        public virtual bool IsConnectable => IsAvailable && !IsConnected && !CheckCalled();
        public event Func<GsxServiceState, Task> OnOperationChanged;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(6, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        public override void InitSubscriptions()
        {
            SubService = SimStore.AddVariable(GsxConstants.VarServiceJetway);
            SubOperating = SimStore.AddVariable(GsxConstants.VarServiceJetwayOperation);
            SubService?.OnReceived += OnStateChange;
            SubOperating?.OnReceived += OnOperationChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubService?.OnReceived -= OnStateChange;
            SubOperating?.OnReceived -= OnOperationChange;
            SimStore.Remove(GsxConstants.VarServiceJetway);
            SimStore.Remove(GsxConstants.VarServiceJetwayOperation);
        }

        protected virtual Task OnOperationChange(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"Operation State Change for {Type}: {(int)(sub?.GetNumber() ?? 0)}");
            _ = TaskTools.RunPool(() => OnOperationChanged?.Invoke(OperatingState), Controller.Token);
            return Task.CompletedTask;
        }

        protected override bool CheckCalled()
        {
            IsCalled = IsOperating || IsRunning;
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
            if (!IsConnected || !IsAvailable || IsOperating)
                return Task.CompletedTask;

            return base.DoCall();
        }

        protected override async Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            await base.OnStateChange(sub, data);
            if (State == GsxServiceState.Callable && IsCalled)
            {
                Logger.Debug($"Reset IsCalled for Jetway");
                IsCalled = false;
            }
        }

        public override Task Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            return Remove();
        }
    }
}
