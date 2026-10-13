using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces.GSX
{
    public interface IGsxService
    {
        public GsxServiceType Type { get; }
        public GsxMenuCommandOperatorType OperatorType { get; }
        public bool RequiresStair { get; }
        public bool HasStairConflict { get; }
        public bool HasSubmenu { get; }

        public bool IsCalled { get; }
        public bool SequenceIsSuccess { get; }

        public GsxServiceState State { get; }
        public GsxServiceState RawState { get; }
        public string TextState { get; }
        public GsxServiceState StateOverride { get; }

        public DateTime ActivationTime { get; }

        public bool InhibitStateUpdates { get; }
        public bool IsStateOverridden { get; }
        public bool IsCallable { get; }
        public bool IsAvailable { get; }
        public bool IsBypassed { get; }
        public bool IsRequested { get; }
        public bool IsRunning { get; }
        public bool IsActive { get; }
        public bool IsCompleted { get; }
        public bool IsCompleting { get; }
        public bool WasRunning { get; }
        public bool WasCanceled { get; }

        public event Func<IGsxService, Task> OnStateChanged;

        public void InitSubscriptions();
        public void ReleaseSubscriptions();

        public Task ResetState(bool resetVariable = false);
        public Task ResetVehicleState(bool force = false);
        public void SetStateOverride(GsxServiceState overrideState);
        public void ClearStateOverride();
        public void ForceComplete();

        public Task<bool> Call();
        public Task<bool> Cancel(GsxCancelService option = GsxCancelService.Complete);
    }
}
