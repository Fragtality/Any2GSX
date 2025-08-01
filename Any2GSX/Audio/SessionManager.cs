﻿using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Any2GSX.Audio
{
    public class SessionManager(AudioController controller)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual DeviceManager DeviceManager => Controller.DeviceManager;
        protected virtual Config Config => Controller.Config;
        protected virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        protected virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        protected virtual ConcurrentDictionary<string, List<AudioSession>> MappedAudioSessions { get; } = [];
        public virtual bool HasEmptySearches => MappedAudioSessions.Any(c => c.Value.Any(s => s.SearchCounter > Config.AudioProcessMaxSearchCount));
        public virtual bool HasInactiveSessions => CheckInactiveSessions();
        public virtual List<Process> ProcessList { get; } = [];

        public virtual void RegisterMappings()
        {
            foreach (var mapping in Profile.AudioMappings)
                RegisterMapping(mapping);

            Controller.ResetMappings = false;
        }

        protected virtual void RegisterMapping(AudioMapping mapping)
        {
            if (!MappedAudioSessions.ContainsKey(mapping.Channel))
                MappedAudioSessions.Add(mapping.Channel, []);

            var session = new AudioSession(Controller, mapping);
            MappedAudioSessions[mapping.Channel].Add(session);
            Logger.Debug($"Registered AudioSession {session}");
        }

        public virtual void UnregisterMappings()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value.ToList())
                    UnregisterMapping(session.Mapping);
        }

        protected virtual void UnregisterMapping(AudioMapping mapping)
        {
            if (!MappedAudioSessions.TryGetValue(mapping.Channel, out List<AudioSession>? sessionList))
                return;

            var list = sessionList.Where(s => s.Binary == mapping.Binary && s.Device == mapping.Device).ToList();
            foreach (var item in list)
            {
                try { item.RestoreVolumes(); } catch { }
                try { item.ClearSimSubscriptions(); } catch { }
                sessionList.Remove(item);
                Logger.Debug($"Removed AudioSession {item}");
            }
        }

        public virtual void Clear()
        {
            MappedAudioSessions.Clear();
        }

        protected virtual bool CheckInactiveSessions()
        {
            bool result;

            try
            {
                result = MappedAudioSessions.Any(c => c.Value.Any(s => s.Mapping.OnlyActive && s.SessionControls.Any(sc => sc.State != CoreAudio.AudioSessionState.AudioSessionStateActive)));
            }
            catch (Exception ex)
            {
                Logger.Warning($"'{ex.GetType().Name}' during Inactive Session Check");
                result = true;
            }

            return result;
        }

        public virtual bool CheckProcesses(bool force = false)
        {
            bool result = false;

            ProcessList.Clear();
            ProcessList.AddRange(Process.GetProcesses());

            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    if (session.CheckProcess(force) != 0)
                        result = true;

            return result;
        }

        public virtual void RestoreVolumes()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    session.RestoreVolumes();
        }

        public virtual void CheckSessions(bool force = false)
        {
            foreach (var channel in MappedAudioSessions)
            {
                foreach (var session in channel.Value)
                {
                    if (session.IsActive && (session.SessionControls.Count == 0 || force))
                    {
                        if (force)
                            Logger.Debug($"Query SessionControls for AudioSession {session}");
                        else
                            Logger.Verbose($"Query SessionControls for AudioSession {session}");
                        var sessions = DeviceManager.GetAudioSessions(session);
                        if (force || sessions.Count != session.SessionControls.Count)
                            session.SessionControls.Clear();

                        if (session.SessionControls.Count == 0 && sessions.Count != 0)
                        {
                            session.SetSessionList(sessions);
                            session.SynchControls();
                            Logger.Debug($"Added {sessions.Count} SessionControls to AudioSession {session}");
                        }
                        else if (session.SessionControls.Count == 0)
                            session.SearchCounter++;
                    }
                }
            }
        }

        public virtual void SynchControls()
        {
            foreach (var channel in MappedAudioSessions)
                foreach (var session in channel.Value)
                    session.SynchControls();
        }
    }
}