using Any2GSX.AppConfig;
using Any2GSX.Audio;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAudio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Audio
{
    public partial class ModelAudio : ModelBase<SettingProfile>
    {
        protected virtual DispatcherTimer UpdateTimer { get; }
        public ICommand CommandDebugInfo { get; } = new RelayCommand(() => AppService.Instance.AudioController.DeviceManager.WriteDebugInformation());
        public override SettingProfile Source => AppService.Instance?.SettingProfile;
        public virtual string ProfileName => AppService.Instance?.SettingProfile.Name;
        public virtual Visibility ChannelVisibility => string.IsNullOrWhiteSpace(AppService.Instance?.SettingProfile?.ChannelFileId) || AppService.Instance?.SettingProfile?.ChannelFileId == SettingProfile.GenericId ? Visibility.Collapsed : Visibility.Visible;

        public ModelAudio(AppService appService) : base(appService.SettingProfile, appService)
        {
            UpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(750),
            };
            UpdateTimer.Tick += UpdateTimer_Tick;

            ChannelCollection = new(this);
            
            AppMappingCollection = new(this);
            AppMappingCollection.CollectionChanged += (_, e) => { SaveConfig(); AudioController.ResetMappings = true; };

            BlacklistCollection = new(this);
            BlacklistCollection.CollectionChanged += (_, _) => SaveConfig();
        }

        protected virtual void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            ChannelCollection.NotifyCollectionChanged();
            AppMappingCollection.NotifyCollectionChanged();
            BlacklistCollection.NotifyCollectionChanged();
            CurrentChannel = ChannelCollection?.Source?.FirstOrDefault() ?? "";
            NotifyPropertyChanged(nameof(ChannelVisibility));
            InhibitConfigSave = false;
            UpdateTimer.Stop();
        }

        protected override void InitializeModel()
        {
            this.PropertyChanged += OnSelfPropertyChanged;
            AudioController.DeviceManager.DevicesChanged += () => NotifyPropertyChanged(nameof(AudioDevices));
            AppService.ProfileChanged += OnProfileChanged;
            AppService.AudioController.OnChannelsChanged += () => OnProfileChanged(null);
            Config.PropertyChanged += OnConfigPropertyChanged;
        }

        protected virtual void OnProfileChanged(SettingProfile profile)
        {
            if (profile != null)
                AudioController.ResetChannels = true;
            NotifyPropertyChanged(string.Empty);
            NotifyPropertyChanged(nameof(ProfileName));
            UpdateTimer.Start();
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

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Config.CurrentProfile))
                NotifyPropertyChanged(nameof(ProfileName));
        }

        [ObservableProperty]
        protected string _CurrentChannel = "";

        public virtual ModelAudioChannels ChannelCollection { get; }

        public virtual bool SetStartupVolume
        {
            get => Source?.AudioStartupVolumes != null && CurrentChannel != null && Source.AudioStartupVolumes.TryGetValue(CurrentChannel, out double vol) && vol >= 0.0;
            set
            {
                if (Source?.AudioStartupVolumes != null && CurrentChannel != null)
                {
                    double setValue = value ? 1.0 : -1.0;
                    Source.AudioStartupVolumes.AddOrUpdate(CurrentChannel, setValue);
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
        public virtual DataFlow AudioDeviceFlow { get => Config.AudioDeviceFlow; set { SetModelValue<DataFlow>(value); AudioController.ResetMappings = true; } }

        public virtual Dictionary<DeviceState, string> DeviceStates { get; } = new()
        {
            { DeviceState.Active, DeviceState.Active.ToString() },
            { DeviceState.Disabled, DeviceState.Disabled.ToString() },
            { DeviceState.NotPresent, DeviceState.NotPresent.ToString() },
            { DeviceState.Unplugged, DeviceState.Unplugged.ToString() },
            { DeviceState.MaskAll, DeviceState.MaskAll.ToString() },
        };
        public virtual DeviceState AudioDeviceState { get => Config.AudioDeviceState; set { SetModelValue<DeviceState>(value); AudioController.ResetMappings = true; } }
    }
}
