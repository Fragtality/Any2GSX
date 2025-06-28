using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Any2GSX.Plugins
{
    public class PluginController
    {
        public virtual Config Config => AppService.Instance.Config;
        public virtual Definition Definition => AppService.Instance.Definition;
        public virtual SortedDictionary<string, PluginManifest> Plugins { get; } = [];
        public virtual SortedDictionary<string, AircraftChannels> Channels { get; } = [];
        protected virtual ConcurrentDictionary<string, Assembly> LoadedAssemblies { get; } = [];
        public virtual List<string> LoadedPluginsBinary => [.. LoadedAssemblies.Keys];

        public virtual void Refresh()
        {
            RefreshPlugins();
            RefreshChannels();
        }

        public virtual void RefreshVersions(PluginRepo repo)
        {
            var repoPlugins = repo.Plugins;
            foreach (var plugin in Plugins.Values)
            {
                var query = repoPlugins.Where((kv) => kv.Value.Id == plugin.Id);
                if (query.Any())
                {
                    var repoPlugin = query.First().Value;
                    if (plugin.VersionPlugin < repoPlugin.VersionPlugin && Definition.ProductVersion >= repoPlugin.VersionApp)
                        plugin.HasUpdateAvail = true;
                    else
                        plugin.HasUpdateAvail = false;
                }
                else
                    plugin.HasUpdateAvail = false;
            }

            var repoChannels = repo.Channels;
            foreach (var channel in Channels.Values)
            {
                var query = repoChannels.Where((kv) => kv.Value.Id == channel.Id);
                if (query.Any())
                {
                    var repoChannel = query.First().Value;
                    if (channel.VersionChannel < repoChannel.VersionChannel && Definition.ProductVersion >= repoChannel.VersionApp)
                        channel.HasUpdateAvail = true;
                    else
                        channel.HasUpdateAvail = false;
                }
                else
                    channel.HasUpdateAvail = false;
            }
        }

        public virtual void RefreshPlugins()
        {
            Logger.Debug($"Refreshing Plugins ...");
            Plugins.Clear();
            try
            {
                var pluginDirectories = Directory.GetDirectories(Definition.PluginFolder);
                foreach (var dir in pluginDirectories)
                {
                    if (File.Exists(Path.Join(dir, Definition.PluginManifest)))
                    {
                        try
                        {
                            var manifest = PluginManifest.ReadManifest(Path.Join(dir, Definition.PluginManifest));
                            if (!File.Exists(Path.Join(dir, manifest.Filename)))
                            {
                                Logger.Warning($"Plugin File '{manifest.Filename}' not found for Plugin '{manifest}'");
                                continue;
                            }

                            if (manifest.VersionApp > Version.Parse(Definition.ProductVersion.ToString(3)))
                            {
                                Logger.Warning($"Plugin '{manifest}' requires Any2GSX Version '{manifest.VersionApp}'");
                                continue;
                            }

                            if (!Plugins.ContainsKey(manifest.Id))
                            {
                                Plugins.Add(manifest.Id, manifest);
                                Logger.Debug($"Plugin '{manifest}' v{manifest.VersionPlugin} added (Type: {manifest.Type} | File: {manifest.Filename})");
                            }
                            else
                                Logger.Warning($"Plugin Manifest for Id '{manifest}' already added");
                        }
                        catch
                        {
                            Logger.Warning($"Error while loading Plugin Manifest in Folder {dir}");
                        }
                    }
                    else
                        Logger.Debug($"Plugin Folder {dir} does not contain a manifest File");
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool DeleteInstalledPlugin(PluginManifest manifest)
        {
            try
            {
                if (!Plugins.ContainsKey(manifest.Id))
                    return false;

                if (LoadedAssemblies.ContainsKey(manifest.Id))
                {
                    MessageBox.Show($"The Plugin {manifest.Id} was already loaded by Any2GSX!\r\nRestart the Application (preferably without the Sim running) and try again!", "Plugin already loaded", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return false;
                }

                string pluginPath = Path.Join(Config.Definition.PluginFolder, manifest.Id);
                if (!Directory.Exists(pluginPath))
                    return true;

                try
                {
                    Directory.Delete(pluginPath, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }


                if (!Directory.Exists(pluginPath))
                {
                    Plugins.Remove(manifest.Id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        public virtual void RefreshChannels()
        {
            Logger.Debug($"Refreshing Channels ...");
            Channels.Clear();
            try
            {
                var channelFiles = Directory.GetFiles(Definition.ChannelFolder, "*.json");
                foreach (var file in channelFiles)
                {
                    var channel = AircraftChannels.DeserializeFile(file);
                    if (channel != null)
                    {
                        if (channel.VersionApp > Version.Parse(Definition.ProductVersion.ToString(3)))
                        {
                            Logger.Warning($"Channel Definiton '{channel.Id}' requires Any2GSX Version '{channel.VersionApp}'");
                            continue;
                        }

                        if (!Channels.ContainsKey(channel.Id))
                        {
                            Channels.Add(channel.Id, channel);
                            Logger.Debug($"Channel Definiton '{channel.Id}' v{channel.VersionChannel} added (Channels: {channel.ChannelDefinitions.Count})");
                        }
                        else
                            Logger.Warning($"Plugin Manifest for Id '{channel.Id}' already added");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public virtual bool DeleteInstalledChannel(AircraftChannels channel)
        {
            try
            {
                if (!Channels.ContainsKey(channel.Id))
                    return false;

                string channelFile = Path.Join(Config.Definition.ChannelFolder, $"{channel.Id}.json");
                if (!File.Exists(channelFile))
                    return true;

                try
                {
                    File.Delete(channelFile);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }

                if (!File.Exists(channelFile))
                {
                    Channels.Remove(channel.Id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        public virtual PluginStartMode GetPluginStartMode(string pluginId)
        {
            if (!Plugins.TryGetValue(pluginId, out var manifest))
                return PluginStartMode.WaitConnected;
            else
                return manifest.StartMode;
        }

        public virtual AircraftBase GetAircraftInterface(SettingProfile profile, string aircraftString, out string pluginId)
        {
            AircraftBase result = new GenericAircraft(AppService.Instance);
            pluginId = "Generic";
            if (profile.PluginId == SettingProfile.GenericId)
            {
                Logger.Debug($"Using Generic Aircraft Interface");
                return result;
            }

            try
            {
                var query = Plugins.Where(p => p.Value.Id == profile.PluginId);
                if (!query.Any())
                {
                    Logger.Debug($"Configured Plugin ID did not match to any registered Plugins - using Generic");
                    return result;
                }

                var manifest = query.First().Value;
                Logger.Debug($"Found configured Plugin ID in registered Plugins. Loading {manifest} ...");

                string path = Path.Join(Definition.PluginFolder, manifest.Directory, manifest.Filename);
                if (!File.Exists(path))
                    Logger.Warning($"Plugin Type did not match - using Generic");
                else if (manifest.Type == PluginType.BinaryV1)
                    result = GetDllInterface(aircraftString, manifest, path);
                else if (manifest.Type == PluginType.LuaV1)
                    result = GetLuaInterface(manifest, path);
                else
                    Logger.Warning($"Plugin Type did not match - using Generic");

                if (result == null)
                {
                    Logger.Warning($"Aircraft Plugin is NULL - using Generic");
                    result = new GenericAircraft(AppService.Instance);
                }
                else
                {
                    pluginId = profile.PluginId;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Logger.Warning($"Error while loading Aircraft Interface - using Generic");
            }
            return result;
        }

        protected virtual AircraftLua GetLuaInterface(PluginManifest manifest, string path)
        {
            var luaInterface = new AircraftLua(AppService.Instance, manifest.Directory, manifest.Filename);
            AppPlugin.Instance = new LuaPlugin(manifest.Id, PluginType.LuaV1, AppService.Instance);
            return luaInterface;
        }

        protected virtual AircraftBase GetDllInterface(string aircraftString, PluginManifest manifest, string path)
        {
            AircraftBase dllInterface = null;
            var assembly = GetAssembly(manifest.Id, path);
            var query = assembly.DefinedTypes.Where(t => t.BaseType == typeof(AppPlugin));
            if (query.Any())
            {
                var pluginType = query.First();
                AppPlugin appPlugin = Activator.CreateInstance(pluginType, AppService.Instance) as AppPlugin;
                
                var moduleType = appPlugin.SimConnectModuleType;
                if (appPlugin.SimConnectModuleType != null)
                    appPlugin.SimConnectModule = AppService.Instance.SimConnect.AddModule(appPlugin.SimConnectModuleType, AppService.Instance.Config);
                
                var aircraftType = appPlugin.GetAircraftInterface(aircraftString);
                dllInterface = Activator.CreateInstance(aircraftType, AppService.Instance) as AircraftBase;
                AppPlugin.Instance = appPlugin;
            }

            return dllInterface;
        }
        
        protected virtual Assembly GetAssembly(string id, string path)
        {
            if (!LoadedAssemblies.TryGetValue(id, out Assembly assembly))
            {
                assembly = Assembly.LoadFile(path);
                LoadedAssemblies.Add(id, assembly);
            }

            return assembly;
        }

        public virtual void UnloadPlugin()
        {
            if (AppPlugin.Instance == null)
                return;

            if (AppPlugin.Instance.SimConnectModuleType != null)
            {
                AppService.Instance.SimConnect.RemoveModule(AppPlugin.Instance.SimConnectModuleType);
                AppPlugin.Instance.SimConnectModule = null;
            }

            AppPlugin.Instance = null;
        }
    }
}
