using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadMenu
    {
        public virtual string title { get; set; } = "";
        public virtual List<string> entries { get; set; } = [];
    }

#pragma warning restore IDE1006
}
