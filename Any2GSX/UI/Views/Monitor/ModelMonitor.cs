using Any2GSX.AppConfig;
using Any2GSX.Audio;
using Any2GSX.GSX;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Monitor
{
    public partial class ModelMonitor(AppService source) : ModelBase<AppService>(source, source)
    {
        protected virtual DispatcherTimer UpdateTimer { get; set; }
        protected virtual bool ForceRefresh { get; set; } = false;
        protected static SolidColorBrush ColorValid { get; } = new(Colors.Green);
        protected static SolidColorBrush ColorInvalid { get; } = new(Colors.Red);
        public virtual ObservableCollection<string> MessageLog { get; } = [];
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController?.GsxServices;
        protected virtual GsxServiceBoarding GsxServiceBoard => GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
        protected virtual GsxServiceDeboarding GsxServiceDeboard => GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding;
        protected virtual GsxServicePushback GsxServicePushBack => GsxServices[GsxServiceType.Pushback] as GsxServicePushback;

        protected override void InitializeModel()
        {
            UpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(AppService.Instance.Config.UiRefreshInterval),
            };
            UpdateTimer.Tick += OnUpdate;
        }

        public virtual void Start()
        {
            ForceRefresh = true;
            UpdateTimer.Start();
        }

        public virtual void Stop()
        {
            UpdateTimer?.Stop();
        }

        [RelayCommand]
        public virtual void LogDir()
        {
            try { Process.Start(new ProcessStartInfo(Path.Join(Config.Definition.ProductPath, Config.Definition.ProductLogPath)) { UseShellExecute = true }); } catch { }
        }

        [RelayCommand]
        public virtual void ShowFlightplanInfo()
        {
            if (AppService.Instance?.Flightplan?.Id > 0)
            {
                var window = new ShowInfoDialog(string.Join("\r\n", AppService.Instance.Flightplan.GetInfoStrings()), true);
                window.Show();
            }
        }

        protected virtual void UpdateBoolState(string propertyValue, string propertyColor, bool value, bool reverseColor = false)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (this.GetPropertyValue<bool>(propertyValue) != value || ForceRefresh)
                {
                    this.SetPropertyValue<bool>(propertyValue, value);
                    UpdateColor(propertyColor, value, reverseColor);
                }
            }
            catch { }
        }

        protected virtual void UpdateColor(string propertyColor, bool state, bool reverseColor = false)
        {
            try
            {
                if (reverseColor)
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorInvalid : ColorValid);
                else
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorValid : ColorInvalid);
            }
            catch { }
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

        protected virtual void OnUpdate(object? sender, EventArgs e)
        {
            try { UpdateSim(); } catch (Exception ex) { Logger.LogException(ex); }
            try { UpdateGsx(); } catch { }
            try { UpdateApp(); } catch (Exception ex) { Logger.LogException(ex); }
            try { UpdateLog(); } catch { }
            try { UpdateAircraftPlugin(); } catch { }
            ForceRefresh = false;
        }

        protected virtual void UpdateSim()
        {
            UpdateBoolState(nameof(SimRunning), nameof(SimRunningColor), SimConnectController.IsSimRunning);
            UpdateBoolState(nameof(SimConnected), nameof(SimConnectedColor), SimConnect.IsSimConnected);
            UpdateBoolState(nameof(SimSession), nameof(SimSessionColor), SimConnect.IsSessionRunning && !SimConnect.IsSessionStopped);

            UpdateBoolState(nameof(SimPaused), nameof(SimPausedColor), SimConnect.IsPaused, true);
            UpdateBoolState(nameof(SimWalkaround), nameof(SimWalkaroundColor), GsxController?.IsWalkaround ?? false, true);
            UpdateState<long>(nameof(CameraState), SimConnect.CameraState);

            UpdateState<string>(nameof(SimVersion), SimConnect.SimVersionString);
        }

        [ObservableProperty]
        public partial bool SimRunning { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimRunningColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimConnected { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimConnectedColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimSession { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimSessionColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimPaused { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimPausedColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool SimWalkaround { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush SimWalkaroundColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial long CameraState { get; set; } = 0;

        [ObservableProperty]
        public partial string SimVersion { get; set; } = "";

        protected virtual void UpdateGsx()
        {
            UpdateBoolState(nameof(GsxRunning), nameof(GsxRunningColor), GsxController.CheckBinaries());
            UpdateState<string>(nameof(GsxStarted), $"{GsxController?.CouatlLastStarted ?? 0} | {GsxController?.CouatlLastProgress ?? 100}");
            UpdateColor(nameof(GsxStartedColor), GsxController.CouatlVarsValid);
            UpdateState<GsxMenuState>(nameof(GsxMenu), GsxController.Menu.MenuState);
            UpdateState<string>(nameof(GsxPaxTarget), $"{GsxServiceBoard?.SubPaxTarget?.GetValue<int>() ?? 0}  ({GsxController?.SubPilotTarget?.GetValue<int>() ?? 0} | {GsxController?.SubCrewTarget?.GetValue<int>() ?? 0})");
            UpdateState<string>(nameof(GsxPaxTotal), $"{GsxServiceBoard?.SubPaxTotal?.GetValue<int>() ?? 0} | {GsxServiceDeboard?.SubPaxTotal?.GetValue<int>() ?? 0}");
            UpdateState<string>(nameof(GsxCargoProgress), $"{GsxServiceBoard?.SubCargoPercent?.GetValue<int>() ?? 0} | {GsxServiceDeboard?.SubCargoPercent?.GetValue<int>() ?? 0}");

            UpdateState<GsxServiceState>(nameof(ServiceReposition), GsxServices[GsxServiceType.Reposition].State);
            UpdateState<GsxServiceState>(nameof(ServiceRefuel), GsxServices[GsxServiceType.Refuel].State);
            UpdateState<GsxServiceState>(nameof(ServiceCatering), GsxServices[GsxServiceType.Catering].State);
            UpdateState<GsxServiceState>(nameof(ServiceLavatory), GsxServices[GsxServiceType.Lavatory].State);
            UpdateState<GsxServiceState>(nameof(ServiceWater), GsxServices[GsxServiceType.Water].State);
            UpdateState<GsxServiceState>(nameof(ServiceCleaning), GsxServices[GsxServiceType.Cleaning].State);
            UpdateState<GsxServiceState>(nameof(ServiceGpu), GsxServices[GsxServiceType.GPU].State);

            UpdateState<GsxServiceState>(nameof(ServiceBoarding), GsxServices[GsxServiceType.Boarding].State);
            UpdateState<GsxServiceState>(nameof(ServiceDeboarding), GsxServices[GsxServiceType.Deboarding].State);
            UpdateState<string>(nameof(ServicePushback), $"{GsxServicePushBack.TextState} ({GsxServicePushBack.PushStatus})");
            UpdateState<string>(nameof(ServiceJetway), $"{GsxController.ServiceJetway.TextState} ({(int)GsxController.ServiceJetway.OperatingState})");
            UpdateState<string>(nameof(ServiceStairs), $"{GsxController.ServiceStairs.TextState} ({(int)GsxController.ServiceStairs.OperatingState})");
        }

        [ObservableProperty]
        public partial bool GsxRunning { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush GsxRunningColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial string GsxStarted { get; set; } = "";

        [ObservableProperty]
        public partial SolidColorBrush GsxStartedColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial GsxMenuState GsxMenu { get; set; } = GsxMenuState.UNKNOWN;

        [ObservableProperty]
        public partial string GsxPaxTarget { get; set; } = "0 (0 | 0)";

        [ObservableProperty]
        public partial string GsxPaxTotal { get; set; } = "0 | 0";

        [ObservableProperty]
        public partial string GsxCargoProgress { get; set; } = "0 | 0";

        [ObservableProperty]
        public partial GsxServiceState ServiceReposition { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceRefuel { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceCatering { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceLavatory { get; set; } = GsxServiceState.Unknown;
        [ObservableProperty]
        public partial GsxServiceState ServiceWater { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceCleaning { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceGpu { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceBoarding { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial GsxServiceState ServiceDeboarding { get; set; } = GsxServiceState.Unknown;

        [ObservableProperty]
        public partial string ServicePushback { get; set; } = $"{GsxServiceState.Unknown} (0)";

        [ObservableProperty]
        public partial string ServiceJetway { get; set; } = GsxServiceState.Unknown.ToString();

        [ObservableProperty]
        public partial string ServiceStairs { get; set; } = GsxServiceState.Unknown.ToString();

        protected virtual void UpdateApp()
        {
            UpdateBoolState(nameof(AppGsxController), nameof(AppGsxControllerColor), GsxController.IsActive);
            UpdateBoolState(nameof(AppAircraftInterface), nameof(AppAircraftInterfaceColor), AircraftController?.IsConnected == true);
            UpdateBoolState(nameof(AppAutomationController), nameof(AppAutomationControllerColor), AutomationController.IsStarted);
            UpdateBoolState(nameof(AppAudioController), nameof(AppAudioControllerColor), AudioController.IsActive);
            UpdateBoolState(nameof(AppDeckController), nameof(AppDeckControllerColor), Source.NotificationManager.IsRunning);

            UpdateState<AutomationState>(nameof(AppAutomationState), AutomationController?.State ?? AutomationState.SessionStart);
            UpdateState<string>(nameof(AppSmartCall), $"{Source?.NotificationTracker?.SmartButton ?? SmartButtonCall.None}");
            var queue = Source?.GsxController?.AutomationController?.DepartureQueue;
            UpdateState<string>(nameof(AppAutomationDepartureServices), $"{queue?.CountCompleted ?? 0} / {queue?.CountRunning ?? 0} / {queue?.CountTotal ?? 0}" ?? "0 / 0 / 0");
            if (queue?.HasNext == true)
                UpdateState<string>(nameof(AppAutomationNextService), $"{queue?.NextType ?? GsxServiceType.Unknown}");
            else
                UpdateState<string>(nameof(AppAutomationNextService), "");
            UpdateState<string>(nameof(AppFlightPlan), $"{AppService.Instance?.Flightplan?.Id ?? 0}");

            UpdateState<string>(nameof(AppProfile), SettingProfile?.Name ?? "");
            UpdateState<string>(nameof(AppPlugin), $"{AircraftController?.PluginId ?? ""} {(!string.IsNullOrWhiteSpace(AircraftController?.Aircraft?.GetType()?.Name) ? $"[ {AircraftController?.Aircraft?.GetType()?.Name} ]" : "")}");
        }

        [ObservableProperty]
        public partial bool AppGsxController { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush AppGsxControllerColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool AppAircraftInterface { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush AppAircraftInterfaceColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool AppAutomationController { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush AppAutomationControllerColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool AppAudioController { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush AppAudioControllerColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial bool AppDeckController { get; set; } = false;

        [ObservableProperty]
        public partial SolidColorBrush AppDeckControllerColor { get; set; } = ColorInvalid;

        [ObservableProperty]
        public partial AutomationState AppAutomationState { get; set; } = AutomationState.SessionStart;

        [ObservableProperty]
        public partial string AppSmartCall { get; set; } = "";

        [ObservableProperty]
        public partial string AppAutomationDepartureServices { get; set; } = "0 / 0 / 0";

        [ObservableProperty]
        public partial string AppAutomationNextService { get; set; } = GsxServiceType.Unknown.ToString();

        [ObservableProperty]
        public partial string AppFlightPlan { get; set; } = "0";

        [ObservableProperty]
        public partial string AppProfile { get; set; } = "";

        [ObservableProperty]
        public partial string AppPlugin { get; set; } = "";

        protected virtual void UpdateAircraftPlugin()
        {
            try
            {
                UpdateState<bool>(nameof(AppOnGround), GsxController?.IsOnGround ?? true);
                UpdateState<bool>(nameof(PluginAvionics), AutomationController?.EquipManager?.AvionicsPowered ?? false);
                UpdateState<bool>(nameof(PluginExtCon), AutomationController?.EquipManager?.PowerConnected ?? false);
                UpdateState<bool>(nameof(PluginGpu), AutomationController?.EquipManager?.EquipmentGpu ?? false);
                UpdateState<bool>(nameof(PluginChocks), AutomationController?.EquipManager?.EquipmentChocks ?? false);
                UpdateState<bool>(nameof(PluginCones), AutomationController?.EquipManager?.EquipmentCones ?? false);
                UpdateState<bool>(nameof(PluginPca), AutomationController?.EquipManager?.EquipmentPca ?? false);
                UpdateState<bool>(nameof(PluginApuRun), AutomationController?.EquipManager?.ApuRunning ?? false);
                UpdateState<bool>(nameof(PluginApuBleed), AutomationController?.EquipManager?.ApuBleed ?? false);
                UpdateState<bool>(nameof(PluginBrake), AutomationController?.EquipManager?.BrakeSet ?? false);
                UpdateState<bool>(nameof(PluginBeacon), AutomationController?.LightBeacon ?? false);
                UpdateState<bool>(nameof(PluginNav), AutomationController?.LightNav ?? false);
                UpdateState<bool>(nameof(PluginEngineRunning), AutomationController?.EnginesRunning ?? false);
                UpdateState<bool>(nameof(PluginSmartButton), AutomationController?.HasSmartButtonRequest ?? false);
                UpdateState<int>(nameof(PluginSpeed), (int)(AutomationController?.Speed ?? 0));
                UpdateState<bool>(nameof(PluginCargo), AutomationController?.IsCargo ?? false);
                UpdateState<bool>(nameof(PluginReadyDeparture), AutomationController?.ReadyDepartureServices ?? false);

                UpdateState<string>(nameof(PluginZeroFuel), $"{Math.Round(Config.ConvertKgToDisplayUnit(AutomationController?.ZeroFuel ?? 0), 1)} {Config.DisplayUnitCurrentString}");
                UpdateState<string>(nameof(PluginFuelOnBoard), $"{Math.Round(Config.ConvertKgToDisplayUnit(AutomationController?.FuelOnBoard ?? 0), 1)} {Config.DisplayUnitCurrentString}");
                UpdateState<string>(nameof(PluginTotalWeight), $"{Math.Round(Config.ConvertKgToDisplayUnit(AutomationController?.WeightTotal ?? 0), 1)} {Config.DisplayUnitCurrentString}");
            }
            catch { }
        }

        [ObservableProperty]
        public partial bool AppOnGround { get; set; } = true;

        [ObservableProperty]
        public partial bool PluginAvionics { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginExtCon { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginCones { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginChocks { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginGpu { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginPca { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginApuRun { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginApuBleed { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginBrake { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginBeacon { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginNav { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginEngineRunning { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginSmartButton { get; set; } = false;

        [ObservableProperty]
        public partial int PluginSpeed { get; set; } = 0;

        [ObservableProperty]
        public partial bool PluginCargo { get; set; } = false;

        [ObservableProperty]
        public partial bool PluginReadyDeparture { get; set; } = false;

        [ObservableProperty]
        public partial string PluginZeroFuel { get; set; } = "0.0";

        [ObservableProperty]
        public partial string PluginFuelOnBoard { get; set; } = "0.0";

        [ObservableProperty]
        public partial string PluginTotalWeight { get; set; } = "0.0";

        protected virtual void UpdateLog()
        {
            if (Logger.Messages.IsEmpty)
                NotifyPropertyChanged(nameof(MessageLog));

            while (!Logger.Messages.IsEmpty)
            {
                MessageLog.Add(Logger.Messages.Dequeue());
                if (MessageLog.Count > 12)
                    MessageLog.RemoveAt(0);
            }
        }
    }
}
