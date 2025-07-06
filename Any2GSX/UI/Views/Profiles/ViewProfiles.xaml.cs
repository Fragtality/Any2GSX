using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ViewProfiles : UserControl, IView
    {
        protected virtual ModelProfiles ViewModel { get; }
        protected virtual ViewModelSelector<SettingProfile, ModelProfileItem> ViewProfileSelector { get; }
        protected virtual ViewModelSelector<ProfileMatching, ProfileMatching> ViewMatchingSelector { get; }
        protected virtual bool IsSelectionChanging { get; set; } = false;

        public ViewProfiles()
        {
            InitializeComponent();

            ViewModel = new(AppService.Instance, SelectorProfiles);
            this.DataContext = ViewModel;

            ViewProfileSelector = new(SelectorProfiles, ViewModel.ProfileCollection, AppWindow.IconLoader);
            ViewMatchingSelector = new(SelectorMatches, ViewModel.MatchingCollection, AppWindow.IconLoader);

            RefreshPluginList();
            RefreshChannelList();

            ViewProfileSelector.BindTextElement(InputName, nameof(ModelProfileItem.Name), "", null, true);
            InputName.KeyUp += (_, e) => OnProfileNameChange(Sys.IsEnter(e));
            InputName.LostFocus += (_, e) => OnProfileNameChange(true);
            InputName.LostKeyboardFocus += (_, e) => OnProfileNameChange(true);

            ViewProfileSelector.BindMember(SelectorPlugin, nameof(ModelProfileItem.PluginId), null, SettingProfile.GenericId);
            SelectorPlugin.SelectionChanged += (_, _) =>
            {
                if (ViewProfileSelector.HasSelection && SelectorPlugin?.SelectedValue is string plugin && !string.IsNullOrWhiteSpace(plugin))
                {
                    ViewProfileSelector.SelectedDisplayItem.InhibitConfigSave = IsSelectionChanging;
                    ViewProfileSelector.SelectedDisplayItem.PluginId = plugin;
                    ViewProfileSelector.SelectedDisplayItem.InhibitConfigSave = false;
                }
            };

            ViewProfileSelector.BindMember(SelectorChannel, nameof(ModelProfileItem.ChannelFileId), null, SettingProfile.GenericId);
            SelectorChannel.SelectionChanged += (_, _) =>
            {
                if (ViewProfileSelector.HasSelection && SelectorChannel?.SelectedValue is string channel && !string.IsNullOrWhiteSpace(channel))
                {
                    ViewProfileSelector.SelectedDisplayItem.InhibitConfigSave = IsSelectionChanging;
                    ViewProfileSelector.SelectedDisplayItem.ChannelFileId = channel;
                    ViewProfileSelector.SelectedDisplayItem.InhibitConfigSave = false;
                    IsSelectionChanging = false;
                }
            };

            ViewProfileSelector.BindMember(CheckboxFeatureGSX, nameof(ModelProfileItem.RunAutomationService), null, false);
            CheckboxFeatureGSX.Click += (_, _) =>
            {
                if (ViewProfileSelector.HasSelection && CheckboxFeatureGSX?.IsChecked is bool isChecked)
                    ViewProfileSelector.SelectedDisplayItem.RunAutomationService = isChecked;
            };

            ViewProfileSelector.BindMember(CheckboxFeatureVolume, nameof(ModelProfileItem.RunAudioService), null, false);
            CheckboxFeatureVolume.Click += (_, _) =>
            {
                if (ViewProfileSelector.HasSelection && CheckboxFeatureVolume?.IsChecked is bool isChecked)
                    ViewProfileSelector.SelectedDisplayItem.RunAudioService = isChecked;
            };

            ViewProfileSelector.BindMember(CheckboxFeaturePilotsdeck, nameof(ModelProfileItem.PilotsDeckIntegration), null, false);
            CheckboxFeaturePilotsdeck.Click += (_, _) =>
            {
                if (ViewProfileSelector.HasSelection && CheckboxFeaturePilotsdeck?.IsChecked is bool isChecked)
                    ViewProfileSelector.SelectedDisplayItem.PilotsDeckIntegration = isChecked;
            };

            ViewProfileSelector.PropertyChanged += ProfileSelectorPropertyChanged;

            ViewProfileSelector.BindRemoveButton(ButtonRemoveProfile, ViewModel.IsSelectionNonDefault);
            ViewProfileSelector.RemoveCommand.Executed += () => ViewModel.CheckActiveProfile();
            ViewProfileSelector.AskConfirmation = true;
            ViewProfileSelector.ConfirmationFunc = () => MessageBox.Show("Delete the selected Profile?", "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;


            ViewMatchingSelector.BindMember(SelectorMatchData, nameof(ProfileMatching.MatchData));
            ViewMatchingSelector.BindMember(SelectorMatchOperation, nameof(ProfileMatching.MatchOperation));
            ViewMatchingSelector.BindTextElement(InputMatchString, nameof(ProfileMatching.MatchString), "", null, true);

            ViewMatchingSelector.ClearInputs += () =>
            {
                ViewModel.InhibitConfigSave = true;
                SelectorMatchData.SelectedIndex = 0;
                SelectorMatchOperation.SelectedIndex = 0;
                ViewModel.InhibitConfigSave = false;
            };

            ViewMatchingSelector.BindAddUpdateButton(ButtonAddMatch, ImageAddMatching, GetMatching).Executed += () => ViewModel.SaveConfig();
            ViewMatchingSelector.AddUpdateCommand.Subscribe(SelectorMatchData);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(SelectorMatchOperation);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(InputMatchString);
            ViewMatchingSelector.BindRemoveButton(ButtonRemoveMatch).Executed += () => ViewModel.SaveConfig();
        }

        protected virtual void ProfileSelectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ViewProfileSelector.SelectedItem))
            {
                ViewModel.InhibitConfigSave = true;
                IsSelectionChanging = true;
                ViewModel.ProfileSelectionChanged(ViewProfileSelector.SelectedItem);
                ViewMatchingSelector.ClearSelection();
                ViewModel.InhibitConfigSave = false;
            }
        }

        protected virtual void OnProfileNameChange(bool isEnter)
        {
            if (isEnter && ViewProfileSelector.HasSelection && !string.IsNullOrWhiteSpace(InputName?.Text) && !InputName.Text.Equals(ViewProfileSelector.SelectedItem.Name))
            {
                ViewProfileSelector.SelectedDisplayItem.Name = InputName.Text;
                int index = ViewProfileSelector.SelectedIndex;
                ViewModel.InhibitConfigSave = true;
                ViewModel.ProfileCollection.NotifyCollectionChanged();
                ViewProfileSelector.SetSelectedIndex(index);
                ViewModel.InhibitConfigSave = false;
            }
        }

        protected virtual ProfileMatching GetMatching()
        {
            try
            {
                return new ProfileMatching((SelectorMatchData?.SelectedValue is MatchData matchData ? matchData : MatchData.SimObject),
                                           (SelectorMatchOperation?.SelectedValue is MatchOperation matchOp ? matchOp : null),
                                           InputMatchString?.Text);
            }
            catch
            {
                return default;
            }
        }

        protected virtual void RefreshPluginList()
        {
            List<string> plugins = [];
            plugins.Add(SettingProfile.GenericId);
            plugins.AddRange(ViewModel.AppService.PluginController.Plugins.Keys);
            SelectorPlugin.ItemsSource = plugins;
            ViewModel.NotifyPropertyChanged(nameof(SettingProfile.PluginId));
        }

        protected virtual void RefreshChannelList()
        {
            List<string> channels = [SettingProfile.GenericId];
            channels.AddRange(ViewModel.AppService.PluginController.Channels.Keys);
            SelectorChannel.ItemsSource = channels;
            ViewModel.NotifyPropertyChanged(nameof(SettingProfile.ChannelFileId));
        }

        public virtual void Start()
        {
            RefreshPluginList();
            RefreshChannelList();
            SelectorProfiles.SelectedItem = AppService.Instance?.Config?.CurrentProfile;
            ViewModel.Start();
        }

        public virtual void Stop()
        {
            ViewModel?.Stop();
        }
    }
}
