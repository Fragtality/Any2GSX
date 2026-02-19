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
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(1, GsxConstants.MenuAdditionalServices) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void InitSubscriptions()
        {
            SubGpuService = SimStore.AddVariable(GsxConstants.VarServiceGpu);
            SubGpuConnect = SimStore.AddVariable(GsxConstants.VarServiceGpuConnect);

            SubGpuService.OnReceived += OnStateChange;
            SubGpuConnect.OnReceived += OnGpuChange;
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            base.OnStateChange(sub, data);
            if ((sub.GetNumber() == 1 || sub.GetNumber() == 2 || sub.GetNumber() == 3) && WasCanceled)
                WasCanceled = false;
        }

        protected virtual void OnGpuChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.IsGsxRunning)
                return;

            if (sub.GetNumber() == 1)
            {
                Logger.Information($"GSX GPU connected");
                TaskTools.RunLogged(() => OnGpuConnection?.Invoke(true), Controller.Token);
            }
            if (sub.GetNumber() == 0)
            {
                Logger.Information($"GSX GPU disconnected");
                TaskTools.RunLogged(() => OnGpuConnection?.Invoke(false), Controller.Token);
            }
        }

        protected override void DoReset()
        {
            WasCanceled = false;
        }

        public override void FreeResources()
        {
            SubGpuConnect.OnReceived -= OnGpuChange;
            SubGpuService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceGpuConnect);
            SimStore.Remove(GsxConstants.VarServiceGpu);
        }

        public override async Task Cancel(int option = -1)
        {
            WasCanceled = await DoCall();
        }
    }
}
