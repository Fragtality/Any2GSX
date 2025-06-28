using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfiles : ModelBase<Config>
    {
        protected virtual Selector Selector { get; }
        protected virtual ModelProfileCollection ProfileCollection { get; }
        public virtual ViewModelSelector<SettingProfile, SettingProfile> ViewModelSelector { get; }
        protected virtual DispatcherTimer AircraftUpdateTimer { get; set; }
        protected virtual DispatcherTimer ProfileUpdateTimer { get; }
        protected virtual bool ForceRefresh { get; set; } = false;
        public virtual ICommandWrapper SetActiveCommand { get; }
        public virtual ICommandWrapper ImportCommand { get; }
        public virtual ICommandWrapper ExportCommand { get; }
        public virtual ICommandWrapper CloneCommand { get; }

        public ModelProfiles(AppService source, Selector selector) : base(source.Config, source)
        {
            Selector = selector;
            ProfileCollection = new();
            ViewModelSelector = new(Selector, ProfileCollection, AppWindow.IconLoader);

            Selector.SelectionChanged += (_, _) => NotifyPropertyChanged(nameof(IsEditAllowed));
            ProfileCollection.CreateMemberBinding<ProfileMatchType, ProfileMatchType>(nameof(SettingProfile.MatchType), new NoneConverter(), null);
            ProfileCollection.CreateMemberBinding<string, string>(nameof(SettingProfile.PluginId), new NoneConverter());
            ProfileCollection.CreateMemberBinding<string, string>(nameof(SettingProfile.ChannelFileId), new NoneConverter());
            ProfileCollection.CreateMemberBinding<bool, bool>(nameof(SettingProfile.RunAutomationService), new NoneConverter());
            ProfileCollection.CreateMemberBinding<bool, bool>(nameof(SettingProfile.RunAudioService), new NoneConverter());
            ProfileCollection.CreateMemberBinding<bool, bool>(nameof(SettingProfile.PilotsDeckIntegration), new NoneConverter());
            ProfileCollection.CollectionChanged += OnCollectionChanged;

            SetActiveCommand = new CommandWrapper(() => AppService.SetSettingProfile((Selector?.SelectedValue as SettingProfile)?.Name), () => Selector?.SelectedValue is SettingProfile);
            SetActiveCommand.Subscribe(Selector);

            ImportCommand = new CommandWrapper(ImportProfile);

            ExportCommand = new CommandWrapper(ExportProfile, () => Selector?.SelectedValue is SettingProfile profile);
            ExportCommand.Subscribe(Selector);

            CloneCommand = new CommandWrapper(CloneProfile, () => Selector?.SelectedValue is SettingProfile profile);
            CloneCommand.Subscribe(Selector);

            Config.PropertyChanged += OnConfigPropertyChanged;

            ProfileUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            ProfileUpdateTimer.Tick += ProfileUpdateTimer_Tick;
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Config.SettingProfiles))
                ProfileUpdateTimer.Start();
        }

        protected virtual void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitConfigSave)
                Config.SaveConfiguration();
        }

        public virtual void CheckActiveProfile()
        {
            if (!Config.SettingProfiles.Any(p => p.Name == AppService.SettingProfile?.Name))
                AppService.SetSettingProfile();
        }

        protected virtual void ProfileUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            ProfileCollection.NotifyCollectionChanged();
            InhibitConfigSave = false;
            ProfileUpdateTimer.Stop();
        }

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

                if (Config.ImportProfile(json, out bool wasDefault))
                {
                    ProfileCollection.NotifyCollectionChanged();
                    if (wasDefault && AppService?.SettingProfile?.MatchType == ProfileMatchType.Default)
                        AppService.SetSettingProfile(SettingProfile.DefaultId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void ExportProfile()
        {
            try
            {
                Logger.Debug($"Exporting Profile to Clipboard ...");
                if (Selector?.SelectedValue is not SettingProfile profile)
                {
                    Logger.Warning($"The selected Value is not a SettingProfile");
                    return;
                }

                string json = JsonSerializer.Serialize<SettingProfile>(profile);
                ClipboardHelper.SetClipboard(json);
                Logger.Debug($"Copied Profile '{profile.Name}' to Clipboard");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void CloneProfile()
        {
            try
            {
                Logger.Debug($"Cloning Profile ...");
                if (Selector?.SelectedValue is not SettingProfile profile)
                {
                    Logger.Warning($"The selected Value is not a SettingProfile");
                    return;
                }

                string json = JsonSerializer.Serialize<SettingProfile>(profile);
                var clone = JsonSerializer.Deserialize<SettingProfile>(json);
                clone.Name = $"Clone of {profile.Name}";
                if (clone.MatchType == ProfileMatchType.Default)
                {
                    Logger.Debug($"Create Clone of Default Profile");
                    clone.MatchType = ProfileMatchType.AircraftString;
                }

                if (Config.SettingProfiles.Any(p => p.Name.Equals(clone.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.Warning($"The Profile '{clone.Name}' is already configured");
                    return;
                }

                Config.SettingProfiles.Add(clone);
                ProfileCollection.NotifyCollectionChanged();
                Logger.Debug($"Cloned Profile '{profile.Name}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected override void InitializeModel()
        {
            AircraftUpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(AppService.Instance.Config.UiRefreshInterval),
            };
            AircraftUpdateTimer.Tick += OnAcDataUpdate;
        }

        public virtual void Start()
        {
            ForceRefresh = true;
            AircraftUpdateTimer.Start();
        }

        public virtual void Stop()
        {
            AircraftUpdateTimer?.Stop();
        }

        protected virtual void UpdateState<T>(string propertyValue, T value)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (!this.GetPropertyValue<T>(propertyValue)?.Equals(value) == true || ForceRefresh)
                    this.SetPropertyValue<T>(propertyValue, value);
            }
            catch { }
        }

        protected virtual void OnAcDataUpdate(object? sender, EventArgs e)
        {
            try { UpdateState<string>(nameof(CurrentAirline), AppService.GetAirline()); } catch { }
            try { UpdateState<string>(nameof(CurrentAtcId), AppService.GetAtcId()); } catch { }
            try { UpdateState<string>(nameof(CurrentTitle), AppService.GetTitle()); } catch { }
            try { UpdateState<string>(nameof(AircraftString), AppService.GetAircraftString()); } catch { }
            try { UpdateState<string>(nameof(CurrentProfile), AppService.SettingProfile?.InfoString() ?? ""); } catch { }
            ForceRefresh = false;
        }

        [ObservableProperty]
        protected string _CurrentAirline = "";

        [ObservableProperty]
        protected string _CurrentAtcId = "";

        [ObservableProperty]
        protected string _CurrentTitle = "";

        [ObservableProperty]
        protected string _AircraftString = "";

        [ObservableProperty]
        protected string _CurrentProfile = "";

        public static Dictionary<ProfileMatchType, string> MatchTypes { get; } = new()
        {
            {ProfileMatchType.Default, "Default" },
            {ProfileMatchType.Airline, "Airline" },
            {ProfileMatchType.Title, "Title/Livery" },
            {ProfileMatchType.AtcId, "ATC ID" },
            {ProfileMatchType.AircraftString, "SimObject" },
        };

        public virtual bool IsSelectionNonDefault()
        {
            return !IsSelectionDefault();
        }

        public virtual bool IsEditAllowed => !IsSelectionDefault() || Selector?.SelectedValue == null;

        public virtual bool IsSelectionDefault()
        {
            return (Selector?.SelectedValue is SettingProfile profile && profile.MatchType == ProfileMatchType.Default);
        }
    }
}
