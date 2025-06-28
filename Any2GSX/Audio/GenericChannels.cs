using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;

namespace Any2GSX.Audio
{
    public class GenericChannels : AircraftChannels
    {
        public override string Id { get; set; } = SettingProfile.GenericId;
        public override string Aircraft { get; set; } = SettingProfile.GenericId;
        public override string Author { get; set; } = "Fragtality";
        public override Version VersionChannel { get; set; } = Config.Definition.ProductVersion;
        public override Version VersionApp { get; set; } = Config.Definition.ProductVersion;
        public override List<ChannelDefinition> ChannelDefinitions { get; set; } = new()
        {
            { new ChannelDefinition() { Name = "AUDIO PANEL", VolumeVariable = "AUDIO PANEL VOLUME", VolumeUnit = SimUnitType.Percent, MinValue = 0, MaxValue = 100 } },
            { new ChannelDefinition() { Name = "TACAN1", VolumeVariable = "TACAN VOLUME:1", VolumeUnit = SimUnitType.PercentOver100, MinValue = 0, MaxValue = 1 } },
            { new ChannelDefinition() { Name = "TACAN2", VolumeVariable = "TACAN VOLUME:2", VolumeUnit = SimUnitType.PercentOver100, MinValue = 0, MaxValue = 1 } },
            { new ChannelDefinition() { Name = "COM", VolumeVariable = "COM VOLUME", VolumeUnit = SimUnitType.Percent, MinValue = 0, MaxValue = 100 } },
            { new ChannelDefinition() { Name = "NAV", VolumeVariable = "NAV VOLUME", VolumeUnit = SimUnitType.Percent, MinValue = 0, MaxValue = 100 } },
            { new ChannelDefinition() { Name = "ADF", VolumeVariable = "ADF VOLUME", VolumeUnit = SimUnitType.PercentOver100, MinValue = 0, MaxValue = 1 } },         
        };
    }
}
