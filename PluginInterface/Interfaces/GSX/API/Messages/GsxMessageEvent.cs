using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Messages
{
#pragma warning disable IDE1006
    public class GsxMessageEvent() : GsxMessageEnvelop
    {
        public virtual string topic { get; set; } = null;
        public virtual JsonNode model { get; set; } = null;
        [JsonIgnore]
        public virtual bool HasModel => model != null && model.GetValueKind() == JsonValueKind.Object;
        public virtual bool gsxRunning { get; set; } = false;
        [JsonIgnore]
        public virtual bool HasRunning => HasKey(nameof(gsxRunning));

        public GsxMessageEvent(int v, string type, ulong ts, JsonNode node) : this()
        {
            this.v = v;
            this.type = type;
            this.ts = ts;
            JsonData = node;
            topic = node!?[nameof(topic)]!?.ToString() ?? "";
            model = node!?[nameof(model)] ?? JsonValue.Create(JsonValueKind.Null);
        }
    }
#pragma warning restore IDE1006
}
