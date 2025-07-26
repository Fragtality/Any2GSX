using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CFIT.AppTools;
using CoreAudio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace Any2GSX.AppConfig
{
    public class Config : AppConfigBase<Definition>, INotifyPropertyChanged, IConfig
    {
        public virtual int PortBase { get; set; } = 60060;
        public virtual int PortRange { get; set; } = 10;
        public virtual bool OpenAppWindowOnStart { get; set; } = false;
        [JsonIgnore]
        public virtual bool ForceOpen { get; set; } = false;
        public virtual bool AutoInstallGsxProfiles { get; set; } = false;
        public virtual double WeightConversion { get; set; } = 2.2046226218;
        public virtual string BinaryGsx2020 { get; set; } = "Couatl64_MSFS";
        public virtual string BinaryGsx2024 { get; set; } = "Couatl64_MSFS2024";
        public virtual string Msfs2024WindowTitle { get; set; } = "Microsoft Flight Simulator 2024 - ";
        public virtual string SimbriefUrlBase { get; set; } = "https://www.simbrief.com";
        public virtual string SimbriefUrlPathName { get; set; } = "/api/xml.fetcher.php?username={0}&json=v2";
        public virtual string SimbriefUrlPathId { get; set; } = "/api/xml.fetcher.php?userid={0}&json=v2";
        public virtual string SimbriefUser { get; set; } = "";
        public virtual int SessionInitDelayMs { get; set; } = 0;
        public virtual int DeckRefreshSelectionDelay { get; set; } = 3000;
        public virtual int DeckRegisterDelay { get; set; } = 2000;
        public virtual int DeckClearedMenuRefresh { get; set; } = 5000;
        public virtual bool RefreshMenuForEfb { get; set; } = false;
        public virtual string DeckUrlBase { get; set; } = "http://localhost:42042";
        public virtual string DeckMessageWrite { get; set; } = "/v1/set/{0}={1}";
        public virtual string DeckMessageRegister { get; set; } = "/v1/register/{0}";
        public virtual string DeckMessageUnregister { get; set; } = "/v1/unregister/{0}";
        public virtual string DeckVarConnected { get; set; } = "X:ANY2GSX_RUNNING";
        public virtual string DeckVarMenu { get; set; } = "X:GSX_MENU_TITLE";
        public virtual string DeckVarLine { get; set; } = "X:GSX_MENU_LINE";
        public virtual string DeckVarState { get; set; } = "X:GSX_INFO_STATE";
        public virtual string DeckVarCall { get; set; } = "X:GSX_INFO_CALL";
        public virtual string DeckVarInfoPax { get; set; } = "X:GSX_INFO_PAX";
        public virtual string DeckVarInfoCargo { get; set; } = "X:GSX_INFO_CARGO";
        public virtual int UiRefreshInterval { get; set; } = 500;
        public virtual double FuelCompareVariance { get; set; } = 50;
        public virtual int TimerGsxCheck { get; set; } = 1000;
        public virtual int TimerGsxProcessCheck { get; set; } = 5000;
        public virtual int TimerGsxStartupMenuCheck { get; set; } = 5000;
        public virtual int GsxMenuStartupMaxFail { get; set; } = 3;
        public virtual bool RestartGsxStartupFail { get; set; } = false;
        public virtual int WaitGsxRestart { get; set; } = 15;
        public virtual bool RestartGsxOnTaxiIn { get; set; } = false;
        public virtual int DelayGsxBinaryStart { get; set; } = 2000;
        public virtual string AudioDebugFile { get; set; } = "log\\AudioDebug.txt";
        public virtual DataFlow AudioDeviceFlow { get; set; } = DataFlow.Render;
        public virtual DeviceState AudioDeviceState { get; set; } = DeviceState.Active;
        public virtual int AudioServiceRunInterval { get; set; } = 1000;
        public virtual int AudioProcessCheckInterval { get; set; } = 2500;
        public virtual int AudioProcessStartupDelay { get; set; } = 2000;
        public virtual int AudioDeviceCheckInterval { get; set; } = 60000;
        public virtual int AudioProcessMaxSearchCount { get; set; } = 30;
        public virtual bool AudioSynchSessionOnCountChange { get; set; } = false;
        public virtual List<string> AudioDeviceBlacklist { get; set; } = [];
        public virtual int GsxServiceStartDelay { get; set; } = 4000;
        public virtual int GroundTicks { get; set; } = 2;
        public virtual int DelayForegroundChange { get; set; } = 1250;
        public virtual int DelayAircraftModeChange { get; set; } = 1500;
        public virtual int MenuCheckInterval { get; set; } = 250;
        public virtual int MenuOpenTimeout { get; set; } = 2500;
        [JsonIgnore]
        public virtual DisplayUnit DisplayUnitCurrent { get; set; }
        [JsonIgnore]
        public string DisplayUnitCurrentString => DisplayUnitCurrent.ToString().ToLowerInvariant();
        public virtual DisplayUnit DisplayUnitDefault { get; set; } = DisplayUnit.KG;
        public virtual DisplayUnitSource DisplayUnitSource { get; set; } = DisplayUnitSource.Simbrief;
        public virtual int OperatorWaitTimeout { get; set; } = 1500;
        public virtual int OperatorSelectTimeout { get; set; } = 10000;
        public virtual bool DebugArrival { get; set; } = false;
        public virtual int StateMachineInterval { get; set; } = 500;
        public virtual int DelayServiceStateChange { get; set; } = 500;
        public virtual int SpeedTresholdTaxiOut { get; set; } = 2;
        public virtual int SpeedTresholdTaxiIn { get; set; } = 30;

        [JsonIgnore]
        public virtual SettingProfile CurrentProfile => AppService.Instance?.SettingProfile;
        public virtual List<SettingProfile> SettingProfiles { get; set; } = new()
        {
            { new SettingProfile() { IsReadOnly = true } }
        };
        public virtual Dictionary<string, double> FuelFobSaved { get; set; } = [];
        public virtual double FuelResetPercent { get; set; } = 0.02;

        public override void SaveConfiguration()
        {
            SaveConfiguration<Config>(this, ConfigFile);
            Logger.Debug($"Configuration saved");
        }

        protected override void InitConfiguration()
        {
            DisplayUnitCurrent = DisplayUnitDefault;

            SettingProfiles ??= [];

            var query = SettingProfiles.Where(p => p.IsDefault);
            if (query?.Any() == false)
                SettingProfiles.Add(new SettingProfile() { IsReadOnly = true });
        }

        protected override void UpdateConfiguration(int buildConfigVersion)
        {
            if (ConfigVersion < 2 && buildConfigVersion >= 2)
            {
                foreach (var profile in SettingProfiles)
                {
                    if (profile.MatchType == ProfileMatchType.Default)
                        profile.IsReadOnly = true;
                    else if (profile.MatchType == ProfileMatchType.Airline)
                        profile.ProfileMatches.Add(new(MatchData.Airline, MatchOperation.StartsWith, profile.MatchString));
                    else if (profile.MatchType == ProfileMatchType.Title)
                        profile.ProfileMatches.Add(new(MatchData.Title, MatchOperation.Contains, profile.MatchString));
                    else if (profile.MatchType == ProfileMatchType.AtcId)
                        profile.ProfileMatches.Add(new(MatchData.AtcId, MatchOperation.Equals, profile.MatchString));
                    else if (profile.MatchType == ProfileMatchType.AircraftString)
                        profile.ProfileMatches.Add(new(MatchData.SimObject, MatchOperation.Contains, profile.MatchString));
                    profile.MatchType = null;
                    profile.MatchString = null;
                }
            }
        }

        public virtual bool ImportProfile(string json)
        {
            try
            {
                SettingProfile profile = null;
                try { profile = JsonSerializer.Deserialize<SettingProfile>(json); } catch { }
                if (profile == null)
                {
                    Logger.Warning($"SettingProfile json Data could not be parsed");
                    return false;
                }

                return ImportProfile(profile);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public virtual bool ImportProfile(SettingProfile profile)
        {
            bool result = false;

            if (profile == null)
            {
                Logger.Warning($"SettingProfile is NULL");
                return result;
            }

            var query = SettingProfiles.Where(p => p.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase));
            if (query.Any())
            {
                Logger.Debug($"The Profile '{profile.Name}' is already configured");
                if (MessageBox.Show($"The Profile '{profile.Name}' is already configured.\r\nDo you want to override it?", "Profile already exists", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return result;
                else
                {
                    query.First().FullCopy(profile);
                    Logger.Debug($"Profile '{profile.Name}' overridden");
                    result = true;
                }
            }
            else
            {
                SettingProfiles.Add(profile);
                Logger.Debug($"Profile '{profile.Name}' imported");
                result = true;
            }

            if (result)
            {
                SettingProfiles?.Sort((x, y) => x.Name.CompareTo(y.Name));
                SaveConfiguration();
            }

            return result;
        }

        public virtual void SetDisplayUnit(DisplayUnit displayUnit)
        {
            bool notify = DisplayUnitCurrent != displayUnit;
            DisplayUnitCurrent = displayUnit;
            if (notify)
                NotifyDisplayUnit();
        }

        public virtual double ConvertKgToDisplayUnit(double kg)
        {
            if (DisplayUnitCurrent == DisplayUnit.KG)
                return kg;
            else
                return kg * WeightConversion;
        }

        public virtual double ConvertLbToDisplayUnit(double lb)
        {
            if (DisplayUnitCurrent == DisplayUnit.LB)
                return lb;
            else
                return lb / WeightConversion;
        }

        public virtual double ConvertFromDisplayUnitKg(double value)
        {
            if (DisplayUnitCurrent == DisplayUnit.KG)
                return value;
            else
                return value / WeightConversion;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            TaskTools.RunLogged(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        public virtual void NotifyDisplayUnit()
        {
            NotifyPropertyChanged(nameof(DisplayUnitCurrent));
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
        }

        public virtual void EvaluateDisplayUnit()
        {
            if (DisplayUnitSource == DisplayUnitSource.App && DisplayUnitCurrent != DisplayUnitDefault)
            {
                DisplayUnitCurrent = DisplayUnitDefault;
                NotifyDisplayUnit();
            }
            else if (AppService.Instance?.SimConnect?.IsSessionRunning == true)
            {
                if (DisplayUnitSource == DisplayUnitSource.Aircraft && AppService.Instance?.AircraftController?.Aircraft?.IsConnected == true
                    && AppService.Instance?.AircraftController?.Aircraft.UnitAircraft != DisplayUnitCurrent)
                {
                    DisplayUnitCurrent = AppService.Instance.AircraftController.Aircraft.UnitAircraft;
                    NotifyDisplayUnit();
                }
                else if (DisplayUnitSource == DisplayUnitSource.Simbrief && AppService.Instance?.Flightplan?.IsLoaded == true
                        && AppService.Instance?.Flightplan?.Unit != DisplayUnitCurrent)
                {
                    DisplayUnitCurrent = AppService.Instance.Flightplan.Unit;
                    NotifyDisplayUnit();
                }
            }
            else if (AppService.Instance?.SimConnect?.IsSessionRunning == false && DisplayUnitCurrent != DisplayUnitDefault)
            {
                DisplayUnitCurrent = DisplayUnitDefault;
                NotifyDisplayUnit();
            }
        }

        public virtual void SetFuelFob(string title, double fuelKg)
        {
            if (FuelFobSaved.ContainsKey(title))
                FuelFobSaved[title] = fuelKg;
            else
                FuelFobSaved.Add(title, fuelKg);

            SaveConfiguration();
            Logger.Debug($"Saved Fuel for '{title}': {fuelKg}kg");
        }

        public virtual double GetFuelFob(string title, double fuelCapacityKg, double resetBaseKg, out bool saved)
        {
            if (!string.IsNullOrWhiteSpace(title) && FuelFobSaved.TryGetValue(title, out double fuelKg))
            {
                Logger.Debug($"Found saved Fuel for '{title}'");
                saved = true;
                return fuelKg;
            }
            else
            {
                fuelKg = CalcFuelValueKg(fuelCapacityKg, resetBaseKg);
                Logger.Debug($"No saved Fuel found for '{title}' - using default Calculation: {fuelKg}kg");
                saved = false;
                return fuelKg;
            }
        }

        public virtual double CalcFuelValueKg(double fuelCapacityKg, double resetBaseKg)
        {
            double fuelKg = resetBaseKg + (fuelCapacityKg * FuelResetPercent);

            return fuelKg;
        }
    }
}
