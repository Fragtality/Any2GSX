using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Any2GSX.Plugins
{
    public class PluginRepo
    {
        public static Config Config => AppService.Instance.Config;
        public static Definition Definition => AppService.Instance.Definition;
        public static PluginController PluginController => AppService.Instance.PluginController;
        public virtual SortedDictionary<string, PluginManifest> Plugins { get; } = [];
        public virtual SortedDictionary<string, AircraftChannels> Channels { get; } = [];
        public virtual SortedDictionary<string, ProfileManifest> Profiles { get; } = [];
        protected virtual DateTime NextCheck { get; set; } = DateTime.MinValue;
        protected virtual string LatestCommit { get; set; } = "";
        public virtual bool LatestValid => !string.IsNullOrWhiteSpace(LatestCommit);
        protected virtual HttpClient HttpClient { get; }
        protected virtual HttpClientHandler HttpClientHandler { get; }

        public PluginRepo()
        {
            HttpClientHandler = new HttpClientHandler
            {
                UseProxy = false
            };
            HttpClient = new()
            {
                Timeout = TimeSpan.FromMilliseconds(Config.HttpRequestTimeoutMs)
            };
            HttpClient.DefaultRequestHeaders.Accept.Clear();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
            HttpClient.DefaultRequestHeaders.CacheControl = new()
            {
                NoStore = true,
                NoCache = true,
            };
        }

        public virtual async Task Refresh()
        {
            if (NextCheck < DateTime.Now)
            {
                await RefreshReleaseInfo();
                if (LatestValid)
                {
                    await RefreshPlugins();
                    await RefreshChannels();
                    await RefreshProfiles();
                }
                else
                    Logger.Error($"Could not refresh latest Commit from Plugin Repository");
                NextCheck = DateTime.Now + TimeSpan.FromMinutes(5);
            }
        }

        protected virtual async Task RefreshReleaseInfo()
        {
            LatestCommit = await ProductDefinitionBase.GetLatestCommit(HttpClient, Definition.ProductAuthor, Definition.RepoPlugins, Definition.RepoDistPathReleaseInfo);
            Logger.Debug($"Received latest Commit: {LatestCommit}");
        }

        public virtual bool HasUpdates()
        {
            return PluginController.Plugins?.Values?.Where(p => p.HasUpdateAvail)?.Any() == true || PluginController?.Channels?.Values?.Where(c => c.HasUpdateAvail)?.Any() == true;
        }

        public virtual async Task RefreshPlugins()
        {
            var result = await GetPlugins();
            Plugins.Clear();
            foreach (var plugin in result)
                Plugins.Add(plugin.Key, plugin.Value);
        }

        public virtual async Task RefreshChannels()
        {
            var result = await GetChannels();
            Channels.Clear();
            foreach (var channel in result)
                Channels.Add(channel.Key, channel.Value);
        }

        public virtual async Task RefreshProfiles()
        {
            var result = await GetProfiles();
            Profiles.Clear();
            foreach (var profile in result)
                Profiles.Add(profile.Key, profile.Value);
        }

        public virtual string GetUrlCommit(string path)
        {
            return ProductDefinitionBase.GetUrlCommit(path, Definition.ProductAuthor, Definition.RepoPlugins, LatestCommit);
        }

        public virtual async Task<JsonNode> GetJsonFromUrl(string url)
        {
            Logger.Debug($"Downloading '{url}' ...");
            string json = await HttpClient.GetStringAsync(url);
            Logger.Verbose($"json received: len {json?.Length}");
            return JsonSerializer.Deserialize<JsonNode>(json);
        }

        public virtual async Task<string> GetStringFromUrl(string url)
        {
            Logger.Debug($"Downloading '{url}' ...");
            string json = await HttpClient.GetStringAsync(url);
            Logger.Verbose($"json received: len {json?.Length}");
            return json;
        }

        public virtual async Task<Dictionary<string, PluginManifest>> GetPlugins()
        {
            Dictionary<string, PluginManifest> result = [];
            try
            {
                JsonNode node = await GetJsonFromUrl(GetUrlCommit(Definition.RepoDistPathPluginFile));

                foreach (var entry in node.AsObject())
                {
                    var manifest = entry.Value.Deserialize<PluginManifest>();
                    if (manifest.VersionApp <= Definition.ProductVersion)
                        result.Add(entry.Key, manifest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual Task<PluginManifest> InstallPluginFromRepo(string pluginFile)
        {
            if (!LatestValid || !Plugins.TryGetValue(pluginFile, out _))
                return Task.FromResult<PluginManifest>(null);

            var installer = new PluginInstaller(GetUrlCommit($"{Definition.RepoDistPathPlugins}/{pluginFile}"));
            return installer.Install();
        }

        public virtual Task<PluginManifest> InstallPluginFromFile(string filePath)
        {
            if (!File.Exists(filePath) || !Path.GetExtension(filePath).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                return Task.FromResult<PluginManifest>(null);

            var installer = new PluginInstaller(filePath);
            return installer.Install();
        }

        public virtual async Task<Dictionary<string, AircraftChannels>> GetChannels()
        {
            Dictionary<string, AircraftChannels> result = [];
            try
            {
                JsonNode node = await GetJsonFromUrl(GetUrlCommit(Definition.RepoDistPathChannelFile));

                foreach (var entry in node.AsObject())
                {
                    var manifest = entry.Value.Deserialize<AircraftChannels>();
                    if (manifest.VersionApp <= Definition.ProductVersion)
                        result.Add(entry.Key, manifest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual async Task<bool> InstallChannelFromRepo(string channelFile)
        {
            if (!Channels.ContainsKey(channelFile))
                return false;

            bool result = false;
            try
            {
                string json = await GetStringFromUrl(GetUrlCommit($"{Definition.RepoDistPathChannels}/{channelFile}"));
                var channelDefinition = AircraftChannels.Deserialize(json);
                if (channelDefinition != null && !string.IsNullOrWhiteSpace(channelDefinition.Id))
                {
                    json = JsonSerializer.Serialize(channelDefinition, JsonOptions.JsonWriteOptions);
                    string path = Path.Join(Definition.ChannelFolder, $"{channelDefinition.Id}.json");
                    if (File.Exists(path))
                        File.Delete(path);
                    File.WriteAllText(path, json);
                    result = File.Exists(path);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        public virtual bool InstallChannelFromFile(string filePath)
        {
            if (!File.Exists(filePath) || !Path.GetExtension(filePath).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                return false;

            bool result = false;
            try
            {
                var channelDefinition = AircraftChannels.DeserializeFile(filePath);
                if (channelDefinition != null && !string.IsNullOrWhiteSpace(channelDefinition.Id))
                {
                    string json = JsonSerializer.Serialize(channelDefinition, JsonOptions.JsonWriteOptions);
                    string path = Path.Join(Definition.ChannelFolder, $"{channelDefinition.Id}.json");
                    if (File.Exists(path))
                        File.Delete(path);
                    File.WriteAllText(path, json);
                    result = File.Exists(path);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        public virtual async Task<Dictionary<string, ProfileManifest>> GetProfiles()
        {
            Dictionary<string, ProfileManifest> result = [];
            try
            {
                JsonNode node = await GetJsonFromUrl(GetUrlCommit(Definition.RepoDistPathProfileFile));

                foreach (var entry in node.AsObject())
                {
                    var manifest = entry.Value.Deserialize<ProfileManifest>();
                    if (manifest.VersionApp <= Definition.ProductVersion)
                        result.Add(entry.Key, manifest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        public virtual async Task<bool> ImportProfileFromRepo(string profileFile, ProfileManifest manifest)
        {
            if (!Profiles.ContainsKey(profileFile))
                return false;

            bool result = false;
            try
            {
                string json = await GetStringFromUrl(GetUrlCommit($"{Definition.RepoDistPathProfiles}/{profileFile}"));
                SettingProfile profile = JsonSerializer.Deserialize<SettingProfile>(json);
                if (profile == null)
                {
                    Logger.Warning($"SettingProfile json Data could not be parsed");
                    return result;
                }
                else
                    profile.Name = manifest.Name;

                var query = Config.SettingProfiles.Where(p => p.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                {
                    Logger.Debug($"Profile already exists ({query.Count()}) - delete old");
                    Config.SettingProfiles.RemoveAll(p => p.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase));
                }

                result = Config.ImportProfile(json);

                if (result && !string.IsNullOrWhiteSpace(profile.ChannelFileId)
                    && profile.ChannelFileId != SettingProfile.GenericId && !PluginController.Channels.ContainsKey(profile.ChannelFileId)
                    && Channels.Where(c => c.Value.Id.Equals(profile.ChannelFileId, StringComparison.InvariantCultureIgnoreCase)).Any())
                {
                    Logger.Debug($"Installing missing Channel File '{profile.ChannelFileId}' from Repo");
                    var channelFile = Channels.Where(c => c.Value.Id.Equals(profile.ChannelFileId, StringComparison.InvariantCultureIgnoreCase)).First();
                    result = await InstallChannelFromRepo(channelFile.Key);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                result = false;
            }

            return result;
        }
    }
}
