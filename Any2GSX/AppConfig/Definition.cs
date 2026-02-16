using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.AppConfig;
using System.IO;

namespace Any2GSX.AppConfig
{
    public class Definition : ProductDefinitionBase, IProductDefinition
    {
        public override int BuildConfigVersion { get; } = 15;
        public override string ProductName => "Any2GSX";
        public override string ProductExePath => Path.Join(Path.Join(ProductPath, "bin"), ProductExe);
        public virtual string RepoDistUrl => $"https://github.com/Fragtality/{ProductName}-Plugins/raw/refs/heads/master/dist";
        public virtual string RepoDistUrlPlugins => $"{RepoDistUrl}/plugins";
        public virtual string RepoDistUrlChannels => $"{RepoDistUrl}/channel";
        public virtual string RepoDistUrlProfiles => $"{RepoDistUrl}/profiles";
        public virtual string RepoFilePlugins => "plugin-repo.json";
        public virtual string RepoFileChannels => "channel-repo.json";
        public virtual string RepoFileProfiles => "profile-repo.json";
        public virtual string RepoDistUrlPluginFile => $"{RepoDistUrlPlugins}/{RepoFilePlugins}";
        public virtual string RepoDistUrlChannelFile => $"{RepoDistUrlChannels}/{RepoFileChannels}";
        public virtual string RepoDistUrlProfileFile => $"{RepoDistUrlProfiles}/{RepoFileProfiles}";
        public virtual string PluginFolderName => "plugins";
        public virtual string PluginFolder => Path.Join(ProductPath, PluginFolderName);
        public virtual string PluginManifest => "manifest.json";
        public virtual string ChannelFolderName => "audio-channel";
        public virtual string ChannelFolder => Path.Join(ProductPath, ChannelFolderName);
        public override bool RequireSimRunning => false;
        public override bool WaitForSim => true; 
        public override bool SingleInstance => true;
        public override bool ProductVersionCheckDev => true;
        public override bool MainWindowShowOnStartup => AppService.Instance?.Config?.OpenAppWindowOnStart == true || AppService.Instance?.Config?.ForceOpen == true;
    }
}
