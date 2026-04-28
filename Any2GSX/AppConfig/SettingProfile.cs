using Any2GSX.Aircraft;
using Any2GSX.Audio;
using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Any2GSX.AppConfig
{
    public class SettingProfile : ISettingProfile
    {
        [JsonIgnore]
        public static string GenericId { get; } = "generic";
        [JsonIgnore]
        public static string GenericIdUpper { get; } = "Generic";
        public static string DefaultId { get; } = "default";
        [JsonIgnore]
        public virtual bool IsDefault => Name?.Equals(DefaultId, StringComparison.InvariantCultureIgnoreCase) == true;
        public virtual string Name { get; set; } = DefaultId;
        public virtual string PluginId { get; set; } = GenericId;
        public virtual string ChannelFileId { get; set; } = GenericId;
        public virtual bool IsReadOnly { get; set; } = false;
        public virtual List<ProfileMatching> ProfileMatches { get; set; } = [];
        //Legacy
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual ProfileMatchType? MatchType { get; set; } = null;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual string MatchString { get; set; } = null;

        public virtual void Copy(SettingProfile profile)
        {
            Name = profile.Name;
            PluginId = profile.PluginId;
            ChannelFileId = profile.ChannelFileId;
            RunAutomationService = profile.RunAutomationService;
            RunAudioService = profile.RunAudioService;
            PilotsDeckIntegration = profile.PilotsDeckIntegration;

            this.ProfileMatches.Clear();
            foreach (var match in profile.ProfileMatches)
                this.ProfileMatches.Add(new(match.MatchData, match.MatchOperation, match.MatchString));
        }

        public virtual void FullCopy(SettingProfile profile)
        {
            var properties = this.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.SetMethod != null
                    && property.Name != nameof(ProfileMatches)
                    && property.Name != nameof(PluginSettings)
                    && property.Name != nameof(DepartureServices)
                    && property.Name != nameof(OperatorPreferences)
                    && property.Name != nameof(CompanyHubs)
                    && property.Name != nameof(AudioMappings)
                    && property.Name != nameof(AudioStartupVolumes)
                    && property.Name != nameof(AudioStartupUnmute))
                {
                    this.SetPropertyValue(property.Name, property.GetValue(profile));
                }
            }

            this.ProfileMatches.Clear();
            foreach (var match in profile.ProfileMatches)
                this.ProfileMatches.Add(new(match.MatchData, match.MatchOperation, match.MatchString));

            this.PluginSettings.Clear();
            foreach (var setting in profile.PluginSettings)
                this.PluginSettings.Add(setting.Key, setting.Value);

            this.DepartureServices.Clear();
            foreach (var service in profile.DepartureServices)
                this.DepartureServices.Add(service.Key, service.Value);

            this.OperatorPreferences.Clear();
            foreach (var operatorPref in profile.OperatorPreferences)
                this.OperatorPreferences.Add(operatorPref);

            this.CompanyHubs.Clear();
            foreach (var companyHub in profile.CompanyHubs)
                this.CompanyHubs.Add(companyHub);

            this.AudioMappings.Clear();
            foreach (var mapping in profile.AudioMappings)
                this.AudioMappings.Add(mapping);

            this.AudioStartupVolumes.Clear();
            foreach (var volume in profile.AudioStartupVolumes)
                this.AudioStartupVolumes.Add(volume.Key, volume.Value);

            this.AudioStartupUnmute.Clear();
            foreach (var mute in profile.AudioStartupUnmute)
                this.AudioStartupUnmute.Add(mute.Key, mute.Value);
        }

        public virtual void Load()
        {
            LoadChannelNames();
            if (AppService.Instance.PluginController.HasPlugin(PluginId, out PluginManifest pluginManifest))
                CheckPluginSettings(pluginManifest.Settings);
            else
                CheckGenericSettings();
        }

        public override string ToString()
        {
            return $"{Name} [{PluginId}]";
        }

        //PilotsDeck
        public virtual bool PilotsDeckIntegration { get; set; } = false;
        [JsonIgnore]
        public virtual bool PilotsDeckRefresh => PilotsDeckIntegration && AppService.Instance.Config.RefreshMenuForDeck;

        //OFP
        public virtual bool FuelRoundUp100 { get; set; } = true;
        public virtual bool RandomizePax { get; set; } = true;
        public virtual int RandomizePaxMaxDiff { get; set; } = 5;
        public virtual bool ApplyPaxToCargo { get; set; } = true;
        public virtual int DelayTurnAroundSeconds { get; set; } = 90;
        public virtual int DelayTurnRecheckSeconds { get; set; } = 30;
        public virtual bool RefreshGsxOnDeparture { get; set; } = true;
        public virtual bool RefreshGsxOnTurn { get; set; } = true;
        public virtual bool UseSimTime { get; set; } = true;
        public virtual bool NotifyPrepFinished { get; set; } = true;
        public virtual bool NotifyFinalReceived { get; set; } = true;
        public virtual bool NotifyChocksPlaced { get; set; } = true;
        public virtual bool NotifyTurnReady { get; set; } = true;

        //Plugin
        public virtual Dictionary<string, object> PluginSettings { get; set; } = [];

        //GSX Automation
        public virtual bool SkipWalkAround { get; set; } = true;
        public virtual bool RunAutomationService { get; set; } = false;
        public virtual bool ConnectGpuWithApuRunning { get; set; } = true;
        public virtual int ConnectPca { get; set; } = 2; // 0 => false | 1 => true | 2 => only on jetway stand
        public virtual bool PcaOverride { get; set; } = true;
        public virtual bool CallReposition { get; set; } = true;
        public virtual bool CallJetwayStairsInWalkaround { get; set; } = true;
        public virtual bool CallJetwayStairsOnPrep { get; set; } = true;
        public virtual bool CallJetwayStairsDuringDeparture { get; set; } = true;
        public virtual bool CallJetwayStairsOnArrival { get; set; } = true;
        public virtual int RemoveStairsAfterDepature { get; set; } = 2; // 0 => false | 1 => true | 2 => only on jetway stand
        public virtual bool AttemptConnectStairRefuel { get; set; } = true;
        public virtual int DelayCallRefuelAfterStair { get; set; } = 60;
        public virtual bool SkipFuelOnTankering { get; set; } = true;
        public virtual bool CallPushbackOnBeacon { get; set; } = false;
        public virtual int CallPushbackWhenTugAttached { get; set; } = 2; // 0 => false | 1 => after Departure Services | 2 => after Final LS
        public virtual bool CancelServicesOnPushPhase { get; set; } = true;
        public virtual bool ClearGroundEquipOnBeacon { get; set; } = true;
        public virtual bool ClearChocksOnTugAttach { get; set; } = true;
        public virtual bool GradualGroundEquipRemoval { get; set; } = false;
        public virtual bool CallDeboardOnArrival { get; set; } = true;
        public virtual bool RunDepartureOnArrival { get; set; } = false;
        public virtual GsxCancelService SmartButtonAbortService { get; set; } = GsxCancelService.Never; // 0 => Never | 1 => abort current service gracefully | 2 => abort current service forcefully
        public virtual int ChockDelayMin { get; set; } = 10;
        public virtual int ChockDelayMax { get; set; } = 20;
        public virtual int FinalDelayMin { get; set; } = 90;
        public virtual int FinalDelayMax { get; set; } = 150;
        public virtual bool CloseDoorsOnFinal { get; set; } = true;
        public virtual bool DoorPaxHandling { get; set; } = true;
        public virtual bool DoorStairHandling { get; set; } = true;
        public virtual bool DoorServiceHandling { get; set; } = true;
        public virtual bool DoorPanelHandling { get; set; } = true;
        public virtual bool DoorCargoHandling { get; set; } = true;
        public virtual bool DoorCargoOpenOnActive { get; set; } = false;
        public virtual int DoorCargoOpenCloseDelay { get; set; } = 2;
        public virtual bool DoorsCargoKeepOpenOnDetachBoard { get; set; } = false;
        public virtual bool DoorsCargoKeepOpenOnDetachDeboard { get; set; } = false;
        public virtual bool RemoveJetwayStairsOnFinal { get; set; } = true;

        public virtual bool FuelSaveLoadFob { get; set; } = true;
        public virtual double FuelResetBaseKg { get; set; } = 2500;
        public virtual double RefuelRateKgSec { get; set; } = 28;
        public virtual bool RefuelFinishOnHose { get; set; } = false;
        public virtual bool UseRefuelTimeTarget { get; set; } = false;
        public virtual int RefuelTimeTargetSeconds { get; set; } = 300;
        public virtual bool ResetPayloadOnPrep { get; set; } = true;
        public virtual bool ResetPayloadOnTurn { get; set; } = true;

        public virtual int AttachTugDuringBoarding { get; set; } = 1; // 0 => not answer | 1 => no | 2 => yes
        public virtual bool OperatorAutoSelect { get; set; } = true;
        public virtual bool OperatorPreferenceSelect { get; set; } = true;
        public virtual List<string> OperatorPreferences { get; set; } = [];
        public virtual List<string> CompanyHubs { get; set; } = [];
        public virtual bool KeepDirectionMenuOpen { get; set; } = true;
        public virtual bool AnswerDeiceOnReopen { get; set; } = true;
        public virtual bool SkipCrewBoardQuestion { get; set; } = true;
        public virtual bool SkipCrewDeboardQuestion { get; set; } = true;
        public virtual int DefaultPilotTarget { get; set; } = 2;
        public virtual int DefaultCrewTarget { get; set; } = 4;
        public virtual int AnswerCrewBoardQuestion { get; set; } = 1; // 0 => not answer | 1 => nobody | 2 => crew | 3 => pilots | 4 => both | (num - 1 for menu)
        public virtual int AnswerCrewDeboardQuestion { get; set; } = 1; // 0 => not answer | 1 => nobody | 2 => crew | 3 => pilots | 4 => both | (num - 1 for menu)
        public virtual bool SkipFollowMe { get; set; } = true;
        public virtual GsxChangePark ClearGateMenuOption { get; set; } = GsxChangePark.ClearAI;
        public virtual GsxStopPush StopPushMenuOption { get; set; } = GsxStopPush.Pause;
        public virtual bool EnableMenuForSelection { get; set; } = true;

        public virtual SortedDictionary<int, ServiceConfig> DepartureServices { get; set; } = new()
        {
            { 0, new ServiceConfig(GsxServiceType.Lavatory, GsxServiceActivation.AfterRequested, TimeSpan.Zero, GsxServiceConstraint.TurnAround, false, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero) },
            { 1, new ServiceConfig(GsxServiceType.Water, GsxServiceActivation.AfterRequested, TimeSpan.Zero, GsxServiceConstraint.NoneAlways, false, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero) },
            { 2, new ServiceConfig(GsxServiceType.Refuel, GsxServiceActivation.AfterRequested, TimeSpan.Zero, GsxServiceConstraint.NoneAlways, true, TimeSpan.Zero, TimeSpan.Zero,TimeSpan.Zero) },
            { 3, new ServiceConfig(GsxServiceType.Cleaning, GsxServiceActivation.AfterActive, TimeSpan.Zero, GsxServiceConstraint.TurnAround, false, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero) },
            { 4, new ServiceConfig(GsxServiceType.Catering, GsxServiceActivation.AfterRequested, TimeSpan.Zero, GsxServiceConstraint.NoneAlways, false, TimeSpan.Zero, TimeSpan.Zero,TimeSpan.Zero) },
            { 5, new ServiceConfig(GsxServiceType.Boarding, GsxServiceActivation.AfterAllCompleted, TimeSpan.Zero, GsxServiceConstraint.NoneAlways, true, TimeSpan.Zero, TimeSpan.Zero,TimeSpan.Zero) },
        };

        public virtual bool IsCompanyHub(string icao)
        {
            try
            {
                if (string.IsNullOrEmpty(icao))
                    return false;

                foreach (var hub in CompanyHubs)
                {
                    if (icao.StartsWith(hub, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return false;
        }

        //Audio
        public virtual bool RunAudioService { get; set; } = false;
        [JsonIgnore]
        public virtual List<string> ChannelIds { get; } = [];
        public virtual List<AudioMapping> AudioMappings { get; set; } = [];
        public virtual Dictionary<string, double> AudioStartupVolumes { get; set; } = [];
        public virtual Dictionary<string, bool> AudioStartupUnmute { get; set; } = [];

        protected virtual void LoadChannelNames()
        {
            var aircraftChannels = GetAircraftChannels();
            if (aircraftChannels != null)
            {
                ChannelIds.Clear();
                ChannelIds.AddRange(aircraftChannels.ChannelDefinitions.Select(d => d.Name));
            }
        }

        public virtual AircraftChannels GetAircraftChannels()
        {
            if (!string.IsNullOrWhiteSpace(ChannelFileId) && ChannelFileId != GenericId)
                return AircraftChannels.LoadChannelFile(ChannelFileId);
            else
                return new GenericChannels();
        }

        //Plugin Settings
        public virtual string GetPluginKey(string key)
        {
            return $"{AppService.Instance.PluginController.GetPluginSettingKey(PluginId)}.Option.{key}";
        }

        public virtual bool HasSetting(string key)
        {
            return PluginSettings.ContainsKey(key);
        }

        public virtual bool HasSetting<T>(string key, out T value)
        {
            try
            {
                if (PluginSettings.ContainsKey(key))
                {
                    value = GetSetting<T>(key);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            value = default;
            return false;
        }

        public virtual bool HasPluginSetting<T>(string key, out T value)
        {
            return HasSetting<T>(GetPluginKey(key), out value);
        }

        public virtual bool HasGenericSetting<T>(string key, out T value)
        {
            return HasSetting<T>($"{GenericIdUpper}.Option.{key}", out value);
        }

        public virtual T GetSetting<T>(string key)
        {
            try
            {
                if (PluginSettings.TryGetValue(key, out object oValue))
                {
                    if (typeof(T) != typeof(object) && oValue is JsonElement element)
                        return (T)GetObjectFromElement<T>(element);
                    else if (oValue != null)
                        return (T)oValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error on Key '{key}'");
                Logger.LogException(ex);
            }

            return default;
        }

        public virtual dynamic GetSetting(PluginSetting pluginSetting)
        {
            if (pluginSetting.Type == PluginSettingType.Bool)
                return GetSetting<bool>(GetPluginKey(pluginSetting.Key));
            else if (pluginSetting.Type == PluginSettingType.Integer)
                return GetSetting<int>(GetPluginKey(pluginSetting.Key));
            else if (pluginSetting.Type == PluginSettingType.Number)
                return GetSetting<double>(GetPluginKey(pluginSetting.Key));
            else if (pluginSetting.Type == PluginSettingType.String)
                return GetSetting<string>(GetPluginKey(pluginSetting.Key));
            else if (pluginSetting.Type == PluginSettingType.Enum)
                return GetSetting<int>(GetPluginKey(pluginSetting.Key));
            else
                return default;
        }

        public virtual T GetPluginSetting<T>(string key)
        {
            return GetSetting<T>(GetPluginKey(key));
        }

        public virtual T GetGenericSetting<T>(string key)
        {
            return GetSetting<T>($"{GenericIdUpper}.Option.{key}");
        }

        public virtual object GetObjectFromElement<T>(JsonElement element)
        {
            object value;
            if (typeof(T) == typeof(bool) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
                value = element.GetBoolean();
            else if (typeof(T) == typeof(double) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double dblValue))
                value = dblValue;
            else if (typeof(T) == typeof(int) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int intValue))
                value = intValue;
            else if (typeof(T).IsEnum && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int enumValue))
                value = enumValue;
            else if (typeof(T) == typeof(string) && element.ValueKind == JsonValueKind.String)
                value = element.GetString();
            else
                value = element.GetRawText();

            return value;
        }

        public virtual bool SetSetting(string key, object value)
        {
            try
            {
                if (PluginSettings.ContainsKey(key))
                {
                    PluginSettings[key] = value;
                    return false;
                }
                else
                {
                    PluginSettings.Add(key, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public virtual bool SetPluginSetting(string key, object value)
        {
            SetSetting(GetPluginKey(key), value);
            AppService.Instance.Config.SaveConfiguration();
            return true;
        }

        public virtual bool SetGenericSetting(string key, object value)
        {
            SetSetting($"{GenericIdUpper}.Option.{key}", value);
            AppService.Instance.Config.SaveConfiguration();
            return true;
        }

        public virtual void CheckGenericSettings()
        {
            Logger.Debug($"Check Generic Settings in Profile '{Name}'");
            var pluginSettings = GenericSettings.GetGenericSettings();
            foreach (var setting in pluginSettings)
                AddSetting(setting);
            AppService.Instance.Config.SaveConfiguration();
        }

        public virtual void CheckPluginSettings(List<PluginSetting> pluginSettings)
        {
            Logger.Debug($"Check Plugin Settings in Profile '{Name}' ({pluginSettings?.Count})");
            var generic = GenericSettings.GetGenericSettings();
            foreach (var setting in pluginSettings)
            {
                if (!AddSetting(setting) && generic.Where(s => s.Key == setting.Key).Any())
                {
                    Logger.Debug($"Set Default Setting Key '{setting.Key}' ({setting?.DefaultValue ?? ""})");
                    SetSetting(setting.Key, setting.DefaultValue);
                }
            }
            AppService.Instance.Config.SaveConfiguration();
        }

        protected virtual bool AddSetting(PluginSetting setting)
        {
            if (!HasSetting(setting.Key))
            {
                Logger.Debug($"Add Setting Key '{setting.Key}' ({setting?.DefaultValue ?? ""})");
                if (setting.DefaultValue is JsonElement element)
                {
                    if (setting.Type == PluginSettingType.Bool)
                        SetSetting(setting.Key, GetObjectFromElement<bool>(element));
                    else if (setting.Type == PluginSettingType.Integer || setting.Type == PluginSettingType.Enum)
                        SetSetting(setting.Key, GetObjectFromElement<int>(element));
                    else if (setting.Type == PluginSettingType.Number)
                        SetSetting(setting.Key, GetObjectFromElement<double>(element));
                    else if (setting.Type == PluginSettingType.String)
                        SetSetting(setting.Key, GetObjectFromElement<string>(element));
                }
                else
                    SetSetting(setting.Key, setting.DefaultValue);

                return true;
            }
            else
                return false;
        }

        //Profile Matching
        [JsonIgnore]
        public int MatchingScore { get; set; } = 0;

        public virtual void Match(AppService appService)
        {
            MatchingScore = 0;
            foreach (var match in ProfileMatches)
                MatchingScore += match.Match(appService);
        }
    }
}
