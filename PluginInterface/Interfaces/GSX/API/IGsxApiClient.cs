using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API
{
    public interface IGsxApiClient
    {
        public IGsxApiData IApiData { get; }
        public bool IsSocketValid { get; }
        public bool IsSubscribed { get; }
        public bool HelloReceived { get; }
        public bool IsConnected { get; }
        public bool IsReady { get; }

        public static Dictionary<GsxCommandRunType, string> CommandRunTypes { get; } = new()
        {
            { GsxCommandRunType.CustomizeAirport, "CUSTOMIZE_AIRPORT_POSITION" },
            { GsxCommandRunType.CustomizeAircraft, "CUSTOMIZE_AIRPLANE" },
            { GsxCommandRunType.RestartCouatl, "RESTART_COUATL" },
            { GsxCommandRunType.ReloadSimbrief, "RELOAD_SIMBRIEF" },
        };

        public event Func<GsxMenuState, string, string[], Task> MenuDataReceived;
        public event Func<bool, Task> ReadyStateChanged;
        public event Func<string, Task> AirportNameChanged;
        public event Func<string, Task> GateNameChanged;
        public event Func<Task> AircraftHandlerChanged;

        public Task<GsxMessageResult> CallService(GsxServiceType type);
        public Task<GsxMessageResult> MenuOpen();
        public Task<GsxMessageResult> MenuClose();
        public Task<GsxMessageResult> MenuSelect(int index);
        public dynamic GetSetting(string key);
        public T GetSetting<T>(string key);
        public Task<GsxMessageResult> SetSetting(string key, dynamic value);
        public Task<GsxMessageResult> SetHandlerGate(string prop, dynamic value);
        public Task<GsxMessageResult> SetHandlerAircraft(string prop, dynamic value);
        public Task<GsxMessageResult> GateSelect(string name, bool force = false);
        public Task<GsxMessageResult> GateSelect(int number, bool force = false);
        public Task<GsxMessageResult> InputSubmit(int session, string text);
        public Task<GsxMessageResult> InputCancel(int session);
        public Task<GsxMessageResult> MenuSearch(string text);
        public Task<GsxMessageResult> CommandRun(GsxCommandRunType type);
    }
}
