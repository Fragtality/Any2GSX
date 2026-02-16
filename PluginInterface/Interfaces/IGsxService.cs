using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IGsxService
    {
        public GsxServiceType Type { get; }        
        public bool IsCalled { get; }
        public GsxServiceState State { get; }
        public GsxServiceState StateOverride { get; set; }
        public bool IsStateOverridden { get; }
        public bool IsCalling { get; }
        public bool IsRunning { get; }
        public bool IsActive { get; }
        public bool IsCompleted { get; }
        public bool IsCompleting { get; }
        public bool IsSkipped { get; }
        public bool WasActive { get; }
        public DateTime ActivationTime { get; }

        public event Func<IGsxService, Task> OnActive;
        public event Func<IGsxService, Task> OnCompleted;
        public event Func<IGsxService, Task> OnStateChanged;
    }
}
