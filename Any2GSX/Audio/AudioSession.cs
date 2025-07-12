using Any2GSX.Tools;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using CoreAudio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Any2GSX.Audio
{
    public class AudioSession(AudioController controller, AudioMapping mapping)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual SessionManager Manager => Controller.SessionManager;
        public virtual AudioMapping Mapping => new(Channel, Device, Binary, UseLatch, OnlyActive);
        public virtual string Channel { get; } = mapping.Channel;
        public virtual string Device { get; } = mapping.Device;
        public virtual string Binary { get; } = mapping.Binary;
        public virtual bool UseLatch { get; } = mapping.UseLatch;
        public virtual bool OnlyActive { get; } = mapping.OnlyActive;
        public virtual uint ProcessId { get; protected set; } = 0;
        public virtual int ProcessCount { get; protected set; } = 0;
        public virtual bool IsActive => ProcessId > 0 && Controller.HasInitialized && Controller.IsExecutionAllowed && ChannelDefinition != null;
        public virtual bool IsRunning => Manager?.ProcessList?.Any(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)) == true;
        public virtual int SearchCounter { get; set; } = 0;
        public virtual ConcurrentDictionary<string, float> SavedVolumes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SavedMutes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsVolume { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsMute { get; } = [];
        public virtual List<AudioSessionControl2> SessionControls { get; } = [];
        public virtual ISimResourceSubscription SubVolume { get; protected set; }
        public virtual ISimResourceSubscription SubMute { get; protected set; }
        public virtual ChannelDefinition ChannelDefinition { get; protected set; }

        public override string ToString()
        {
            return $"{Channel}: '{Binary}' @ '{(string.IsNullOrWhiteSpace(Device) ? "all" : Device)}'";
        }

        public virtual int CheckProcess(bool force = false)
        {
            try
            {
                bool running = IsRunning;
                int result = 0;

                if (!running && ProcessId != 0 || force)
                {
                    if (!force)
                        Logger.Debug($"Binary '{Binary}' stopped");
                    else
                        Logger.Verbose($"Binary '{Binary}' stopped");
                    ClearSimSubscriptions();
                    ProcessId = 0;
                    SessionControls.Clear();
                    result = -1;
                }

                if (running && ProcessId == 0)
                {
                    ProcessId = (uint)Manager.ProcessList.Where(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().Id;
                    if (ProcessId != 0)
                    {
                        Logger.Debug($"Binary '{Binary}' started (PID: {ProcessId})");
                        SetSimSubscriptions();
                        result = 1;
                    }
                }
                else if (running && ProcessId > 0)
                {
                    int count = Manager?.ProcessList?.Where(p => p.ProcessName.Equals(Binary, StringComparison.InvariantCultureIgnoreCase)).Count() ?? 0;
                    if (ProcessCount != count)
                    {
                        result = 1;
                        Logger.Debug($"Process Count changed");
                    }
                    ProcessCount = count;
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return 0;
        }

        public virtual void RestoreVolumes()
        {
            var savedVolumes = SavedVolumes.ToArray();
            foreach (var ident in savedVolumes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Volume for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.MasterVolume = ident.Value;
                        SavedVolumes.Remove(ident.Key);
                    }
                    catch { }
                }
            }
            SavedVolumes.Clear();

            var savedMutes = SavedMutes.ToArray();
            foreach (var ident in savedMutes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Mute for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.Mute = ident.Value;
                        SavedMutes.Remove(ident.Key);
                    }
                    catch { }
                }
            }
            SavedMutes.Clear();

            SynchedSessionsVolume.Clear();
            SynchedSessionsMute.Clear();
        }

        public virtual void SetSessionList(List<AudioSessionControl2> list)
        {
            SessionControls.Clear();
            SearchCounter = 0;

            foreach (var item in list)
            {
                SavedVolumes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.MasterVolume);
                SavedMutes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.Mute);
                SessionControls.Add(item);
            }
        }

        public virtual void SetSimSubscriptions()
        {
            if (Controller.ChannelDefinitions.TryGetValue(Channel, out var definition))
                ChannelDefinition = definition;
            else
            {
                ChannelDefinition = null;
                Logger.Warning($"Loaded Channel Definitions do not contain Channel '{Channel}'");
                return;
            }

            SubVolume = Controller.GetVolumeSub(Channel);
            if (SubVolume != null)
                SubVolume.OnReceived += OnVolumeChange;
            
            SubMute = Controller.GetMuteSub(Channel);
            if (SubMute != null)
                SubMute.OnReceived += OnMuteChange;
        }

        public virtual void ClearSimSubscriptions()
        {
            if (SubVolume != null)
            {
                try { SubVolume.OnReceived -= OnVolumeChange; } catch { }
                SubVolume = null;
            }

            if (SubMute != null)
            {
                try { SubMute.OnReceived -= OnMuteChange; } catch { }
                SubMute = null;
            }

            ChannelDefinition = null;
        }

        public virtual void SynchControls()
        {
            if (SubVolume != null)
                OnVolumeChange(SubVolume, null);
            if (SubMute != null)
                OnMuteChange(SubMute, null);
        }

        protected virtual void OnVolumeChange(ISimResourceSubscription sub, object data)
        {
            if (!IsActive || SessionControls.Count == 0 || SubVolume == null)
                return;
            if (sub.Name != SubVolume.Name)
                return;

            double value = sub.GetValue<double>();
            if (value < ChannelDefinition.MinValue || value > ChannelDefinition.MaxValue)
            {
                Logger.Debug($"Invalid Value Range for '{sub.Name}': {value}");
                return;
            }
            float fValue = (float)AudioTools.NormalizedRatio(value, ChannelDefinition.MinValue, ChannelDefinition.MaxValue);

            try
            {
                if (data != null || Controller.Config.AudioSynchSessionOnCountChange)
                    SessionControls.ForEach(ctrl => ctrl.SimpleAudioVolume.MasterVolume = fValue);
                else
                {
                    foreach (var ctrl in SessionControls)
                    {
                        if (Controller.ResetVolumes || !SynchedSessionsVolume.ContainsKey(ctrl.SessionInstanceIdentifier))
                        {
                            ctrl.SimpleAudioVolume.MasterVolume = fValue;
                            SynchedSessionsVolume.TryAdd(ctrl.SessionInstanceIdentifier, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void OnMuteChange(ISimResourceSubscription sub, object data)
        {
            if (!IsActive || SessionControls.Count == 0 || !UseLatch || SubMute == null)
                return;
            if (sub.Name != SubMute.Name)
                return;

            double value = sub.GetValue<double>();
            bool? mute = null;
            if (value == ChannelDefinition.UnmutedValue)
                mute = false;
            else if (value == ChannelDefinition.MutedValue)
                mute = true;
            else
            {
                Logger.Debug($"Invalid Value for '{sub.Name}': {value}");
                return;
            }

            try
            {
                if (data != null || Controller.Config.AudioSynchSessionOnCountChange)
                    SessionControls.ForEach(ctrl => ctrl.SimpleAudioVolume.Mute = mute == true);
                else
                {
                    foreach (var ctrl in SessionControls)
                    {
                        if (Controller.ResetVolumes || !SynchedSessionsMute.ContainsKey(ctrl.SessionInstanceIdentifier))
                        {
                            ctrl.SimpleAudioVolume.Mute = mute == true;
                            SynchedSessionsMute.TryAdd(ctrl.SessionInstanceIdentifier, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
