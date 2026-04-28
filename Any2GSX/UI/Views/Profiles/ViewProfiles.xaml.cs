using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ViewProfiles : UserControl, IView
    {
        protected virtual ModelProfiles ViewModel { get; }
        protected virtual ViewModelSelector<SettingProfile, ModelProfileItem> ViewProfileSelector { get; }
        protected virtual ViewModelSelector<ProfileMatching, ProfileMatching> ViewMatchingSelector { get; }

        public ViewProfiles()
        {
            InitializeComponent();

            ViewModel = new(AppService.Instance, SelectorProfiles, SelectorMatches);
            this.DataContext = ViewModel;

            ViewProfileSelector = ViewModel.ViewProfileSelector;
            ViewProfileSelector.BindAddUpdateButton(ButtonAddProfile, ImageAddProfile, GetProfile).Executed += OnAddExecuted;
            ViewProfileSelector.BindTextElement(InputName, nameof(ModelProfileItem.Name), "", null, true);
            InputName.UpdateSelectorOnLostFocus(ViewProfileSelector);

            ViewProfileSelector.BindMember(SelectorPlugin, nameof(ModelProfileItem.PluginId), null, SettingProfile.GenericId);
            SelectorPlugin.SelectionChanged += (_, _) =>
            {
                if (SelectorPlugin?.SelectedValue is string plugin)
                    ViewModel.SetPlugin(plugin);
            };

            ViewProfileSelector.BindMember(SelectorChannel, nameof(ModelProfileItem.ChannelFileId), null, SettingProfile.GenericId);
            SelectorChannel.SelectionChanged += (_, _) =>
            {
                if (SelectorChannel?.SelectedValue is string channel)
                    ViewModel.SetChannel(channel);
            };

            ViewProfileSelector.BindMember(CheckboxFeatureGSX, nameof(ModelProfileItem.RunAutomationService), null, false);
            CheckboxFeatureGSX.Click += (_, _) =>
            {
                ViewModel.SelectedModel.SetFeature(CheckboxFeatureGSX?.IsChecked, nameof(ModelProfileItem.RunAutomationService));
            };

            ViewProfileSelector.BindMember(CheckboxFeatureVolume, nameof(ModelProfileItem.RunAudioService), null, false);
            CheckboxFeatureVolume.Click += (_, _) =>
            {
                ViewModel.SelectedModel.SetFeature(CheckboxFeatureVolume?.IsChecked, nameof(ModelProfileItem.RunAudioService));
            };

            ViewProfileSelector.BindMember(CheckboxFeaturePilotsdeck, nameof(ModelProfileItem.PilotsDeckIntegration), null, false);
            CheckboxFeaturePilotsdeck.Click += (_, _) =>
            {
                ViewModel.SelectedModel.SetFeature(CheckboxFeaturePilotsdeck?.IsChecked, nameof(ModelProfileItem.PilotsDeckIntegration));
            };

            ViewMatchingSelector = ViewModel.ViewMatchingSelector;
            ViewMatchingSelector.BindMember(SelectorMatchData, nameof(ProfileMatching.MatchData));
            ViewMatchingSelector.BindMember(SelectorMatchOperation, nameof(ProfileMatching.MatchOperation));
            ViewMatchingSelector.BindTextElement(InputMatchString, nameof(ProfileMatching.MatchString), "", null, true);

            ViewMatchingSelector.BindAddUpdateButton(ButtonAddMatch, ImageAddMatching, GetMatching);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(SelectorMatchData);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(SelectorMatchOperation);
            ViewMatchingSelector.AddUpdateCommand.Subscribe(InputMatchString);
            ViewMatchingSelector.BindRemoveButton(ButtonRemoveMatch);

            ViewModel.SubscribeProperty(nameof(ViewModel.IsSessionRunning), OnSessionChange);
            ViewModel.SubscribeProperty(nameof(ViewModel.IsSessionStopped), OnSessionChange);

            ViewProfileSelector.BindRemoveButton(ButtonRemoveProfile, () => ViewModel.IsEditAllowed).Executed += () => ViewModel.OnProfileRemoved();
            ViewProfileSelector.AskConfirmation = true;
            ViewProfileSelector.ConfirmationFunc = () => MessageBox.Show(Any2GSX.Instance.AppWindow, "Delete the selected Profile?", "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        protected virtual void OnAddExecuted()
        {
            if (ViewModel.LastProfile == null && ViewModel.SelectedProfile == null)
            {
                var query = ViewModel.ProfileCollection.Source.Where((p) => p.Name.Equals(InputName?.Text, StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                    ViewProfileSelector.SelectedItem = query.First();
            }
        }

        protected virtual void OnSessionChange()
        {
            if (ViewModel.IsSessionRunning)
                ButtonSetProfile.Foreground = Brushes.OrangeRed;
            else
                ButtonSetProfile.Foreground = (Brush)Application.Current.FindResource(SystemColors.ControlTextBrushKey);
        }

        protected virtual SettingProfile GetProfile()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputName?.Text))
                    return new SettingProfile()
                    {
                        Name = InputName?.Text,
                        PluginId = SelectorPlugin.SelectedValue as string,
                        ChannelFileId = SelectorChannel.SelectedValue as string,
                        RunAutomationService = CheckboxFeatureGSX?.IsChecked == true,
                        RunAudioService = CheckboxFeatureVolume?.IsChecked == true,
                        PilotsDeckIntegration = CheckboxFeaturePilotsdeck?.IsChecked == true
                    };
                else
                    return null;
            }
            catch
            {
                return null;
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

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
