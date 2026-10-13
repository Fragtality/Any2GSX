using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadService
    {
        public static Dictionary<string, GsxServiceType> ServiceIdLookup { get; } = new()
        {
            { "Deboarding", GsxServiceType.Deboarding },
            { "Catering", GsxServiceType.Catering },
            { "Refueling", GsxServiceType.Refuel },
            { "Boarding", GsxServiceType.Boarding },
            { "Departure", GsxServiceType.Pushback },
            { "OperateJetways", GsxServiceType.Jetway },
            { "OperateStairs", GsxServiceType.Stairs },
            { "GPU", GsxServiceType.GPU },
            { "DeIce", GsxServiceType.Deice },
            { "Lavatory", GsxServiceType.Lavatory },
            { "Water", GsxServiceType.Water },
            { "Cleaning", GsxServiceType.Cleaning },
        };

        public static string ReverseLookup(GsxServiceType type)
        {
            return ServiceIdLookup.FirstOrDefault(kv => kv.Value == type).Key;
        }

        public virtual string id { get; set; }
        [JsonIgnore]
        public virtual GsxServiceType Type => ServiceIdLookup[id];
        public virtual GsxServiceType stateRaw { get; set; }
        public virtual bool canTrigger { get; set; }
        public virtual string @operator { get; set; } = "";
        public virtual string progressText { get; set; }
    }

#pragma warning restore IDE1006
}
