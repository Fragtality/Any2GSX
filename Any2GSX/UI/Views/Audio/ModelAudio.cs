using Any2GSX.AppConfig;
using Any2GSX.Audio;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAudio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Any2GSX.UI.Views.Audio
{
    public partial class ModelAudio : ModelBase<SettingProfile>
    {
        public ICommand CommandDebugInfo { get; } = new RelayCommand(() => AppService.Instance.AudioController.DeviceManager.WriteDebugInformation());
        public override SettingProfile Source => AppService.Instance?.SettingProfile;
        public virtual Visibility ChannelVisibility => string.IsNullOrWhiteSpace(AppService.Instance?.SettingProfile?.ChannelFileId) || AppService.Instance?.SettingProfile?.ChannelFileId == SettingProfile.GenericId ? Visibility.Collapsed : Visibility.Visible;

        public ModelAudio(AppService appService) : base(appService.SettingProfile, appService)
        {
            ChannelCollection = new(this);

            AppMappingCollection = new(this);
            AppMappingCollection.CollectionChanged += (_, e) =>
            {
                SaveConfig();
                SwapEnabled = SettingProfile.AudioMappings.Count > 0;
                AudioController.ResetMappings = true;
            };

            BlacklistCollection = new(this);
            BlacklistCollection.CollectionChanged += (_, _) => SaveConfig();
        }

        protected override void InitializeModel()
        {
            this.PropertyChanged += OnSelfPropertyChanged;
            AudioController.DeviceManager.DevicesChanged += () => NotifyPropertyChanged(nameof(AudioDevices));
            AppService.ProfileChanged += OnProfileChanged;
            AppService.AudioController.OnChannelsChanged += () => OnProfileChanged(null);
        }

        protected virtual void OnProfileChanged(SettingProfile profile)
        {
            InhibitConfigSave = true;
            ChannelCollection.NotifyCollectionChanged();
            AppMappingCollection.NotifyCollectionChanged();
            BlacklistCollection.NotifyCollectionChanged();
            CurrentChannel = ChannelCollection?.Source?.FirstOrDefault() ?? "";
            NotifyPropertyChanged(nameof(ChannelVisibility));
            NotifyPropertyChanged(string.Empty);
            InhibitConfigSave = false;
            SwapEnabled = SettingProfile.AudioMappings.Count > 0;
        }

        protected virtual void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(CurrentChannel))
            {
                NotifyPropertyChanged(nameof(SetStartupVolume));
                NotifyPropertyChanged(nameof(StartupVolume));
                NotifyPropertyChanged(nameof(StartupUnmute));
            }
        }

        protected virtual void SetConfigValue<T>(T value, [CallerMemberName] string propertyName = null!)
        {
            if (!Config.IsPropertyType<T>(propertyName))
                return;

            OnPropertyChanging(propertyName);
            Config.SetPropertyValue<T>(propertyName, value);
            SaveConfig();
            OnPropertyChanged(propertyName);
        }

        [ObservableProperty]
        public partial bool SwapEnabled { get; set; } = true;

        [ObservableProperty]
        public partial string SwapName1 { get; set; } = "CPT";
        [ObservableProperty]
        public partial string SwapName2 { get; set; } = "FO";



        [RelayCommand]
        protected virtual void SwapChannels()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SwapName1) || string.IsNullOrWhiteSpace(SwapName2) || SwapName1 == SwapName2)
                    return;

                var channelNames = SettingProfile.GetAircraftChannels().ChannelDefinitions.Select(c => c.Name);
                List<string> swapped = [];
                string name;
                SwapEnabled = false;

                foreach (var mapping in SettingProfile.AudioMappings)
                {
                    if (mapping.Channel.Contains(SwapName1, StringComparison.InvariantCultureIgnoreCase))
                    {
                        name = mapping.Channel.Replace(SwapName1, SwapName2);
                        if (SwapChannel(channelNames, mapping, name))
                            swapped.Add(name);

                    }
                    else if (mapping.Channel.Contains(SwapName2, StringComparison.InvariantCultureIgnoreCase))
                    {
                        name = mapping.Channel.Replace(SwapName2, SwapName1);
                        if (SwapChannel(channelNames, mapping, name))
                            swapped.Add(name);
                    }
                }

                if (swapped.Count > 0)
                {
                    SaveConfig();
                    OnProfileChanged(SettingProfile);
                }
                else
                    SwapEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual bool SwapChannel(IEnumerable<string> channelNames, AudioMapping mapping, string name)
        {
            if (channelNames.Contains(name))
            {
                if (SettingProfile.AudioStartupVolumes.ContainsKey(mapping.Channel))
                {
                    if (SettingProfile.AudioStartupVolumes.TryGetValue(name, out double volume))
                    {
                        SettingProfile.AudioStartupVolumes.AddOrUpdate(name, SettingProfile.AudioStartupVolumes[mapping.Channel]);
                        SettingProfile.AudioStartupVolumes.AddOrUpdate(mapping.Channel, volume);
                    }
                    else
                    {
                        SettingProfile.AudioStartupVolumes.Add(name, SettingProfile.AudioStartupVolumes[mapping.Channel]);
                        SettingProfile.AudioStartupVolumes.Remove(mapping.Channel);
                    }
                }

                if (SettingProfile.AudioStartupUnmute.ContainsKey(mapping.Channel))
                {
                    if (SettingProfile.AudioStartupUnmute.TryGetValue(name, out bool mute))
                    {
                        SettingProfile.AudioStartupUnmute.Remove(name);
                        SettingProfile.AudioStartupUnmute.Add(name, SettingProfile.AudioStartupUnmute[mapping.Channel]);
                        SettingProfile.AudioStartupUnmute.Remove(mapping.Channel);
                        SettingProfile.AudioStartupUnmute.Add(mapping.Channel, mute);
                    }
                    else
                    {
                        SettingProfile.AudioStartupUnmute.Add(name, SettingProfile.AudioStartupUnmute[mapping.Channel]);
                        SettingProfile.AudioStartupUnmute.Remove(mapping.Channel);
                    }
                }

                mapping.Channel = name;
                return true;
            }
            else
                return false;
        }


        [ObservableProperty]
        public partial string CurrentChannel { get; set; } = "";
        public virtual ModelAudioChannels ChannelCollection { get; }

        public virtual bool SetStartupVolume
        {
            get => Source?.AudioStartupVolumes != null && CurrentChannel != null && Source.AudioStartupVolumes.TryGetValue(CurrentChannel, out double vol) && vol >= 0.0;
            set
            {
                if (Source?.AudioStartupVolumes != null && CurrentChannel != null)
                {
                    if (value)
                        Source.AudioStartupVolumes.AddOrUpdate(CurrentChannel, 1.0);
                    else
                        Source.AudioStartupVolumes.Remove(CurrentChannel);

                    Config.SaveConfiguration();
                    OnPropertyChanged(nameof(SetStartupVolume));
                    OnPropertyChanged(nameof(StartupVolume));
                }
            }
        }

        public virtual double StartupVolume
        {
            get => Source?.AudioStartupVolumes != null && CurrentChannel != null && Source.AudioStartupVolumes.TryGetValue(CurrentChannel, out double vol) ? vol : 0.0;
            set
            {
                if (Source?.AudioStartupVolumes != null && CurrentChannel != null)
                {
                    Source.AudioStartupVolumes.AddOrUpdate(CurrentChannel, value);
                    Config.SaveConfiguration();
                    OnPropertyChanged(nameof(StartupVolume));
                }
            }
        }

        public virtual bool StartupUnmute
        {
            get => Source?.AudioStartupUnmute != null && CurrentChannel != null && Source.AudioStartupUnmute.TryGetValue(CurrentChannel, out bool value) && value;
            set
            {
                if (Source?.AudioStartupUnmute != null && CurrentChannel != null)
                {
                    Source.AudioStartupUnmute.AddOrUpdate(CurrentChannel, value);
                    Config.SaveConfiguration();
                    OnPropertyChanged(nameof(StartupUnmute));
                }
            }
        }

        public virtual ModelAppMappings AppMappingCollection { get; }

        public virtual List<string> AudioDevices
        {
            get
            {
                var list = new List<string> { "All" };
                list.AddRange([.. AudioController.DeviceManager.GetDeviceNames()]);

                return list;
            }
        }

        public virtual ModelDeviceBlacklist BlacklistCollection { get; }

        public virtual Dictionary<DataFlow, string> DeviceDataFlows { get; } = new()
        {
            { DataFlow.Render, DataFlow.Render.ToString() },
            { DataFlow.Capture, DataFlow.Capture.ToString() },
            { DataFlow.All, DataFlow.All.ToString() },
        };
        public virtual DataFlow AudioDeviceFlow { get => Config.AudioDeviceFlow; set { SetConfigValue<DataFlow>(value); AudioController.ResetMappings = true; } }

        public virtual Dictionary<DeviceState, string> DeviceStates { get; } = new()
        {
            { DeviceState.Active, DeviceState.Active.ToString() },
            { DeviceState.Disabled, DeviceState.Disabled.ToString() },
            { DeviceState.NotPresent, DeviceState.NotPresent.ToString() },
            { DeviceState.Unplugged, DeviceState.Unplugged.ToString() },
            { DeviceState.MaskAll, DeviceState.MaskAll.ToString() },
        };
        public virtual DeviceState AudioDeviceState { get => Config.AudioDeviceState; set { SetConfigValue<DeviceState>(value); AudioController.ResetMappings = true; } }
    }
}
