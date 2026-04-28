using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.Audio;
using Any2GSX.CommBus;
using Any2GSX.GSX;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Plugins;
using Any2GSX.Tools;
using CFIT.AppFramework.Messages;
using CFIT.AppFramework.Services;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Any2GSX
{
    public enum AppResetRequest
    {
        None = 0,
        App = 1,
        AppGsx = 2,
    }

    public class AppService : AppService<Any2GSX, AppService, Config, Definition>, IAppResources
    {
        public virtual IConfig AppConfig => Config;
        public virtual IProductDefinition ProductDefinition => Definition;
        public virtual CancellationTokenSource RequestTokenSource { get; protected set; }
        public virtual CancellationToken RequestToken { get; protected set; }
        public virtual ModuleCommBus CommBus { get; protected set; }
        public virtual ICommBus ICommBus => CommBus;
        public virtual InputEventManager InputEventManager => SimConnect.InputManager;
        public virtual IWeightConverter WeightConverter { get; } = new WeightConverter();
        public virtual double FuelWeightKgPerGallon => AircraftController?.FuelWeightKgPerGallon ?? 3.03907;
        public virtual Flightplan Flightplan { get; set; }
        public virtual IFlightplan IFlightplan => Flightplan;
        public virtual NotificationManager NotificationManager { get; protected set; }
        public virtual NotificationTracker NotificationTracker { get; protected set; }
        public virtual PluginController PluginController { get; protected set; }
        public virtual IGsxController IGsxController => GsxController;
        public virtual GsxController GsxController { get; protected set; }
        public virtual DateTime LastGsxRestart { get; protected set; } = DateTime.MinValue;
        public virtual AircraftController AircraftController { get; protected set; }
        public virtual string AircraftString => SimConnect?.AircraftString ?? "";
        public virtual bool IsMsfs2020 => SimService?.Manager?.GetSimVersion() == SimVersion.MSFS2020;
        public virtual bool IsMsfs2024 => SimService?.Manager?.GetSimVersion() == SimVersion.MSFS2024;
        public virtual AudioController AudioController { get; protected set; }
        public virtual ISettingProfile ISettingProfile => SettingProfile;
        public virtual bool IsProfileLoaded => SettingProfile != null;
        public virtual SettingProfile SettingProfile { get; protected set; } = null;
        public virtual string ProfileName => SettingProfile?.Name ?? "NULL";
        public virtual PluginCapabilities PluginCapabilities { get; protected set; } = new();
        public virtual ApiController ApiController { get; protected set; }
        public virtual AppResetRequest ResetRequested { get; set; } = AppResetRequest.None;
        protected virtual bool IsSessionInitialized { get; set; } = false;
        protected virtual DateTime NextGarbageCollection { get; set; } = DateTime.Now + TimeSpan.FromSeconds(300);

        public virtual string AircraftAtcAirline { get; protected set; } = "";
        public virtual string AircraftAtcId { get; protected set; } = "";
        public virtual string AircraftTitle { get; protected set; } = "";


        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<SettingProfile> ProfileChanged;
        public event Action<PluginCapabilities> PluginCapabilitiesChanged;
        public event Action ProfileCollectionChanged;

        public AppService(Config config) : base(config)
        {
            RefreshToken();
        }

        protected virtual void RefreshToken()
        {
            RequestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Any2GSX.Instance.Token);
            RequestToken = RequestTokenSource.Token;
        }

        protected override void CreateServiceControllers()
        {
            CommBus = SimConnect.AddModule(typeof(ModuleCommBus), Config) as ModuleCommBus;

            ApiController = new(Config);
            ServiceControllers.Add(ApiController);

            Flightplan = new();
            NotificationManager = new();
            NotificationTracker = new();
            PluginController = new();
            GsxController = new GsxController(Config);
            AircraftController = new AircraftController(Config);
            AudioController = new AudioController(Config);
        }

        protected override async Task DoInit()
        {
            await base.DoInit();
            MessageService.Subscribe<MsgSessionReady>(SessionInitialize);
            MessageService.Subscribe<MsgSessionEnded>(SessionCleanup);

            SimService.Controller.SimConnect.CallbackAircraftString += OnAircraftStringChanged;
            SimStore.AddVariable("ATC AIRLINE", SimUnitType.String).OnReceived += OnAtcAirlineChanged;
            if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
                SimStore.AddVariable("LIVERY NAME", SimUnitType.String).OnReceived += OnAtcTitleChanged;
            else
                SimStore.AddVariable("TITLE", SimUnitType.String).OnReceived += OnAtcTitleChanged;
            SimStore.AddVariable("ATC ID", SimUnitType.String).OnReceived += OnAtcIdChanged;

            PluginController.Refresh();
            Config.InhibitSave = true;
            Logger.Debug($"Fetching Setting Profile");
            SetSettingProfile();
            Config.InhibitSave = false;
        }

        public virtual Task OnAtcAirlineChanged(ISimResourceSubscription sub, object data)
        {
            AircraftAtcAirline = (string)data;
            NotifyPropertyChanged(nameof(AircraftAtcAirline));
            return Task.CompletedTask;
        }

        public virtual Task OnAtcTitleChanged(ISimResourceSubscription sub, object data)
        {
            AircraftTitle = (string)data;
            NotifyPropertyChanged(nameof(AircraftTitle));
            return Task.CompletedTask;
        }

        public virtual void OnAircraftStringChanged(SimConnectManager manager, string aircraft)
        {
            NotifyPropertyChanged(nameof(AircraftString));
        }

        public virtual Task OnAtcIdChanged(ISimResourceSubscription sub, object data)
        {
            AircraftAtcId = (string)data;
            NotifyPropertyChanged(nameof(AircraftAtcId));
            return Task.CompletedTask;
        }

        public virtual void NotifyPluginCapabilitiesChanged()
        {
            PluginCapabilities = PluginController.GetPluginCapabilities(SettingProfile.PluginId);
            ModelHelper.RunOnDispatcher(() => PluginCapabilitiesChanged?.Invoke(PluginCapabilities));
        }

        public virtual void NotifyAircraftChannelsChanged()
        {
            if (AudioController.IsActive)
                AudioController.ResetChannels = true;
        }

        public virtual void NotifyProfileCollectionChanged()
        {
            if (ProfileCollectionChanged != null)
                ModelHelper.RunOnDispatcher(() => ProfileCollectionChanged?.Invoke());
        }

        public virtual void NotifyProfileChanged()
        {
            if (ProfileChanged != null)
                ModelHelper.RunOnDispatcher(() => ProfileChanged?.Invoke(SettingProfile));
        }

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            ModelHelper.RunOnDispatcher(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        public virtual void AddSettingProfile(SettingProfile settingProfile)
        {
            if (settingProfile == null)
                return;

            Config.SettingProfiles.Add(settingProfile);
            Config.SortProfiles();
            NotifyProfileCollectionChanged();
        }

        public virtual bool RemoveSettingProfile(SettingProfile settingProfile)
        {
            if (settingProfile == null || settingProfile.IsReadOnly)
                return false;

            if (Config.SettingProfiles.Any((p) => p.Name.Equals(settingProfile.Name)))
            {
                Config.SettingProfiles.RemoveAll((p) => p.Name.Equals(settingProfile.Name));
                Config.SaveConfiguration();
                NotifyProfileCollectionChanged();
                if (!Config.SettingProfiles.Any(p => p.Name == SettingProfile?.Name))
                    SetSettingProfile();
                return true;
            }
            else
                return false;
        }

        public virtual void RenameSettingProfile(SettingProfile settingProfile, string name)
        {
            if (settingProfile == null)
                return;

            bool isActive = settingProfile.Name == SettingProfile.Name;
            settingProfile.Name = name;
            Config.SortProfiles();
            NotifyProfileCollectionChanged();
            if (isActive)
                NotifyPropertyChanged(nameof(ProfileName));
        }

        public virtual void UpdateSettingProfile(SettingProfile settingProfile, SettingProfile newData)
        {
            bool isActive = settingProfile.Name == SettingProfile.Name;
            settingProfile.Copy(newData);
            Config.SortProfiles();
            if (isActive)
                NotifyPropertyChanged(nameof(ProfileName));
        }

        public virtual void SetSettingProfile(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                SettingProfile = Config.SettingProfiles.First(p => p.Name == name);
                SettingProfile.Load();
                Logger.Debug($"Using Profile {SettingProfile}");
                PluginCapabilities = PluginController.GetPluginCapabilities(SettingProfile.PluginId);
                NotifyProfileChanged();
                NotifyPluginCapabilitiesChanged();
                NotifyPropertyChanged(nameof(ProfileName));
                NotifyPropertyChanged(nameof(SettingProfile));

                if (IsSessionInitialized)
                    ReloadAircraft();
            }
        }

        public virtual void ReloadAircraft()
        {
            if (!IsSessionInitialized)
                return;

            _ = TaskTools.RunDelayed(AircraftController.Restart, 1000, Token);
            if (AudioController.IsActive)
                _ = TaskTools.RunDelayed(() => AudioController.ResetChannels = true, 1500, Token);
        }

        public virtual void SetSettingProfile()
        {
            SetSettingProfile(SearchSettingProfile()?.Name);
        }

        public virtual SettingProfile SearchSettingProfile()
        {
            SettingProfile profile = null;

            profile ??= SearchSettingProfile(Config.SettingProfiles);
            profile ??= Config.SettingProfiles.Where(p => p.IsDefault).First() ?? new SettingProfile() { IsReadOnly = true };

            Logger.Information($"Matched Setting Profile: {profile}");
            return Config.CheckServices(profile);
        }

        protected virtual SettingProfile SearchSettingProfile(IEnumerable<SettingProfile> settingProfiles)
        {
            Logger.Debug($"Matching Profiles ...");
            foreach (var profile in settingProfiles)
            {
                profile.Match(this);
                Logger.Debug($"Profile '{profile.Name}' Score: {profile.MatchingScore}");
            }

            var maxProfile = settingProfiles.MaxBy(p => p.MatchingScore);
            if (maxProfile?.MatchingScore > 0)
                return maxProfile;
            else
                return null;
        }

        protected virtual async Task SessionInitialize()
        {
            try
            {
                if (IsSessionInitialized)
                    return;

                if (string.IsNullOrWhiteSpace(Config?.SimbriefUser))
                {
                    MessageBox.Show(Any2GSX.Instance.AppWindow, "SimBrief User is not set!\r\nConfigure your User Name or ID in the App Settings.", "SimBrief User", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Logger.Debug($"Refresh Token");
                RefreshToken();

                if (Config.SessionInitDelayMs > 0)
                {
                    Logger.Information($"Delaying Session Init by {Config.SessionInitDelayMs}ms ...");
                    await Task.Delay(Config.SessionInitDelayMs, RequestToken);
                }
                Logger.Information("Initializing Sim Session");

                Logger.Debug($"Reset CommBus");
                await CommBus.Reset();
                await Task.Delay(750, Token);

                Logger.Debug($"Waiting for CommBus Module ...");
                while (!CommBus.IsConnected && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                    await Task.Delay(Config.CheckInterval, Token);
                Logger.Debug($"CommBus Module connected");
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                Logger.Debug($"Fetching Setting Profile");
                SetSettingProfile();
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                Logger.Debug($"Reset Notification Tracker");
                NotificationTracker.Reset();

                Logger.Debug($"Start GSX Controller");
                await GsxController.Start();
                Logger.Debug($"Waiting for GSX Controller active ...");
                while (!GsxController.IsActive && !Token.IsCancellationRequested)
                    await Task.Delay(Config.CheckInterval, Token);
                Logger.Debug($"GSX Controller active");
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                Logger.Debug($"Start AircraftController");
                await AircraftController.Start();
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                Logger.Debug($"Start Notification Manager");
                await NotificationManager.Start();

                if (SettingProfile.RunAudioService)
                {
                    Logger.Debug($"Start AudioController");
                    await AudioController.Start();
                }

                IsSessionInitialized = true;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Debug("--- Session Init Done ---");
        }

        protected virtual async Task SessionCleanup()
        {
            try
            {
                if (!IsSessionInitialized)
                    return;

                Logger.Debug($"Cancel Request Token");
                RequestTokenSource.Cancel();

                Logger.Debug($"Stop AudioController");
                await AudioController.Stop();

                Logger.Debug($"Stop AircraftController");
                await AircraftController.Stop();

                Logger.Debug($"Stop Notification Manager");
                await NotificationManager.Stop();

                Logger.Debug($"Reset CommBus");
                await CommBus.Reset();

                Logger.Debug($"Stop GsxController");
                await GsxController.Stop();

                Logger.Debug($"Reset Notification Tracker");
                NotificationTracker.Reset();

                Logger.Debug($"Reset Flightplan");
                await Flightplan?.Reset();

                Config.SetDisplayUnit(Config.DisplayUnitDefault);

                LastGsxRestart = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            IsSessionInitialized = false;
            Logger.Debug("--- Session Cleanup Done ---"); ;
        }

        protected override async Task StopServiceControllers()
        {
            await SessionCleanup();
            await base.StopServiceControllers();
        }

        public virtual void SetGsxStartTime()
        {
            LastGsxRestart = DateTime.Now;
        }

        public virtual async Task RestartGsx()
        {
            try
            {
                NotificationTracker.Track(AppNotification.GsxRestart);
                Sys.KillProcess(App.Config.BinaryGsx2020);
                Sys.KillProcess(App.Config.BinaryGsx2024);

                Logger.Debug($"Wait for Binary Exit ({Config.DelayGsxBinaryStart}ms) ...");
                await Task.Delay(Config.DelayGsxBinaryStart, Token);

                if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2020 && !Sys.GetProcessRunning(App.Config.BinaryGsx2020))
                {
                    Logger.Debug($"Starting Process {App.Config.BinaryGsx2020}");
                    string dir = Path.Join(GsxController.PathInstallation, "couatl64");
                    Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2020}.exe"), dir);
                }

                if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2024 && !Sys.GetProcessRunning(App.Config.BinaryGsx2024))
                {
                    Logger.Debug($"Starting Process {App.Config.BinaryGsx2024}");
                    string dir = Path.Join(GsxController.PathInstallation, "couatl64");
                    Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2024}.exe"), dir);
                }

                Logger.Debug($"Wait for Binary Start ({Config.DelayGsxBinaryStart}ms) ...");
                await Task.Delay(Config.DelayGsxBinaryStart, Token);

                Logger.Debug($"GSX Restart finished");
                SetGsxStartTime();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            NotificationTracker.Clear(AppNotification.GsxRestart);
        }

        protected override async Task MainLoop()
        {
            await Task.Delay(App.Config.TimerGsxCheck, Token);

            if (ResetRequested > AppResetRequest.None)
            {
                Logger.Debug($"Reset was requested: {ResetRequested}");
                Logger.Information($"Restarting App Services ...");
                await SessionCleanup();
                if (ResetRequested == AppResetRequest.App)
                    await Task.Delay(2500, Token);
                else
                {
                    Logger.Information($"Restarting GSX ...");
                    await RestartGsx();
                }
                await SessionInitialize();
                ResetRequested = AppResetRequest.None;
            }
            else if (DateTime.Now >= NextGarbageCollection)
                DoGarbageCollect();
        }

        protected virtual void DoGarbageCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            NextGarbageCollection = DateTime.Now + TimeSpan.FromSeconds(300);
            Logger.Verbose("Garbage collected");
        }

        protected override Task DoCleanup()
        {
            try
            {
                SimStore["ATC AIRLINE"]?.OnReceived -= OnAtcAirlineChanged;
                SimStore.Remove("ATC AIRLINE");
                if (IsMsfs2024)
                {
                    SimStore["LIVERY NAME"]?.OnReceived -= OnAtcAirlineChanged;
                    SimStore.Remove("LIVERY NAME");
                }
                else
                {
                    SimStore["TITLE"]?.OnReceived -= OnAtcAirlineChanged;
                    SimStore.Remove("TITLE");
                }
                SimStore["ATC ID"]?.OnReceived -= OnAtcIdChanged;
                SimStore.Remove("ATC ID");

                MessageService.Unsubscribe<MsgSessionReady>(SessionInitialize);
                MessageService.Unsubscribe<MsgSessionEnded>(SessionCleanup);

                return base.DoCleanup();
            }
            catch { }

            return Task.CompletedTask;
        }
    }
}
