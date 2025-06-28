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
        protected virtual GsxAutomationController AutomationController => GsxController?.AutomationController;
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

            UpdateState<string>(nameof(AircraftString), SimConnect.AircraftString);
        }

        [ObservableProperty]
        protected bool _SimRunning = false;
        [ObservableProperty]
        protected SolidColorBrush _SimRunningColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimConnected = false;
        [ObservableProperty]
        protected SolidColorBrush _SimConnectedColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimSession = false;
        [ObservableProperty]
        protected SolidColorBrush _SimSessionColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimPaused = false;
        [ObservableProperty]
        protected SolidColorBrush _SimPausedColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimWalkaround = false;
        [ObservableProperty]
        protected SolidColorBrush _SimWalkaroundColor = ColorInvalid;

        [ObservableProperty]
        protected long _CameraState = 0;

        [ObservableProperty]
        protected string _SimVersion = "";

        [ObservableProperty]
        protected string _AircraftString = "";

        protected virtual void UpdateGsx()
        {
            UpdateBoolState(nameof(GsxRunning), nameof(GsxRunningColor), GsxController.CheckBinaries());
            UpdateState<string>(nameof(GsxStarted), $"{GsxController?.CouatlLastStarted ?? 0} | {GsxController?.CouatlLastProgress ?? 0}");
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
            UpdateState<GsxServiceState>(nameof(ServiceGpu), GsxServices[GsxServiceType.GPU].State);

            UpdateState<GsxServiceState>(nameof(ServiceBoarding), GsxServices[GsxServiceType.Boarding].State);
            UpdateState<GsxServiceState>(nameof(ServiceDeboarding), GsxServices[GsxServiceType.Deboarding].State);
            UpdateState<string>(nameof(ServicePushback), $"{GsxServicePushBack.State} ({GsxServicePushBack.PushStatus})");
            UpdateState<string>(nameof(ServiceJetway), $"{GsxController.ServiceJetway.State} ({GsxController.ServiceJetway.SubOperating.GetNumber()})");
            UpdateState<string>(nameof(ServiceStairs), $"{GsxController.ServiceStairs.State} ({GsxController.ServiceStairs.SubOperating.GetNumber()})");
        }

        [ObservableProperty]
        protected bool _GsxRunning = false;
        [ObservableProperty]
        protected SolidColorBrush _GsxRunningColor = ColorInvalid;

        [ObservableProperty]
        protected string _GsxStarted = "";
        [ObservableProperty]
        protected SolidColorBrush _GsxStartedColor = ColorInvalid;

        [ObservableProperty]
        protected GsxMenuState _GsxMenu = GsxMenuState.UNKNOWN;

        [ObservableProperty]
        protected string _GsxPaxTarget = "0 (0 | 0)";

        [ObservableProperty]
        protected string _GsxPaxTotal = "0 | 0";

        [ObservableProperty]
        protected string _GsxCargoProgress = "0 | 0";

        [ObservableProperty]
        protected GsxServiceState _ServiceReposition = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceRefuel = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceCatering = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceLavatory = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceWater = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceGpu = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceBoarding = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceDeboarding = GsxServiceState.Unknown;

        [ObservableProperty]
        protected string _ServicePushback = $"{GsxServiceState.Unknown} (0)";

        [ObservableProperty]
        protected string _ServiceJetway = GsxServiceState.Unknown.ToString();

        [ObservableProperty]
        protected string _ServiceStairs = GsxServiceState.Unknown.ToString();

        protected virtual void UpdateApp()
        {
            UpdateBoolState(nameof(AppGsxController), nameof(AppGsxControllerColor), GsxController.IsActive);
            UpdateBoolState(nameof(AppAircraftInterface), nameof(AppAircraftInterfaceColor), AircraftController?.IsConnected == true);
            UpdateBoolState(nameof(AppAutomationController), nameof(AppAutomationControllerColor), AutomationController.IsStarted);
            UpdateBoolState(nameof(AppAudioController), nameof(AppAudioControllerColor), AudioController.IsActive);
            UpdateBoolState(nameof(AppDeckController), nameof(AppDeckControllerColor), Source.NotificationManager.IsRunning);

            UpdateState<AutomationState>(nameof(AppAutomationState), AutomationController?.State ?? AutomationState.SessionStart);
            UpdateState<string>(nameof(AppSmartCall), $"{Source?.NotificationManager?.ReportedCall ?? SmartButtonCall.None}{(!string.IsNullOrWhiteSpace(Source?.NotificationManager?.ReportedCallInfo) ? $" ({Source?.NotificationManager?.ReportedCallInfo})" : "")}");
            UpdateState<string>(nameof(AppAutomationDepartureServices), $"{Source?.NotificationManager?.ReportedServicesCompleted ?? 0} / {Source?.NotificationManager?.ReportedServicesRunning ?? 0} / {Source?.NotificationManager?.ReportedServicesTotal ?? 0}" ?? "0 / 0 / 0");
            if (AutomationController?.DepartureServicesEnumerator?.CheckEnumeratorValid() == true)
                UpdateState<string>(nameof(AppAutomationNextService), $"{AutomationController?.DepartureServicesCurrent?.ServiceType ?? GsxServiceType.Unknown}");
            else
                UpdateState<string>(nameof(AppAutomationNextService), "");
            UpdateState<string>(nameof(AppFlightPlan), $"{AppService.Instance?.Flightplan?.Id ?? 0}");

            UpdateState<string>(nameof(AppProfile), SettingProfile?.Name ?? "");
            UpdateState<string>(nameof(AppPlugin), $"{AircraftController?.PluginId ?? ""} {(!string.IsNullOrWhiteSpace(AircraftController?.Aircraft?.GetType()?.Name) ? $"[ {AircraftController?.Aircraft?.GetType()?.Name} ]" : "")}");
        }

        [ObservableProperty]
        protected bool _AppGsxController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppGsxControllerColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAircraftInterface = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAircraftInterfaceColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAutomationController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAutomationControllerColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAudioController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAudioControllerColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppDeckController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppDeckControllerColor = ColorInvalid;

        [ObservableProperty]
        protected AutomationState _AppAutomationState = AutomationState.SessionStart;

        [ObservableProperty]
        protected string _AppSmartCall = "";

        [ObservableProperty]
        protected string _AppAutomationDepartureServices = "0 / 0 / 0";

        [ObservableProperty]
        protected string _AppAutomationNextService = GsxServiceType.Unknown.ToString();

        [ObservableProperty]
        protected string _AppFlightPlan = "0";


        [ObservableProperty]
        protected string _AppProfile = "";

        [ObservableProperty]
        protected string _AppPlugin = "";

        protected virtual void UpdateAircraftPlugin()
        {
            try
            {
                UpdateState<bool>(nameof(AppOnGround), GsxController?.IsOnGround ?? true);
                UpdateState<bool>(nameof(PluginAvionics), AircraftController?.Aircraft?.IsAvionicPowered ?? false);
                UpdateState<bool>(nameof(PluginExtCon), AircraftController?.Aircraft?.IsExternalPowerConnected ?? false);
                UpdateState<bool>(nameof(PluginGpu), AircraftController?.Aircraft?.EquipmentPower ?? false);
                UpdateState<bool>(nameof(PluginChocks), AircraftController?.Aircraft?.EquipmentChocks ?? false);
                UpdateState<bool>(nameof(PluginCones), AircraftController?.Aircraft?.EquipmentCones ?? false);
                UpdateState<bool>(nameof(PluginPca), AircraftController?.Aircraft?.EquipmentPca ?? false);
                UpdateState<bool>(nameof(PluginApuRun), AircraftController?.Aircraft?.IsApuRunning ?? false);
                UpdateState<bool>(nameof(PluginApuBleed), AircraftController?.Aircraft?.IsApuBleedOn ?? false);
                UpdateState<bool>(nameof(PluginBrake), AircraftController?.Aircraft?.IsBrakeSet ?? false);
                UpdateState<bool>(nameof(PluginBeacon), AircraftController?.Aircraft?.LightBeacon ?? false);
                UpdateState<bool>(nameof(PluginNav), AircraftController?.Aircraft?.LightNav ?? false);
                UpdateState<bool>(nameof(PluginEngineRunning), AircraftController?.Aircraft?.IsEngineRunning ?? false);
                UpdateState<bool>(nameof(PluginSmartButton), AircraftController?.Aircraft?.SmartButtonRequest ?? false);
                UpdateState<int>(nameof(PluginSpeed), AircraftController?.Aircraft?.GroundSpeed ?? 0);
                UpdateState<bool>(nameof(PluginCargo), AircraftController?.Aircraft?.IsCargo ?? false);

                UpdateState<string>(nameof(PluginFuelCapacity), $"{Math.Round(Config.ConvertKgToDisplayUnit(AircraftController?.FuelCapacityKg ?? 0), 1)} {Config.DisplayUnitCurrentString}");
                UpdateState<string>(nameof(PluginFuelOnBoard), $"{Math.Round(Config.ConvertKgToDisplayUnit(AircraftController?.Aircraft?.FuelOnBoardKg ?? 0), 1)} {Config.DisplayUnitCurrentString}");
                UpdateState<string>(nameof(PluginTotalWeight), $"{Math.Round(Config.ConvertKgToDisplayUnit(AircraftController?.Aircraft?.WeightTotalKg ?? 0), 1)} {Config.DisplayUnitCurrentString}");
            }
            catch { }
        }

        [ObservableProperty]
        protected bool _AppOnGround = true;

        [ObservableProperty]
        protected bool _PluginAvionics = false;

        [ObservableProperty]
        protected bool _PluginExtCon = false;

        [ObservableProperty]
        protected bool _PluginCones = false;

        [ObservableProperty]
        protected bool _PluginChocks = false;

        [ObservableProperty]
        protected bool _PluginGpu = false;

        [ObservableProperty]
        protected bool _PluginPca = false;

        [ObservableProperty]
        protected bool _PluginApuRun = false;

        [ObservableProperty]
        protected bool _PluginApuBleed = false;

        [ObservableProperty]
        protected bool _PluginBrake = false;

        [ObservableProperty]
        protected bool _PluginBeacon = false;

        [ObservableProperty]
        protected bool _PluginNav = false;

        [ObservableProperty]
        protected bool _PluginEngineRunning = false;

        [ObservableProperty]
        protected bool _PluginSmartButton = false;

        [ObservableProperty]
        protected int _PluginSpeed = 0;

        [ObservableProperty]
        protected bool _PluginCargo = false;

        [ObservableProperty]
        protected string _PluginFuelCapacity = "0.0";

        [ObservableProperty]
        protected string _PluginFuelOnBoard = "0.0";

        [ObservableProperty]
        protected string _PluginTotalWeight = "0.0";

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
