using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX.API;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Messages;
using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using CFIT.AppTools;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Any2GSX.GSX.API
{
    public class GsxApiData : IGsxApiData
    {
        public virtual GsxMessageHello HelloData { get; protected set; } = new();
        public virtual bool HasHandlerData => HandlerData?.aircraft?.exits?.Count > 0;
        public virtual GsxPayloadHandlerData HandlerData { get; protected set; } = new();

        public virtual ConcurrentDictionary<GsxServiceType, GsxPayloadService> ServiceMap { get; } = [];
        public virtual bool GsxRunning => HelloData?.gsxRunning == true;
        public virtual bool MenuLoading { get; protected set; } = true;
        public virtual int State { get; protected set; } = 0;

        public virtual bool MenuReceived { get; protected set; } = false;
        public virtual bool MenuShown { get; protected set; } = false;
        public virtual bool SearchActive { get; protected set; } = false;
        public virtual int SearchSession { get; protected set; } = -1;
        public virtual GsxPayloadMenu Menu { get; protected set; } = new();
        public virtual bool HasSettingMap => SettingMap?.Count > 0;
        public virtual ConcurrentDictionary<string, GsxPayloadSetting> SettingMap { get; } = [];

        public virtual bool CheckApiState()
        {
            return MenuReceived && HasHandlerData && HasSettingMap;
        }

        public virtual void ParseDataHandler(JsonNode handlerData)
        {
            HandlerData = JsonSerializer.Deserialize<GsxPayloadHandlerData>(handlerData);
        }

        public virtual void ParseDataServices(JsonNode node)
        {
            var services = node!?.AsArray() ?? null;
            if (services == null)
                return;

            ServiceMap.Clear();
            foreach (JsonNode service in services)
            {
                var data = JsonSerializer.Deserialize<GsxPayloadService>(service);
                ServiceMap.Add(data.Type, data);
            }
        }

        public virtual void ParseDataHello(GsxMessageHello hello)
        {
            HelloData = hello;
        }

        public virtual void ParseDataEvent(GsxMessageEvent @event)
        {
            if (@event.HasRunning)
                HelloData.gsxRunning = @event.gsxRunning;

            if (@event.HasModel)
                MenuLoading = @event.model!?["active"]!?.GetValue<bool>() == true || @event.model!?["bars"]!?.AsArray()!?.Count > 0;
        }

        public virtual void ParseDataStartup(JsonNode node)
        {
            if (node!?["active"] != null && node!?["bars"] != null)
                MenuLoading = node!?["active"]!?.GetValue<bool>() == true || node!?["bars"]!?.AsArray()!?.Count > 0;
        }

        public virtual void ParseDataState(JsonNode node)
        {
            State = node?.GetValue<int>() ?? 0;
        }

        public virtual void ParseDataSearch(JsonNode node)
        {
            if (node!?["active"] != null && node!?["session"] != null)
            {
                SearchActive = node!["active"]!?.GetValue<bool>() ?? false;
                SearchSession = node!["session"]!?.GetValue<int>() ?? -1;
            }
        }

        public virtual void ParseDataMenuShown(JsonNode node)
        {
            MenuShown = node?.GetValue<bool>() ?? false;

            if (!MenuShown)
            {
                SearchActive = false;
                SearchSession = -1;
            }
        }

        public virtual void ParseDataMenu(JsonNode menu)
        {
            Menu = JsonSerializer.Deserialize<GsxPayloadMenu>(menu);
            MenuReceived = true;
        }

        public virtual void ParseDataSettings(JsonNode node)
        {
            var tabs = node!?["tabs"]!?.AsArray() ?? [];

            foreach (JsonNode tab in tabs)
            {
                if (tab!["subtabs"] != null)
                {
                    var subtabs = tab!["subtabs"].AsArray();
                    foreach (JsonNode subtab in subtabs)
                    {
                        if (subtab!["fields"] != null)
                            ParseDataSettingFields(subtab!["fields"].AsArray());
                    }
                }
                else if (tab!["fields"] != null)
                {
                    ParseDataSettingFields(tab!["fields"].AsArray());
                }
            }
        }

        protected virtual void ParseDataSettingFields(JsonArray fields)
        {

            foreach (JsonNode field in fields)
            {
                if (field == null || field!?["key"] == null)
                    continue;
                var setting = JsonSerializer.Deserialize<GsxPayloadSetting>(field);

                if (setting?.key == null)
                    continue;
                if (!SettingMap.TryAdd(setting.key, setting))
                    SettingMap[setting.key] = setting;
            }
        }
    }
}
