using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.AppConfig;
using System.IO;

namespace Any2GSX.AppConfig
{
    public class Definition : ProductDefinitionBase, IProductDefinition
    {
        public override int BuildConfigVersion { get; } = 19;
        public override string ProductName => "Any2GSX";
        public override string ProductExePath => Path.Join(Path.Join(ProductPath, "bin"), ProductExe);
        public virtual string RepoPlugins => $"{ProductName}-Plugins";
        public virtual string RepoDistPath => "dist";
        public virtual string RepoDistPathPlugins => $"{RepoDistPath}/plugins";
        public virtual string RepoDistPathChannels => $"{RepoDistPath}/channel";
        public virtual string RepoDistPathProfiles => $"{RepoDistPath}/profiles";
        public virtual string RepoFilePlugins => "plugin-repo.json";
        public virtual string RepoFileChannels => "channel-repo.json";
        public virtual string RepoFileProfiles => "profile-repo.json";
        public virtual string RepoDistPathReleaseInfo => $"{RepoDistPath}/release-info";
        public virtual string RepoDistPathPluginFile => $"{RepoDistPathPlugins}/{RepoFilePlugins}";
        public virtual string RepoDistPathChannelFile => $"{RepoDistPathChannels}/{RepoFileChannels}";
        public virtual string RepoDistPathProfileFile => $"{RepoDistPathProfiles}/{RepoFileProfiles}";
        public virtual string PluginFolderName => "plugins";
        public virtual string PluginFolder => Path.Join(ProductPath, PluginFolderName);
        public virtual string PluginManifest => "manifest.json";
        public virtual string ChannelFolderName => "audio-channel";
        public virtual string ChannelFolder => Path.Join(ProductPath, ChannelFolderName);
        public override bool RequireSimRunning => false;
        public override bool WaitForSim => true;
        public override bool SingleInstance => true;
        public override bool MainWindowShowOnStartup => AppService.Instance?.Config?.OpenAppWindowOnStart == true || AppService.Instance?.Config?.ForceOpen == true;
    }
}
