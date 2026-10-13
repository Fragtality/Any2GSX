using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Messages
{
#pragma warning disable IDE1006

    public class GsxMessageResult : GsxMessageEnvelop
    {
        public virtual bool ok { get; set; } = false;
        [JsonIgnore]
        public virtual bool HasId => !string.IsNullOrWhiteSpace(id);
        public virtual string id { get; set; } = "";
        public virtual GsxMessageResultError error { get; set; } = null;
        [JsonIgnore]
        public virtual bool HasError => !string.IsNullOrWhiteSpace(error?.code);
        public virtual JsonNode payload { get; set; } = null;
        [JsonIgnore]
        public virtual bool HasPayload => payload != null && payload.GetValueKind() != JsonValueKind.Null;

        public virtual bool MatchCode(string code)
        {
            return error?.code?.Equals(code, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        public static GsxMessageResult Create(bool ok, string id, string code = null)
        {
            GsxMessageResult result = new()
            {
                ok = ok,
                id = id
            };

            if (!string.IsNullOrWhiteSpace(code))
                result.error = new()
                {
                    code = code
                };

            return result;
        }
    }

    public class GsxMessageResultError
    {
        public virtual string code { get; set; } = "";
        public virtual string message { get; set; } = "";
    }

#pragma warning restore IDE1006
}
