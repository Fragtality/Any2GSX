using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.Audio
{
    public class AudioController : ServiceController<Any2GSX, AppService, Config, Definition>
    {
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual SimConnectManager SimConnect => Any2GSX.Instance.AppService.SimConnect;
        public virtual bool IsActive { get; protected set; } = false;
        protected virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        protected virtual SettingProfile SettingProfile => AppService.Instance?.SettingProfile;
        public virtual bool IsPlanePowered => AircraftController?.Aircraft?.IsAvionicPowered == true;
        public virtual bool HasInitialized { get; protected set; } = false;
        public virtual DeviceManager DeviceManager { get; }
        public virtual SessionManager SessionManager { get; }
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool ResetVolumes { get; set; } = false;
        public virtual bool ResetMappings { get; set; } = false;
        public virtual bool ResetChannels { get; set; } = false;
        public virtual AircraftChannels AircraftChannels { get; protected set; } = null;
        public virtual ConcurrentDictionary<string, ChannelDefinition> ChannelDefinitions { get; } = [];
        public virtual ConcurrentDictionary<string, ISimResourceSubscription> VolumeVariables { get; } = [];
        public virtual ConcurrentDictionary<string, ISimResourceSubscription> MuteVariables { get; } = [];

        public event Action OnChannelsChanged;

        public AudioController(Config config) : base(config)
        {
            DeviceManager = new(this);
            SessionManager = new(this);
        }

        protected override Task FreeResources()
        {
            UnregisterChannels();
            DeviceManager.Clear();
            return Task.CompletedTask;
        }

        protected virtual void RegisterChannels()
        {
            AircraftChannels = SettingProfile.GetAircraftChannels();
            foreach (var channel in AircraftChannels.ChannelDefinitions)
            {
                if (ChannelDefinitions.ContainsKey(channel.Name))
                {
                    Logger.Warning($"Channel Name '{channel.Name}' already added");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(channel.VolumeVariable))
                {
                    var sub = SimStore.AddVariable(channel.VolumeVariable, channel.VolumeUnit);
                    VolumeVariables.Add(channel.Name, sub);
                    Logger.Debug($"Added Volume Variable '{channel.VolumeVariable}' for Channel '{channel.Name}'");
                }

                if (!string.IsNullOrWhiteSpace(channel.MuteVariable))
                {
                    var sub = SimStore.AddVariable(channel.MuteVariable, channel.MuteUnit);
                    MuteVariables.Add(channel.Name, sub);
                    Logger.Debug($"Added Mute Variable '{channel.MuteVariable}' for Channel '{channel.Name}'");
                }

                ChannelDefinitions.Add(channel.Name, channel);
            }
            ResetChannels = false;
            TaskTools.RunLogged(() => OnChannelsChanged?.Invoke());
        }

        protected virtual void UnregisterChannels()
        {
            try
            {
                foreach (var channel in ChannelDefinitions.Values)
                {
                    SimStore.Remove(channel.VolumeVariable);
                    if (!string.IsNullOrWhiteSpace(channel.MuteVariable))
                        SimStore.Remove(channel.MuteVariable);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            ChannelDefinitions.Clear();
            VolumeVariables.Clear();
            MuteVariables.Clear();
            AircraftChannels = null;
        }

        public virtual ISimResourceSubscription GetVolumeSub(string channel)
        {
            if (VolumeVariables.TryGetValue(channel, out var sub))
                return sub;
            else
                return null;
        }

        public virtual ISimResourceSubscription GetMuteSub(string channel)
        {
            if (MuteVariables.TryGetValue(channel, out var sub))
                return sub;
            else
                return null;
        }

        protected virtual async Task SetStartupVolumes()
        {
            try
            {
                foreach (var channel in AircraftChannels.ChannelDefinitions)
                {

                    if (SettingProfile.AudioStartupVolumes.TryGetValue(channel.Name, out double pos) && pos >= 0.0)
                    {
                        if (channel.VolumeStartupCode != null)
                        {
                            Logger.Debug($"Sending Startup Volume Code of '{channel.Name}' for Pos {pos}");
                            await AppService.Instance.CommBus.ExecuteCalculatorCode(channel.VolumeStartupCode.Replace("{0}", Conversion.ToString(pos)));
                        }
                        else
                        {
                            Logger.Debug($"Setting Startup Volume Variable for '{channel.Name}' to {pos}");
                            await SimStore[channel.VolumeVariable].WriteValue(pos);
                        }                        
                    }

                    if (SettingProfile.AudioStartupUnmute.TryGetValue(channel.Name, out bool target) && target)
                    {
                        
                        if (channel.MuteStartupCode != null)
                        {
                            Logger.Debug($"Sending Startup Mute Code of '{channel.Name}' ({channel.UnmutedValue})");
                            await AppService.Instance.CommBus.ExecuteCalculatorCode(channel.MuteStartupCode.Replace("{0}", Conversion.ToString(channel.UnmutedValue)));
                        }
                        else
                        {
                            Logger.Debug($"Setting Startup Unmute Variable for '{channel.Name}' ({channel.UnmutedValue})");
                            await SimStore[channel.MuteVariable].WriteValue(channel.UnmutedValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected override async Task DoRun()
        {
            try
            {
                while ((!IsPlanePowered || !AircraftController.IsConnected) && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                    await Task.Delay(Config.AudioServiceRunInterval, Token);

                RegisterChannels();
                await Task.Delay(1000, Token);
                SessionManager.RegisterMappings();
                bool rescanNeeded = false;
                IsActive = true;
                Logger.Debug($"Aircraft powered. AudioService active");
                await SetStartupVolumes();
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    rescanNeeded = SessionManager.HasInactiveSessions || SessionManager.HasEmptySearches || ResetMappings || ResetChannels;
                    if (rescanNeeded)
                        Logger.Debug($"Rescan Needed - InactiveSessions {SessionManager.HasInactiveSessions} | EmptySearches {SessionManager.HasEmptySearches} | ResetMappings {ResetMappings}");

                    if (ResetChannels)
                    {
                        Logger.Debug($"Resetting Channels");
                        SessionManager.UnregisterMappings();
                        UnregisterChannels();
                        await Task.Delay(250);
                        RegisterChannels();
                        SessionManager.RegisterMappings();
                    }
                    else if (ResetMappings)
                    {
                        Logger.Debug($"Resetting Mappings");
                        SessionManager.UnregisterMappings();
                        SessionManager.RegisterMappings();
                    }

                    if (rescanNeeded || NextProcessCheck <= DateTime.Now)
                    {
                        if (SessionManager.CheckProcesses(rescanNeeded))
                        {
                            if (!rescanNeeded)
                                Logger.Debug($"Rescan Needed - CheckProcess had Changes");
                            rescanNeeded = true;
                            await Task.Delay(Config.AudioProcessStartupDelay, Token);
                        }
                        NextProcessCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.AudioProcessCheckInterval);
                    }

                    if (DeviceManager.Scan(SessionManager.HasEmptySearches))
                        rescanNeeded = true;
                    if (rescanNeeded)
                        Logger.Debug($"Rescan Needed - DeviceEnum");

                    HasInitialized = true;

                    SessionManager.CheckSessions(rescanNeeded);
                    if (ResetVolumes)
                        SessionManager.SynchControls();

                    ResetVolumes = false;
                    rescanNeeded = false;
                    await Task.Delay(Config.AudioServiceRunInterval, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            IsActive = false;

            try
            {
                SessionManager.UnregisterMappings();
                UnregisterChannels();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Debug($"AudioService ended");
        }

        public override Task Stop()
        {
            base.Stop();

            try { SessionManager.RestoreVolumes(); } catch { }
            UnregisterChannels();
            DeviceManager.Clear();
            SessionManager.Clear();
            IsActive = false;
            HasInitialized = false;

            return Task.CompletedTask;
        }
    }
}
