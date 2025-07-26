using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.Plugins;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppLogger;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class ViewPlugins : UserControl, IView
    {
        protected virtual ViewModelSelector<PluginManifest, PluginManifest> ViewModelInstalledPlugins { get; }
        protected virtual ViewModelSelector<AircraftChannels, AircraftChannels> ViewModelInstalledChannels { get; }
        protected virtual ViewModelSelector<PluginManifest, PluginManifest> ViewModelRepoPlugins { get; }
        protected virtual ViewModelSelector<AircraftChannels, AircraftChannels> ViewModelRepoChannels { get; }
        protected virtual ViewModelSelector<ProfileManifest, ProfileManifest> ViewModelRepoProfiles { get; }
        protected virtual CommandWrapper CommandInstallPluginFromRepo { get; }
        protected virtual CommandWrapper CommandInstallChannelFromRepo { get; }
        protected virtual CommandWrapper CommandImportProfileFromRepo { get; }
        protected virtual CommandWrapper CommandDeletePlugin { get; }
        protected virtual CommandWrapper CommandDeleteChannel { get; }
        protected virtual AppWindow AppWindow { get; }

        protected virtual ModelPlugins ViewModel { get; }

        public ViewPlugins(AppWindow appWindow)
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
            AppWindow = appWindow;

            ViewModelInstalledPlugins = new(GridInstalledPlugin, ViewModel.ModelInstalledPlugins, AppWindow.IconLoader);
            ViewModelInstalledChannels = new(GridInstalledChannels, ViewModel.ModelInstalledChannels, AppWindow.IconLoader);

            ViewModelRepoPlugins = new(GridOnlinePlugin, ViewModel.ModelRepoPlugins, AppWindow.IconLoader);
            ViewModelRepoChannels = new(GridOnlineChannels, ViewModel.ModelRepoChannels, AppWindow.IconLoader);
            ViewModelRepoProfiles = new(GridOnlineProfiles, ViewModel.ModelRepoProfiles, AppWindow.IconLoader);

            CommandInstallPluginFromRepo = new(async () => await InstallPluginSelected(), () => GridOnlinePlugin?.SelectedValue is PluginManifest);
            CommandInstallPluginFromRepo.Subscribe(GridOnlinePlugin);
            CommandInstallPluginFromRepo.Executed += () => AppService.Instance?.Config?.NotifyPropertyChanged(nameof(Config.SettingProfiles));
            ButtonInstallPluginFromRepo.Command = CommandInstallPluginFromRepo;

            CommandInstallChannelFromRepo = new(async () => await InstallChannelSelected(), () => GridOnlineChannels?.SelectedValue is AircraftChannels);
            CommandInstallChannelFromRepo.Subscribe(GridOnlineChannels);
            ButtonInstallChannelFromRepo.Command = CommandInstallChannelFromRepo;

            CommandImportProfileFromRepo = new(async () => await InstallProfileSelected(), () => GridOnlineProfiles?.SelectedValue is ProfileManifest);
            CommandImportProfileFromRepo.Subscribe(GridOnlineProfiles);
            CommandImportProfileFromRepo.Executed += () => AppService.Instance?.Config?.NotifyPropertyChanged(nameof(Config.SettingProfiles));
            ButtonImportProfileFromRepo.Command = CommandImportProfileFromRepo;

            CommandDeletePlugin = new(() => {
                if (GridInstalledPlugin?.SelectedItem is not PluginManifest manifest)
                    return;
                RemovePlugin(manifest);
            }, () => GridInstalledPlugin?.SelectedItem is PluginManifest);
            CommandDeletePlugin.Subscribe(GridInstalledPlugin);
            ButtonRemovePlugin.Command = CommandDeletePlugin;

            CommandDeleteChannel = new(() => {
                if (GridInstalledChannels?.SelectedItem is not AircraftChannels channel)
                    return;
                RemoveChannel(channel);
            }, () => GridInstalledChannels?.SelectedItem is AircraftChannels);
            CommandDeleteChannel.Subscribe(GridInstalledChannels);
            ButtonRemoveChannel.Command = CommandDeleteChannel;

            ButtonUpdateAll.Click += async (_, _) => await UpdateAll();
            _ = Refresh();
        }

        public virtual async void Start()
        {
            await Refresh();
        }

        public virtual void Stop()
        {

        }

        protected virtual async Task Refresh()
        {
            try
            {
                await ViewModel.PluginRepo.Refresh();
                ViewModel.ModelRepoPlugins.NotifyCollectionChanged();
                ViewModel.ModelRepoChannels.NotifyCollectionChanged();
                ViewModel.ModelRepoProfiles.NotifyCollectionChanged();
                AppService.Instance.PluginController.RefreshVersions(ViewModel.PluginRepo);
                ViewModel.ModelInstalledPlugins.NotifyCollectionChanged();
                ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                if (ViewModel.PluginRepo.HasUpdates())
                {
                    Any2GSX.Instance.NotifyIcon.SetIconUpdate();
                    AppWindow.SetPluginUpdateNotice();
                    ButtonUpdateAll.Visibility = Visibility.Visible;
                }
                else
                {
                    Any2GSX.Instance.NotifyIcon.SetIconNormal();
                    AppWindow.RemovePluginUpdateNotice();
                    ButtonUpdateAll.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        protected virtual async void ButtonInstallPluginFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button)
                    return;
                Logger.Debug($"Opening File Dialog (Plugin)");

                OpenFileDialog openFileDialog = new()
                {
                    Title = "Install Plugin ...",
                    Filter = $"Plugin Archive (Zip File (*.zip)|*.zip|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (await ViewModel.PluginRepo.InstallPluginFromFile(openFileDialog.FileName))
                    {
                        ViewModel.AppService.PluginController.Refresh();
                        ViewModel.ModelInstalledPlugins.NotifyCollectionChanged();
                        ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                    }
                    else
                        MessageBox.Show($"Failed to install Plugin", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error while installing Plugin: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual void ButtonInstallChannelFromFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button)
                    return;
                Logger.Debug($"Opening File Dialog (Channel)");

                OpenFileDialog openFileDialog = new()
                {
                    Title = "Install Channel Definition ...",
                    Filter = $"Channel Definition (json (*.json)|*.json|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (ViewModel.PluginRepo.InstallChannelFromFile(openFileDialog.FileName))
                    {
                        ViewModel.AppService.PluginController.RefreshChannels();
                        ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                    }
                    else
                        MessageBox.Show($"Failed to install Channel Definition", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                MessageBox.Show($"Error while installing Channel: {ex.Message}", ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual async Task InstallPluginSelected()
        {
            try
            {
                if (GridOnlinePlugin?.SelectedValue is PluginManifest manifest)
                {
                    var key = ViewModel.PluginRepo.Plugins.Where((kv) => kv.Value.Id == manifest.Id).FirstOrDefault().Key;
                    if (ViewModel.AppService.PluginController.LoadedPluginsBinary.Contains(key))
                    {
                        MessageBox.Show($"The Plugin DLL is already loaded!\r\nPlease close Any2GSX and install/update the Plugin before it was loaded.", "Already loaded", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (await ViewModel.PluginRepo.InstallPluginFromRepo(key))
                    {
                        ViewModel.AppService.PluginController.RefreshPlugins();
                        ViewModel.AppService.PluginController.RefreshChannels();
                        ViewModel.ModelInstalledPlugins.NotifyCollectionChanged();
                        ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                    }
                    else
                        MessageBox.Show($"Failed to install Plugin", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual async Task InstallChannelSelected()
        {
            try
            {
                if (GridOnlineChannels?.SelectedValue is AircraftChannels channels)
                {
                    var key = ViewModel.PluginRepo.Channels.Where((kv) => kv.Value.Id == channels.Id).FirstOrDefault().Key;
                    if (await ViewModel.PluginRepo.InstallChannelFromRepo(key))
                    {
                        ViewModel.AppService.PluginController.RefreshChannels();
                        ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                    }
                    else
                        MessageBox.Show($"Failed to install Channel Definition", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual async Task InstallProfileSelected()
        {
            try
            {
                if (GridOnlineProfiles?.SelectedValue is ProfileManifest selectedProfile)
                {
                    var query = ViewModel?.PluginRepo?.Profiles?.Where(p => p.Value.Name == selectedProfile.Name);
                    if (query.Any() == false)
                        return;
                    string key = query.First().Key;

                    if (await ViewModel.PluginRepo.ImportProfileFromRepo(key, selectedProfile))
                    {
                        ViewModel.AppService.PluginController.RefreshChannels();
                        ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                        ViewModel.Config.NotifyPropertyChanged(nameof(Config.SettingProfiles));
                    }
                    else
                        MessageBox.Show($"Failed to install Channel Definition", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void RemovePlugin(PluginManifest manifest)
        {
            if (manifest is null)
                return;

            if (MessageBox.Show($"Do you want to delete the Aircraft Plugin '{manifest.Id}'?", $"Remove Plugin '{manifest.Id}'", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (AppService.Instance.PluginController.DeleteInstalledPlugin(manifest))
            {
                ViewModel.AppService.PluginController.RefreshPlugins();
                ViewModel.ModelInstalledPlugins.NotifyCollectionChanged();
            }
        }

        protected virtual void RemoveChannel(AircraftChannels channel)
        {
            if (channel is null)
                return;

            if (MessageBox.Show($"Do you want to delete the Aircraft Channel Definition '{channel.Id}'?", $"Remove Channel '{channel.Id}'", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (AppService.Instance.PluginController.DeleteInstalledChannel(channel))
            {
                ViewModel.AppService.PluginController.RefreshChannels();
                ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
            }
        }

        protected virtual async Task UpdateAll()
        {
            try
            {
                var PluginController = AppService.Instance.PluginController;
                var updatedPlugins = PluginController.Plugins?.Values?.Where(p => p.HasUpdateAvail);
                foreach (var plugin in updatedPlugins)
                {
                    if (ViewModel.AppService.PluginController.LoadedPluginsBinary.Contains(plugin.Id))
                    {
                        MessageBox.Show($"The Plugin DLL is already loaded!\r\nPlease close Any2GSX and install/update the Plugin before it was loaded.", "Already loaded", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (await ViewModel.PluginRepo.InstallPluginFromRepo(ViewModel.PluginRepo.Plugins.Where((kv) => kv.Value.Id == plugin.Id).FirstOrDefault().Key) == false)
                        MessageBox.Show($"Failed to install Plugin", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                var updatedChannels = PluginController.Channels?.Values?.Where(p => p.HasUpdateAvail);
                foreach (var channel in updatedChannels)
                {
                    if (await ViewModel.PluginRepo.InstallChannelFromRepo(ViewModel.PluginRepo.Channels.Where((kv) => kv.Value.Id == channel.Id).FirstOrDefault().Key) == false)
                        MessageBox.Show($"Failed to install Channel Definition", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                ViewModel.AppService.PluginController.RefreshPlugins();
                ViewModel.AppService.PluginController.RefreshChannels();
                ViewModel.ModelInstalledPlugins.NotifyCollectionChanged();
                ViewModel.ModelInstalledChannels.NotifyCollectionChanged();
                await Refresh();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
