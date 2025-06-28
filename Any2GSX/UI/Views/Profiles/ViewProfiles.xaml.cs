using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ViewProfiles : UserControl, IView
    {
        protected virtual ModelProfiles ViewModel { get; }
        protected virtual ViewModelSelector<SettingProfile, SettingProfile> ViewModelSelector => ViewModel.ViewModelSelector;

        public ViewProfiles()
        {
            InitializeComponent();

            ViewModel = new(AppService.Instance, SelectorProfiles);
            this.DataContext = ViewModel;

            InputType.ItemsSource = ModelProfiles.MatchTypes;
            RefreshPluginList();
            RefreshChannelList();

            ViewModelSelector.BindTextElement(InputName, nameof(SettingProfile.Name));
            ViewModelSelector.BindMember(InputType, nameof(SettingProfile.MatchType), null, ProfileMatchType.Default);
            ViewModelSelector.BindTextElement(InputMatchString, nameof(SettingProfile.MatchString));
            ViewModelSelector.BindMember(InputPlugin, nameof(SettingProfile.PluginId), null, SettingProfile.GenericId);
            ViewModelSelector.BindMember(InputChannel, nameof(SettingProfile.ChannelFileId), null, SettingProfile.GenericId);
            ViewModelSelector.BindMember(CheckboxFeatureGSX, nameof(SettingProfile.RunAutomationService), null, false);
            ViewModelSelector.BindMember(CheckboxFeatureVolume, nameof(SettingProfile.RunAudioService), null, false);
            ViewModelSelector.BindMember(CheckboxFeaturePilotsdeck, nameof(SettingProfile.PilotsDeckIntegration), null, false);

            ButtonAdd.Command = ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, GetItem, IsItemValid);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputName);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputType);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputMatchString);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputPlugin);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputChannel);
            ViewModelSelector.AddUpdateCommand.Subscribe(CheckboxFeatureGSX);
            ViewModelSelector.AddUpdateCommand.Subscribe(CheckboxFeatureVolume);
            ViewModelSelector.AddUpdateCommand.Subscribe(CheckboxFeaturePilotsdeck);
            ViewModelSelector.AddUpdateCommand.Executed += () => AppService.Instance?.Config?.NotifyPropertyChanged(nameof(Config.CurrentProfile));

            ButtonRemove.Command = ViewModelSelector.BindRemoveButton(ButtonRemove, ViewModel.IsSelectionNonDefault);
            ViewModelSelector.RemoveCommand.Subscribe(InputName);
            ViewModelSelector.RemoveCommand.Subscribe(InputType);
            ViewModelSelector.RemoveCommand.Subscribe(InputMatchString);
            ViewModelSelector.RemoveCommand.Subscribe(InputPlugin);
            ViewModelSelector.RemoveCommand.Subscribe(InputChannel);
            ViewModelSelector.RemoveCommand.Subscribe(CheckboxFeatureGSX);
            ViewModelSelector.RemoveCommand.Subscribe(CheckboxFeatureVolume);
            ViewModelSelector.RemoveCommand.Subscribe(CheckboxFeaturePilotsdeck);
            ViewModelSelector.RemoveCommand.Executed += () => ViewModel.CheckActiveProfile();
            ViewModelSelector.AskConfirmation = true;
            ViewModelSelector.ConfirmationFunc = () => MessageBox.Show("Delete the selected Profile?", "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        protected virtual SettingProfile GetItem()
        {
            try
            {
                return new SettingProfile() { Name = InputName.Text, MatchType = (ProfileMatchType)(InputType.SelectedValue ?? ProfileMatchType.Default), MatchString = InputMatchString.Text,
                                            PluginId = InputPlugin.SelectedValue?.ToString() ?? SettingProfile.GenericId, ChannelFileId = InputChannel.SelectedValue?.ToString() ?? "",
                                            RunAutomationService = CheckboxFeatureGSX?.IsChecked == true, RunAudioService = CheckboxFeatureVolume?.IsChecked == true, PilotsDeckIntegration = CheckboxFeaturePilotsdeck?.IsChecked == true};
            }
            catch
            {
                return default;
            }
        }

        public virtual bool IsItemValid()
        {
            bool isTypeDefault = InputType?.SelectedValue is ProfileMatchType type && type == ProfileMatchType.Default;
            bool baseCheck = (!string.IsNullOrWhiteSpace(InputName?.Text) && InputType?.SelectedValue is ProfileMatchType
                && !string.IsNullOrWhiteSpace(InputMatchString?.Text) && !string.IsNullOrWhiteSpace(InputPlugin?.SelectedValue?.ToString()) && !string.IsNullOrWhiteSpace(InputChannel?.SelectedValue?.ToString()))
                || (isTypeDefault);

            if (baseCheck && isTypeDefault)
                baseCheck = InputName.Text == SettingProfile.DefaultId;

            if (!baseCheck)
                return false;

            if (InputName?.Text?.Equals(ViewModelSelector?.SelectedItem?.Name, StringComparison.InvariantCultureIgnoreCase) == true)
                return true;
            else
                return ViewModelSelector?.ItemsSource?.Source?.Any(p => p.Name.Equals(InputName?.Text, StringComparison.InvariantCultureIgnoreCase)) == false;
        }

        protected virtual void RefreshPluginList()
        {
            List<string> plugins = [];
            plugins.Add(SettingProfile.GenericId);
            plugins.AddRange(ViewModel.AppService.PluginController.Plugins.Keys);
            InputPlugin.ItemsSource = plugins;
            ViewModel.NotifyPropertyChanged(nameof(SettingProfile.PluginId));
        }

        protected virtual void RefreshChannelList()
        {
            List<string> channels = [SettingProfile.GenericId];
            channels.AddRange(ViewModel.AppService.PluginController.Channels.Keys);
            InputChannel.ItemsSource = channels;
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
