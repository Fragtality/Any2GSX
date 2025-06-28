using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface ICommBus
    {
        public Task SendCommBus(string @event, string data, BroadcastFlag flag = BroadcastFlag.DEFAULT);
        public Task RegisterCommBus(string @event, BroadcastFlag eventSource, Action<string, string> callback);
        public Task UnregisterCommBus(string @event, BroadcastFlag eventSource, Action<string, string> callback);
        public Task ExecuteCalculatorCode(string code);
    }
}
