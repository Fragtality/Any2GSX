using Any2GSX.Audio;
using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Any2GSX.Aircraft
{
    public class AircraftChannels
    {
        public virtual string Id { get; set; }
        public virtual string Aircraft { get; set; }
        public virtual string Author { get; set; }
        public virtual Version VersionChannel { get; set; }
        [JsonIgnore]
        public virtual string Version => VersionChannel?.ToString(3);
        [JsonIgnore]
        public virtual bool HasUpdateAvail { get; set; } = false;
        public virtual Version VersionApp { get; set; }
        public virtual List<ChannelDefinition> ChannelDefinitions { get; set; } = [];

        public static AircraftChannels LoadChannelFile(string id)
        {
            AircraftChannels channel = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    string fileName = $"{id}.json";
                    string filePath = Path.Join(AppService.Instance.Definition.ChannelFolder, fileName);
                    if (File.Exists(filePath))
                        channel = JsonSerializer.Deserialize<AircraftChannels>(File.ReadAllText(filePath));
                    else
                        Logger.Warning($"No Channel Definition File for configured ID '{id}' found ({AppService.Instance.Definition.ChannelFolderName}\\{fileName})");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                channel = new();
            }

            return channel;
        }

        public static AircraftChannels Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<AircraftChannels>(json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        public static AircraftChannels DeserializeFile(string filePath)
        {
            try
            {
                return JsonSerializer.Deserialize<AircraftChannels>(File.ReadAllText(filePath));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }
    }
}
