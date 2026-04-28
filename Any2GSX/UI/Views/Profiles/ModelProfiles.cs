using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Controls.Primitives;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfiles : ModelBase<Config>
    {
        public virtual ViewModelSelector<SettingProfile, ModelProfileItem> ViewProfileSelector { get; }
        public virtual ModelProfileItem SelectedModel => ViewProfileSelector?.SelectedDisplayItem;
        public virtual SettingProfile SelectedProfile => ViewProfileSelector?.SelectedItem;
        public virtual SettingProfile LastProfile { get; protected set; } = null;
        public virtual ModelProfileCollection ProfileCollection { get; }
        public virtual ViewModelCollection<string, string> PluginCollection { get; }
        public virtual ViewModelCollection<string, string> ChannelCollection { get; }
        public virtual ModelMatchingCollection MatchingCollection { get; }
        public virtual ViewModelSelector<ProfileMatching, ProfileMatching> ViewMatchingSelector { get; }
        public static Dictionary<MatchData, string> MatchDataTexts => ProfileMatching.MatchDataTexts;
        public static Dictionary<MatchOperation, string> MatchOperationTexts => ProfileMatching.MatchOperationTexts;
        [ObservableProperty]
        public virtual partial bool IsActivateVisible { get; set; } = false;
        [ObservableProperty]
        public virtual partial bool IsAddVisible { get; set; } = true;
        [ObservableProperty]
        public virtual partial bool IsEditAllowed { get; set; } = false;
        public virtual CommandWrapper RemoveProfileCommand => ViewProfileSelector.RemoveCommand;
        protected virtual bool FirstLoad { get; set; } = true;

        public ModelProfiles(AppService source, Selector profileSelector, Selector matchingSelector) : base(source.Config, source)
        {
            ProfileCollection = [];
            ViewProfileSelector = new(profileSelector, ProfileCollection, AppWindow.IconLoader)
            {
                ClearOnAddUpdate = false
            };
            ViewProfileSelector.OnSelectionChanged += OnProfileSelectionChanged;
            ViewProfileSelector.ClearInputs += OnProfileSelectionCleared;

            PluginCollection = new(GetPluginList(), (i) => i);
            ChannelCollection = new(GetChannelList(), (i) => i);

            MatchingCollection = new(source?.SettingProfile);
            ViewMatchingSelector = new(matchingSelector, MatchingCollection, AppWindow.IconLoader);

            AppService.ProfileChanged += OnAppProfileChanged;
            AppService.ProfileCollectionChanged += OnAppProfileCollectionChanged;

            AppService.PluginController.PluginsChanged += OnPluginsChanged;
            AppService.PluginController.ChannelsChanged += OnChannelsChanged;
        }

        protected override void InitializeModel()
        {
            InitializeMessageService();
        }

        protected virtual void OnAppProfileChanged(SettingProfile settingProfile)
        {
            if (!settingProfile.IsDefault || !FirstLoad)
                ViewProfileSelector.SelectedItem = settingProfile;
            else
                FirstLoad = false;
            NotifySelectionChange();
        }

        public virtual void OnProfileRemoved()
        {
            LastProfile = null;
        }

        protected virtual void OnAppProfileCollectionChanged()
        {
            ProfileCollection.NotifyCollectionChanged();
        }

        protected virtual void OnProfileSelectionChanged()
        {
            if (SelectedProfile != null)
                LastProfile = SelectedProfile;
            else if (SelectedProfile == null && LastProfile != null)
                ViewProfileSelector.SelectedItem = LastProfile;

            if (ViewProfileSelector.SelectedIndex != -1)
                IsAddVisible = false;
            else
                IsAddVisible = true;

            if (ViewProfileSelector?.SelectedItem != null)
            {
                MatchingCollection.ChangeProfile(ViewProfileSelector.SelectedItem);
            }
            else
            {
                MatchingCollection.Clear();
                ViewMatchingSelector.ClearSelection();
            }

            NotifySelectionChange();
        }

        protected virtual void OnProfileSelectionCleared()
        {
            LastProfile = null;
        }

        protected virtual void OnPluginsChanged()
        {
            var list = PluginCollection.Source as List<string>;
            list.Clear();
            list.AddRange(GetPluginList());
            PluginCollection.NotifyCollectionChanged();
        }

        protected virtual List<string> GetPluginList()
        {
            List<string> plugins = [];
            plugins.Add(SettingProfile.GenericId);
            plugins.AddRange(AppService.PluginController.Plugins.Keys);
            return plugins;
        }

        public virtual void SetPlugin(string plugin)
        {
            if (SelectedProfile != null && !string.IsNullOrWhiteSpace(plugin) && SelectedModel.PluginId != plugin)
                SelectedModel.PluginId = plugin;
        }

        protected virtual void OnChannelsChanged()
        {
            var list = ChannelCollection.Source as List<string>;
            list.Clear();
            list.AddRange(GetChannelList());
            ChannelCollection.NotifyCollectionChanged();
        }

        protected virtual List<string> GetChannelList()
        {
            List<string> channels = [SettingProfile.GenericId];
            channels.AddRange(AppService.PluginController.Channels.Keys);
            return channels;
        }

        public virtual void SetChannel(string channel)
        {
            if (SelectedProfile != null && !string.IsNullOrWhiteSpace(channel) && SelectedModel.ChannelFileId != channel)
                SelectedModel.ChannelFileId = channel;
        }

        protected virtual void NotifySelectionChange()
        {
            NotifyPropertyChanged(string.Empty);
            IsActivateVisible = SelectedProfile != null && SelectedModel?.IsActive == false;
            IsEditAllowed = SelectedProfile != null && SelectedModel?.IsEditAllowed == true;
            NotifyPropertyChanged(nameof(IsAddVisible));
            NotifyPropertyChanged(nameof(IsActivateVisible));
            NotifyPropertyChanged(nameof(IsEditAllowed));
            NotifyPropertyChanged(nameof(SelectedModel));
            SelectedModel.NotifyPropertyChanged(string.Empty);
            NotifyPropertyChanged(nameof(SelectedProfile));
        }

        [RelayCommand]
        protected virtual void ImportProfile()
        {
            try
            {
                Logger.Debug($"Importing Profile from Clipboard ...");
                string json = ClipboardHelper.GetClipboard();
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warning($"Clipboard Data is empty / wrong Type");
                    return;
                }

                Config.ImportProfile(json);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        protected virtual void ExportProfile()
        {
            try
            {
                Logger.Debug($"Exporting Profile to Clipboard ...");
                if (SelectedProfile == null)
                {
                    Logger.Warning($"Selected Profile is Null");
                    return;
                }

                string json = JsonSerializer.Serialize(SelectedProfile);
                ClipboardHelper.SetClipboard(json);
                Logger.Information($"Copied Profile '{SelectedProfile.Name}' to Clipboard");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        [RelayCommand]
        protected virtual void CloneProfile()
        {
            try
            {
                Logger.Debug($"Cloning Profile ...");
                if (SelectedProfile == null)
                {
                    Logger.Warning($"Selected Profile is Null");
                    return;
                }

                string json = JsonSerializer.Serialize(SelectedProfile);
                var clone = JsonSerializer.Deserialize<SettingProfile>(json);
                clone.Name = $"Clone of {SelectedProfile.Name}";
                if (SelectedProfile.IsDefault)
                {
                    Logger.Debug($"Create Clone of Default Profile");
                    clone.IsReadOnly = false;
                }

                if (Config.SettingProfiles.Any(p => p.Name.Equals(clone.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.Warning($"The Profile '{clone.Name}' is already configured");
                    return;
                }

                AppService.Instance.AddSettingProfile(clone);
                Logger.Information($"Cloned Profile '{SelectedProfile.Name}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
