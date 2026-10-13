using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Tools;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.API
{
#pragma warning disable IDE1006
    public enum ApiRequestType
    {
        unknown = 0,
        setExternal,
        callService,
        cancelService,
        queueService,
        setPaxCount,
        setBagCount,
        setPaxDiff,
        setFuelPlanned,
        sendFinal,
        setSavedFob,
        setPushDirection,
        callPushback,
        setGateArrival,
        smartCall,
        setProfile,
        getProfile,
    }

    public class RequestPayload
    {
        public virtual ApiRequestType request { get; set; } = ApiRequestType.unknown;
        [JsonIgnore]
        public virtual bool HasData => data != null && data.GetValueKind() != JsonValueKind.Null;
        public virtual JsonNode data { get; set; } = JsonValue.Create(JsonValueKind.Null);
        public virtual string? handler { get; set; } = null;

        public virtual bool HasDataKey(string key)
        {
            return data!?[key] != null && data!?[key]!?.GetValueKind() != JsonValueKind.Null;
        }

        public virtual bool TryGetDataKey<T>(string key, out T value)
        {
            value = default;

            try
            {
                if (HasData && data[key]!?.GetValueKind() != JsonValueKind.Null)
                {
                    value = data[key].GetValue<T>();
                    return true;
                }
                else
                    return false;
            }
            catch { }

            return false;
        }

        public virtual bool TryGetData<T>(out T value)
        {
            value = default;

            try
            {
                if (HasData && data!?.GetValueKind() != JsonValueKind.Null)
                {
                    value = data.GetValue<T>();
                    return true;
                }
                else
                    return false;
            }
            catch { }

            return false;
        }

        public virtual T DeserializeData<T>()
        {
            return JsonSerializer.Deserialize<T>(data, AppServer.SerializerOptions);
        }
    }

    public class PayloadService
    {
        public virtual GsxServiceType service { get; set; }
    }

    public class PayloadQueueService : PayloadService
    {
        public virtual GsxServiceActivation? activation { get; set; }
        public virtual TimeSpan? timeDeparture { get; set; }
        public virtual GsxServiceDependency? serviceDependency { get; set; }
        public virtual GsxServiceDependencyScope? dependencyScope { get; set; }
    }

    public class PayloadSetFob
    {
        public virtual string? title { get; set; } = null;
        public virtual int? fuel { get; set; } = 0;
        public virtual bool? set { get; set; } = false;
    }
#pragma warning restore IDE1006
}
