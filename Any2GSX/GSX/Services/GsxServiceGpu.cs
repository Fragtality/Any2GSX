using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceGpu(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        public virtual ISimResourceSubscription SubGpuService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubGpuService;
        public virtual ISimResourceSubscription SubGpuConnect { get; protected set; }
        public virtual bool IsConnected => SubGpuConnect?.GetNumber() == 1;
        public virtual bool WasCanceled { get; protected set; } = false;

        public event Func<bool, Task> OnGpuConnection;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        public override void InitSubscriptions()
        {
            SubGpuService = SimStore.AddVariable(GsxConstants.VarServiceGpu);
            SubGpuConnect = SimStore.AddVariable(GsxConstants.VarServiceGpuConnect);

            SubGpuService?.OnReceived += OnStateChange;
            SubGpuConnect?.OnReceived += OnGpuChange;
        }

        protected override async Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            await base.OnStateChange(sub, data);
            if ((sub.GetNumber() == 1 || sub.GetNumber() == 2 || sub.GetNumber() == 3) && WasCanceled)
                WasCanceled = false;
        }

        protected virtual Task OnGpuChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.IsGsxRunning)
                return Task.CompletedTask;

            if (sub.GetNumber() == 1)
            {
                Logger.Information($"GSX GPU connected");
                _ = TaskTools.RunPool(() => OnGpuConnection?.Invoke(true), Controller.Token);
            }
            if (sub.GetNumber() == 0)
            {
                Logger.Information($"GSX GPU disconnected");
                _ = TaskTools.RunPool(() => OnGpuConnection?.Invoke(false), Controller.Token);
            }

            return Task.CompletedTask;
        }

        protected override Task DoReset()
        {
            WasCanceled = false;

            return Task.CompletedTask;
        }

        public override void FreeResources()
        {
            SubGpuConnect?.OnReceived -= OnGpuChange;
            SubGpuService?.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceGpuConnect);
            SimStore.Remove(GsxConstants.VarServiceGpu);
        }

        public override async Task Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            WasCanceled = await DoCall();
        }
    }
}
