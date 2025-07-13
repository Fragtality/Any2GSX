using Any2GSX.AppConfig;
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
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Profiles
{
    public partial class ModelProfiles : ModelBase<Config>
    {
        protected virtual Selector ProfileSelector { get; }
        public virtual ModelProfileCollection ProfileCollection { get; }
        public virtual ModelMatchingCollection MatchingCollection { get; }
        protected virtual DispatcherTimer AircraftUpdateTimer { get; set; }
        protected virtual DispatcherTimer ProfileUpdateTimer { get; }
        protected virtual bool ForceRefresh { get; set; } = false;
        public virtual ICommandWrapper SetActiveCommand { get; }
        public virtual ICommandWrapper ImportCommand { get; }
        public virtual ICommandWrapper ExportCommand { get; }
        public virtual ICommandWrapper CloneCommand { get; }

        public ModelProfiles(AppService source, Selector profileSelector) : base(source.Config, source)
        {
            ProfileCollection = new();
            ProfileSelector = profileSelector;
            MatchingCollection = new(source?.SettingProfile);

            ProfileCollection.CollectionChanged += OnCollectionChanged;

            SetActiveCommand = new CommandWrapper(() => AppService.SetSettingProfile((ProfileSelector?.SelectedValue as ModelProfileItem)?.Name), () => ProfileSelector?.SelectedValue is ModelProfileItem);
            SetActiveCommand.Subscribe(ProfileSelector);

            ImportCommand = new CommandWrapper(ImportProfile);

            ExportCommand = new CommandWrapper(ExportProfile, () => ProfileSelector?.SelectedValue is ModelProfileItem profile);
            ExportCommand.Subscribe(ProfileSelector);

            CloneCommand = new CommandWrapper(CloneProfile, () => ProfileSelector?.SelectedValue is ModelProfileItem profile);
            CloneCommand.Subscribe(ProfileSelector);

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
            MatchingCollection.ChangeProfile(AppService?.SettingProfile);
            InhibitConfigSave = false;
            ProfileUpdateTimer.Stop();
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

        public static Dictionary<MatchData, string> MatchDataTexts => ProfileMatching.MatchDataTexts;
        public static Dictionary<MatchOperation, string> MatchOperationTexts => ProfileMatching.MatchOperationTexts;

        public virtual bool IsSelectionNonDefault()
        {
            return !IsSelectionReadOnly();
        }

        public virtual bool IsEditAllowed => IsSelectionNonDefault() || ProfileSelector?.SelectedValue == null;

        public virtual bool IsSelectionReadOnly()
        {
            return (ProfileSelector?.SelectedValue is ModelProfileItem profile && profile.IsReadOnly);
        }

        public virtual void ProfileSelectionChanged(SettingProfile profile)
        {
            InhibitConfigSave = true;
            MatchingCollection.ChangeProfile(profile);
            MatchingCollection.NotifyCollectionChanged();
            NotifyPropertyChanged(nameof(IsEditAllowed));
            InhibitConfigSave = false;
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

                if (Config.ImportProfile(json))
                {
                    AppService.Instance.Config?.SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
                    ProfileCollection.NotifyCollectionChanged();
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
                if (ProfileSelector?.SelectedValue is not ModelProfileItem profileItem)
                {
                    Logger.Warning($"The selected Value is not a SettingProfile");
                    return;
                }

                string json = JsonSerializer.Serialize<SettingProfile>(profileItem.Source);
                ClipboardHelper.SetClipboard(json);
                Logger.Debug($"Copied Profile '{profileItem.Source.Name}' to Clipboard");
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
                if (ProfileSelector?.SelectedValue is not ModelProfileItem profileItem)
                {
                    Logger.Warning($"The selected Value is not a SettingProfile");
                    return;
                }

                string json = JsonSerializer.Serialize<SettingProfile>(profileItem.Source);
                var clone = JsonSerializer.Deserialize<SettingProfile>(json);
                clone.Name = $"Clone of {profileItem.Source.Name}";
                if (clone.IsDefault)
                {
                    Logger.Debug($"Create Clone of Default Profile");
                    clone.IsReadOnly = false;
                }

                if (Config.SettingProfiles.Any(p => p.Name.Equals(clone.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.Warning($"The Profile '{clone.Name}' is already configured");
                    return;
                }

                Config.SettingProfiles.Add(clone);
                AppService.Instance.Config?.SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
                ProfileCollection.NotifyCollectionChanged();
                Logger.Debug($"Cloned Profile '{profileItem.Source.Name}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
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
            try { UpdateState<string>(nameof(CurrentProfile), AppService.SettingProfile?.ToString() ?? ""); } catch { }
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
    }
}
