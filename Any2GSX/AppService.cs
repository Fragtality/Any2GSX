using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.Audio;
using Any2GSX.CommBus;
using Any2GSX.GSX;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.Plugins;
using Any2GSX.Tools;
using CFIT.AppFramework.Messages;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public class AppService(Config config) : AppService<Any2GSX, AppService, Config, Definition>(config), IAppResources
    {
        public virtual IConfig AppConfig => Config;
        public virtual IProductDefinition ProductDefinition => Definition;
        public virtual ModuleCommBus CommBus { get; protected set; }
        public virtual ICommBus ICommBus => CommBus;
        public virtual InputEventManager InputEventManager => SimConnect.InputManager;
        public virtual IWeightConverter WeightConverter { get; } = new WeightConverter();
        public virtual double FuelWeightKgPerGallon => AircraftController?.FuelWeightKgPerGallon ?? 3.03907;
        public virtual Flightplan Flightplan { get; set; }
        public virtual IFlightplan IFlightplan => Flightplan;
        public virtual NotificationManager NotificationManager { get; protected set; }
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
        public event Action<SettingProfile> ProfileChanged;
        public virtual ApiController ApiController { get; protected set; }

        public virtual AppResetRequest ResetRequested {  get; set; } = AppResetRequest.None;

        protected override void CreateServiceControllers()
        {
            CommBus = SimConnect.AddModule(typeof(ModuleCommBus), Config) as ModuleCommBus;
            Flightplan = new();
            NotificationManager = new();
            PluginController = new();
            GsxController = new GsxController(Config);
            AircraftController = new AircraftController(Config);
            AudioController = new AudioController(Config);
        }

        protected override Task InitReceivers()
        {
            base.InitReceivers();
            ReceiverStore.Add<MsgSessionReady>().OnMessage += OnSessionReady;
            ReceiverStore.Add<MsgSessionEnded>().OnMessage += OnSessionEnded;

            Logger.Information($"Starting API Controller Thread ...");
            ApiController = new();
            Task task = new(ApiController.Run, Token, TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning);
            task.Start();

            SimStore.AddVariable("ATC AIRLINE", SimUnitType.String);
            SimStore.AddVariable("TITLE", SimUnitType.String);
            if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
				SimStore.AddVariable("LIVERY NAME", SimUnitType.String);
			SimStore.AddVariable("ATC ID", SimUnitType.String);

            PluginController.Refresh();
            Logger.Debug($"Fetching Setting Profile");
            SetSettingProfile();
            return Task.CompletedTask;
        }

        public virtual string GetAirline()
        {
            return SimStore["ATC AIRLINE"]?.GetString() ?? "";
        }

        public virtual string GetTitle()
        {
            if (IsMsfs2024)
                return SimStore["LIVERY NAME"]?.GetString() ?? "";
            else
                return SimStore["TITLE"]?.GetString() ?? "";
        }

        public virtual string GetAircraftString()
        {
            return SimService?.Controller?.SimConnect?.AircraftString ?? "";
        }

        public virtual string GetAtcId()
        {
            return SimStore["ATC ID"]?.GetString() ?? "";
        }

        public virtual void SetSettingProfile(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                SettingProfile = Config.SettingProfiles.First(p => p.Name == name);
                SettingProfile.Load();
                Logger.Debug($"Using Profile {SettingProfile}");
                ProfileChanged?.Invoke(SettingProfile);
            }
        }

        public virtual void SetSettingProfile()
        {
            SetSettingProfile(SearchSettingProfile()?.Name);
        }

        public virtual SettingProfile SearchSettingProfile()
        {
            SettingProfile profile = null;

            profile ??= SearchSettingProfile(Config.SettingProfiles);
            profile ??= Config.SettingProfiles.Where(p => p.Name == SettingProfile.DefaultId).First() ?? new SettingProfile();

            Logger.Information($"Matched Setting Profile: {profile}");
            return profile;
        }

        protected virtual SettingProfile SearchSettingProfile(IEnumerable<SettingProfile> settingProfiles)
        {
            foreach (var profile in settingProfiles)
            {
                if (profile.MatchType != ProfileMatchType.AtcId)
                    continue;
                var strings = profile.MatchString.Split('|');
                foreach (var s in strings)
                {
                    if (GetAtcId().Equals(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on ATC ID - '{GetAtcId()}' equals '{s}')");
                        return profile;
                    }
                }
            }

            foreach (var profile in settingProfiles)
            {
                if (profile.MatchType != ProfileMatchType.Title)
                    continue;
                var strings = profile.MatchString.Split('|');
                foreach (var s in strings)
                {
                    if (GetTitle().Contains(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on Title/Livery - '{GetTitle()}' contains '{s}')");
                        return profile;
                    }
                }
            }

            foreach (var profile in settingProfiles)
            {
                if (profile.MatchType != ProfileMatchType.Airline)
                    continue;
                var strings = profile.MatchString.Split('|');
                foreach (var s in strings)
                {
                    if (GetAirline().StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on Airline - '{GetAirline()}' starts with '{s}')");
                        return profile;
                    }
                }
            }

            foreach (var profile in settingProfiles)
            {
                if (profile.MatchType != ProfileMatchType.AircraftString)
                    continue;
                var strings = profile.MatchString.Split('|');
                foreach (var s in strings)
                {
                    if (GetAircraftString().Contains(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Information($"Loading Profile '{profile.Name}' (matched on SimObject - '{GetAircraftString()}' contains '{s}')");
                        return profile;
                    }
                }
            }

            return null;
        }

        protected virtual void OnSessionReady(MsgSessionReady obj)
        {
            _ = SessionInitialize();
            if (string.IsNullOrWhiteSpace(Config?.SimbriefUser))
            {
                MessageBox.Show("SimBrief User is not set!\r\nConfigure your User Name or ID in the App Settings.", "SimBrief User", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected virtual async Task SessionInitialize()
        {
            try
            {
                Logger.Debug($"Reset CommBus");
                await CommBus.Reset();
                await Task.Delay(750, Token);

                Logger.Debug($"Waiting for CommBus Module ...");
                while (!CommBus.IsConnected && !Token.IsCancellationRequested)
                    await Task.Delay(Config.CheckInterval, Token);
                Logger.Debug($"CommBus Module connected");

                Logger.Debug($"Fetching Setting Profile");
                SetSettingProfile();
                var startMode = PluginController.GetPluginStartMode(SettingProfile.PluginId);

                if (startMode == PluginStartMode.PreWalkaround)
                    await StartAircraftController(startMode);

                Logger.Debug($"Start GSX Controller");
                GsxController.Start();
                Logger.Debug($"Waiting for GSX Controller active ...");
                while (!GsxController.IsActive && !Token.IsCancellationRequested)
                    await Task.Delay(Config.CheckInterval, Token);
                Logger.Debug($"GSX Controller active");

                if (startMode == PluginStartMode.WaitConnected)
                    await StartAircraftController(startMode);

                Logger.Debug($"Start Notification Manager");
                NotificationManager.Start();                

                if (SettingProfile.RunAudioService)
                {
                    Logger.Debug($"Start AudioController");
                    AudioController.Start();
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Debug($"Init done");
        }

        protected virtual async Task StartAircraftController(PluginStartMode mode)
        {
            Logger.Debug($"Start AircraftController ({mode})");
            AircraftController.Start();
            Logger.Debug($"Waiting for Aircraft Interface connected ...");
            while (!AircraftController.IsConnected && !Token.IsCancellationRequested)
                await Task.Delay(Config.CheckInterval, Token);
            Logger.Debug($"Aircraft Interface connected");
        }

        protected virtual void OnSessionEnded(MsgSessionEnded obj)
        {
            _ = SessionCleanup();
        }

        protected virtual async Task SessionCleanup()
        {
            try
            {
                Logger.Debug($"Stop AudioController");
                AudioController?.Stop();

                Logger.Debug($"Stop AircraftController");
                AircraftController?.Stop();

                Logger.Debug($"Stop Notification Manager");
                await NotificationManager.Stop();

                Logger.Debug($"Reset CommBus");
                CommBus?.Reset();

                Logger.Debug($"Stop GsxController");
                await GsxController.Stop();

                Logger.Debug($"Reset Flightplan");
                Flightplan?.Reset();

                Config.SetDisplayUnit(Config.DisplayUnitDefault);

                LastGsxRestart = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            Logger.Debug($"Cleanup done");
        }

        protected override async Task StopServiceControllers()
        {
            await SessionCleanup();
            await base.StopServiceControllers();
        }

        public virtual async Task RestartGsx()
        {
            LastGsxRestart = DateTime.Now;
            Sys.KillProcess(App.Config.BinaryGsx2020);
            Sys.KillProcess(App.Config.BinaryGsx2024);

            Logger.Debug($"Wait for Binary Start ({Config.DelayGsxBinaryStart}ms) ...");
            await Task.Delay(Config.DelayGsxBinaryStart, App.Token);

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

            await Task.Delay(Config.DelayGsxBinaryStart, App.Token);
        }

        bool flag = true;
        protected override async Task MainLoop()
        {
            await Task.Delay(App.Config.TimerGsxCheck, App.Token);

            if (ResetRequested > AppResetRequest.None)
            {
                Logger.Debug($"Reset was requested: {ResetRequested}");
                Logger.Information($"Restarting App Services ...");
                OnSessionEnded(null);
                if (ResetRequested == AppResetRequest.App)
                    await Task.Delay(1500, App.Token);
                else
                {
                    Logger.Information($"Restarting GSX ...");
                    await RestartGsx();
                }
                OnSessionReady(null);
                ResetRequested = AppResetRequest.None;
            }

            if (!flag && AircraftController?.Aircraft?.IsConnected == true)
            {
                await CommBus.RegisterCommBus("TabletToPlane", BroadcastFlag.WASM, (evt, data) => Logger.Debug("TabletToPlane -- " + data));
                await CommBus.RegisterCommBus("PlaneToTablet", BroadcastFlag.JS, (evt, data) => Logger.Debug("PlaneToTablet -- " + data));
                flag = true;
            }
        }

        protected override Task FreeResources()
        {
            base.FreeResources();
            SimStore.Remove("ATC AIRLINE");
            SimStore.Remove("TITLE");
            if (IsMsfs2024)
                SimStore.Remove("LIVERY NAME");
            SimStore.Remove("ATC ID");

            ReceiverStore.Remove<MsgSessionReady>().OnMessage -= OnSessionReady;
            ReceiverStore.Remove<MsgSessionEnded>().OnMessage -= OnSessionEnded;

            ApiController.IsExecutionAllowed = false;
            NotificationManager.Dispose();
            AircraftController.Dispose();
            GsxController.Dispose();

            return Task.CompletedTask;
        }
    }
}
