using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Messages
{
#pragma warning disable IDE1006
    public class GsxMessageHello() : GsxMessageEnvelop
    {
        public virtual int protocol { get; set; } = 0;
        public virtual string engine { get; set; } = "";
        public virtual string sim { get; set; } = "";
        public virtual bool gsxRunning { get; set; } = false;
        public virtual bool authRequired { get; set; } = false;
        public virtual List<string> capabilities { get; set; } = [];
        [JsonIgnore]
        public virtual bool IsValid => protocol > 0 && !string.IsNullOrWhiteSpace(engine) && !string.IsNullOrWhiteSpace(sim) && capabilities?.Count > 0;
    }
#pragma warning restore IDE1006
}
