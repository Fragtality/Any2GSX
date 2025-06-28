using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.Plugins;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class ModelPlugins : ModelBase<Config>
    {
        public virtual ModelInstalledPlugins ModelInstalledPlugins { get; }
        public virtual ModelInstalledChannels ModelInstalledChannels { get; }
        public virtual ModelRepoPlugins ModelRepoPlugins { get; }
        public virtual ModelRepoChannels ModelRepoChannels { get; }
        public virtual ModelRepoProfiles ModelRepoProfiles { get; }
        public virtual PluginRepo PluginRepo { get; }
        public virtual RelayCommand<PluginManifest> ShowPluginDialogCommand { get; }
        public virtual RelayCommand<AircraftChannels> ShowChannelInstalledDialogCommand { get; }
        public virtual RelayCommand<AircraftChannels> ShowChannelOnlineDialogCommand { get; }
        public virtual RelayCommand<string> ShowDescriptionDialogCommand { get; }

        public ModelPlugins(AppService appService) : base(appService.Config, appService)
        {
            ModelInstalledPlugins = new(this);
            ModelInstalledChannels = new(this);
            PluginRepo = new();

            ModelRepoPlugins = new(this);
            ModelRepoChannels = new(this);
            ModelRepoProfiles = new(this);

            ShowPluginDialogCommand = new((manifest) => {
                var window = new PluginCapabilityDialog(manifest);
                window.ShowDialog();
            });

            ShowChannelInstalledDialogCommand = new((manifest) => {
                var window = new ShowInfoDialog(string.Join("\r\n", manifest.ChannelDefinitions.Select(c => c.GetInfoString())));
                window.Show();
            });

            ShowChannelOnlineDialogCommand = new((manifest) => {
                var window = new ShowInfoDialog(string.Join("\r\n", manifest.ChannelDefinitions.Select(c => c.GetInfoString())));
                window.Show();
            });

            ShowDescriptionDialogCommand = new((info) => {
                var window = new ShowInfoDialog(info);
                window.Show();
            });
        }

        protected override void InitializeModel()
        {
            
        }

        public virtual bool AutoInstallGsxProfiles { get => Source.AutoInstallGsxProfiles; set => SetModelValue<bool>(value); }
    }
}
