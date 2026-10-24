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
    public class GsxServiceGpu() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        protected override int MenuSelectionService => 1;
        protected override string StateVarName => GsxConstants.VarServiceGpu;
        protected override List<string> ServiceVariables => [GsxConstants.VarServiceGpuConnect];
        protected virtual ISimResourceSubscription SubGpuConnect { get; set; }
        public virtual bool IsConnected => SubGpuConnect?.GetNumber() == 1;

        public event Func<bool, Task> OnGpuConnection;

        protected override void DoInit()
        {
            SubGpuConnect = SimStore[GsxConstants.VarServiceGpuConnect];
            SubGpuConnect?.OnReceived += OnGpuChange;
        }

        protected override Task DoReset(bool resetVariable)
        {
            if (resetVariable)
                return SubGpuConnect.WriteValue(0);
            else
                return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubGpuConnect?.OnReceived -= OnGpuChange;
        }

        protected override async Task OnCouatlStopped(IGsxController gsxController)
        {
            await base.OnCouatlStopped(gsxController);
            await ResetState(true);
        }

        protected override bool EvaluateCompleted(GsxServiceState state)
        {
            return state == GsxServiceState.Completed;
        }

        protected virtual Task OnGpuChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.IsGsxRunning || InhibitStateUpdates)
                return Task.CompletedTask;

            if (sub.GetNumber() == 1)
            {
                Logger.Information($"GSX GPU connected");
                _ = TaskTools.RunPool(() => OnGpuConnection?.Invoke(true), Controller.RequestToken);
            }
            if (sub.GetNumber() == 0)
            {
                Logger.Information($"GSX GPU disconnected");
                _ = TaskTools.RunPool(() => OnGpuConnection?.Invoke(false), Controller.RequestToken);
            }

            return Task.CompletedTask;
        }

        public virtual Task SetGpuConnection(bool target)
        {
            return SimStore[GsxConstants.VarServiceGpuConnect].WriteValue(target ? 1 : 0);
        }
    }
}
