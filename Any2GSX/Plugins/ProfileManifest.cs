using System;
using System.Text.Json.Serialization;

namespace Any2GSX.Plugins
{
    public class ProfileManifest
    {
        public virtual string Name { get; set; }
        public virtual string Aircraft { get; set; }
        public virtual string Description { get; set; }
        public virtual string Author { get; set; }
        public virtual Version VersionProfile { get; set; }
        [JsonIgnore]
        public virtual string Version => VersionProfile?.ToString(3);
        public virtual Version VersionApp { get; set; }
    }
}
