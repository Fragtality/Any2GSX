using Any2GSX.PluginInterface.Interfaces.GSX.API.Payload;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Messages
{
#pragma warning disable IDE1006
    public class GsxMessagePatch() : GsxMessageEnvelop
    {
        public virtual string path { get; set; } = "";
        [JsonIgnore]
        public virtual GsxPayloadName PayloadPath => Enum.TryParse<GsxPayloadName>(path?.Replace("/", "") ?? "", out var type) ? type : GsxPayloadName.unknown;
        public virtual JsonNode value { get; set; } = JsonValue.Create(JsonValueKind.Null);
        [JsonIgnore]
        public virtual bool HasValue => value != null && value.GetValueKind() != JsonValueKind.Null;

        public GsxMessagePatch(int v, string type, ulong ts, JsonNode node) : this()
        {
            this.v = v;
            this.type = type;
            this.ts = ts;
            JsonData = node;
            path = node!?[nameof(path)]!?.ToString() ?? "";
            value = node!?[nameof(value)] ?? JsonValue.Create(JsonValueKind.Null);
        }
    }
#pragma warning restore IDE1006
}
