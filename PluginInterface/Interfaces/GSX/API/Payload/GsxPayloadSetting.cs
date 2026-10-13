using System.Text.Json;
using System.Text.Json.Nodes;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadSetting
    {
        public virtual string type { get; set; }
        public virtual string key { get; set; }
        public virtual JsonNode value { get; set; }
        public virtual double min { get; set; }
        public virtual double max { get; set; }
        public virtual double step { get; set; }
        public virtual string unit { get; set; }
        public virtual bool @float { get; set; }

        public dynamic GetValue()
        {
            if (value.GetValueKind() == JsonValueKind.String)
                return value.GetValue<string>();
            else if (value.GetValueKind() == JsonValueKind.Number)
            {
                if (@float)
                    return value.GetValue<double>();
                else
                    return (int)value;
            }
            else
                return null;
        }
    }

#pragma warning restore IDE1006
}
