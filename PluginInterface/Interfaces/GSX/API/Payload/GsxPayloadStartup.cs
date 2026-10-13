using System.Text.Json.Nodes;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadStartup
    {
        public virtual bool active { get; set; } = true;
        public virtual long sid { get; set; } = 0;
        public virtual JsonArray bars { get; set; } = [];
    }

#pragma warning restore IDE1006
}
