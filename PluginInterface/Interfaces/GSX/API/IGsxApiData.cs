using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using System.Collections.Concurrent;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API
{
    public interface IGsxApiData
    {
        public GsxMessageHello HelloData { get; }
        public bool HasHandlerData { get; }
        public GsxPayloadHandlerData HandlerData { get; }
        public ConcurrentDictionary<GsxServiceType, GsxPayloadService> ServiceMap { get; }
        public bool GsxRunning { get; }
        public bool MenuLoading { get; }
        public int State { get; }

        public bool MenuReceived { get; }
        public bool MenuShown { get; }
        public bool SearchActive { get; }
        public int SearchSession { get; }
        public GsxPayloadMenu Menu { get; }
        public bool HasSettingMap { get; }
        public ConcurrentDictionary<string, GsxPayloadSetting> SettingMap { get; }

        public bool CheckApiState();
    }
}
