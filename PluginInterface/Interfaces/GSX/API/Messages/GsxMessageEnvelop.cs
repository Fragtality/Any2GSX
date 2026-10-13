using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using CFIT.AppLogger;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Messages
{
#pragma warning disable IDE1006
    public class GsxMessageEnvelop()
    {
        public virtual int v { get; set; } = 0;
        public virtual string type { get; set; } = GsxMessageType.unknown.ToString();
        public virtual ulong ts { get; set; } = 0;
        [JsonIgnore]
        public virtual JsonNode JsonData { get; protected set; } = JsonValue.Create(new object());
        [JsonIgnore]
        public virtual GsxMessageType MessageType => GetMessageType();

        public static GsxMessageEnvelop Parse(string jsonString)
        {
            GsxMessageEnvelop message = new();
            try
            {
                message = JsonSerializer.Deserialize<GsxMessageEnvelop>(jsonString);
                message.JsonData = JsonNode.Parse(jsonString);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return message;
        }

        public virtual GsxMessageHello ParseHello()
        {
            GsxMessageHello result = new();
            try
            {
                result = JsonData.Deserialize<GsxMessageHello>();
                result.JsonData = JsonData;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual GsxMessagePatch ParsePatch()
        {
            GsxMessagePatch result = new();
            try
            {
                result = new(v, type, ts, JsonData);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual GsxMessageResult ParseResult()
        {
            GsxMessageResult result = new();
            try
            {
                result = JsonData.Deserialize<GsxMessageResult>();
                result.JsonData = JsonData;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual GsxMessageEvent ParseEvent()
        {
            GsxMessageEvent @event = new();
            try
            {
                @event = new(v, type, ts, JsonData);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return @event;
        }

        public override string ToString()
        {
            return JsonData?.ToJsonString() ?? "";
        }

        public virtual bool IsType(GsxMessageType type)
        {
            return GetMessageType() == type;
        }

        public virtual GsxMessageType GetMessageType()
        {
            if (Enum.TryParse(type, out GsxMessageType result))
                return result;
            else
                return GsxMessageType.unknown;
        }

        public virtual bool HasKey(GsxPayloadName payload)
        {
            return HasKey(payload.ToString());
        }

        public virtual bool HasKey(string key)
        {
            return JsonData!?[key] != null;
        }

        public virtual bool HasKey(string key, JsonValueKind kind)
        {
            return (JsonData!?[key] ?? JsonValue.Create(JsonValueKind.Null))?.GetValueKind() == kind;
        }

        public virtual T GetKey<T>(string key)
        {
            try
            {
                return JsonData[key].GetValue<T>() ?? default;
            }
            catch
            {
                return default;
            }
        }

        public virtual bool GetKey<T>(string key, out T value)
        {
            value = default;
            try
            {
                if (HasKey(key))
                {
                    value = JsonData[key].GetValue<T>();
                    return true;
                }
            }
            catch { }

            return false;
        }

        public virtual bool GetKey(GsxPayloadName payload, out JsonNode node)
        {
            return GetKey(payload.ToString(), out node);
        }

        public virtual bool GetKey(string key, out JsonNode node)
        {
            node = JsonValue.Create(JsonValueKind.Null);
            try
            {
                node = JsonData!?[key];
                return JsonData!?[key]!?.GetValueKind() != JsonValueKind.Null;
            }
            catch
            {
                return false;
            }
        }

        public virtual bool GetKey(string key, JsonValueKind kind, out JsonNode node)
        {
            node = JsonValue.Create(JsonValueKind.Null);
            try
            {
                node = JsonData!?[key];
                return JsonData!?[key]!?.GetValueKind() == kind;
            }
            catch
            {
                return false;
            }
        }
    }
#pragma warning restore IDE1006
}
