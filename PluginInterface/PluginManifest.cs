using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface
{
    public class PluginManifest
    {
        public virtual string Id { get; set; }
        public virtual PluginType Type { get; set; }
        public virtual PluginStartMode StartMode { get; set; } = PluginStartMode.WaitConnected;
        [JsonIgnore]
        public virtual string Directory { get; set; }
        public virtual string Filename { get; set; }
        public virtual string ChannelFile { get; set; }
        public virtual List<string> AircraftProfileEntries { get; set; }
        public virtual string Aircraft { get; set; }
        public virtual string Author { get; set; }
        public virtual string Url { get; set; }
        public virtual Version VersionPlugin { get; set; }
        [JsonIgnore]
        public virtual string Version => VersionPlugin?.ToString(3);
        [JsonIgnore]
        public virtual bool HasUpdateAvail { get; set; } = false;
        public virtual Version VersionApp { get; set; }
        public virtual PluginCapabilities Capabilities { get; set; } = new();
        public virtual List<PluginSetting> Settings { get; set; } = [];
        public virtual List<string> HideGenericSettings { get; set; } = [];
        public virtual string InstallUsageNotes { get; set; }
        public virtual JsonElement PluginDefaultProfile { get; set; }

        public static PluginManifest ReadManifest(string manifestFile)
        {
            var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestFile));
            manifest.Directory = new DirectoryInfo(Path.GetDirectoryName(manifestFile)).Name;
            return manifest;
        }

        public override string ToString()
        {
            return Id;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PluginManifest manifest)
                return Id.Equals(manifest.Id);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
