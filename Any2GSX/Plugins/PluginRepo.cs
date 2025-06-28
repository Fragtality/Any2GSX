using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
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

        public virtual async Task Refresh()
        {
            await RefreshPlugins();
            await RefreshChannels();
            await RefreshProfiles();
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

        public static async Task<JsonNode> GetJsonFromUrl(string url)
        {
            HttpClient client = new()
            {
                Timeout = TimeSpan.FromMilliseconds(1500)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            Logger.Debug($"Downloading '{url}' ...");
            string json = await client.GetStringAsync(url);
            Logger.Debug($"json received: len {json?.Length}");
            return JsonSerializer.Deserialize<JsonNode>(json);
        }

        public static async Task<string> GetStringFromUrl(string url)
        {
            HttpClient client = new()
            {
                Timeout = TimeSpan.FromMilliseconds(1500)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            Logger.Debug($"Downloading '{url}' ...");
            string json = await client.GetStringAsync(url);
            Logger.Debug($"json received: len {json?.Length}");
            return json;
        }

        public static async Task<Dictionary<string, PluginManifest>> GetPlugins()
        {
            Dictionary<string, PluginManifest> result = [];
            try
            {
                //JsonNode node = await GetJsonFromUrl(Definition.RepoDistUrlPluginFile);
                JsonNode node = JsonSerializer.Deserialize<JsonNode>(await File.ReadAllTextAsync(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\plugins\plugin-repo.json"));

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

        public virtual async Task<bool> InstallPluginFromRepo(string pluginFile)
        {
            if (!Plugins.TryGetValue(pluginFile, out _))
                return false;
            //var installer = new PluginInstaller($"{Definition.RepoDistUrlPlugins}/{pluginFile}");
            var installer = new PluginInstaller(Path.Join(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\plugins", pluginFile));
            return await installer.Install();
        }

        public virtual async Task<bool> InstallPluginFromFile(string filePath)
        {
            if (!File.Exists(filePath) || !Path.GetExtension(filePath).Equals(".zip", StringComparison.InvariantCultureIgnoreCase))
                return false;
            var installer = new PluginInstaller(filePath);
            return await installer.Install();
        }

        public static async Task<Dictionary<string, AircraftChannels>> GetChannels()
        {
            Dictionary<string, AircraftChannels> result = [];
            try
            {
                //JsonNode node = await GetJsonFromUrl(Definition.RepoDistUrlChannelFile);
                JsonNode node = JsonSerializer.Deserialize<JsonNode>(await File.ReadAllTextAsync(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\channel\channel-repo.json"));

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
                //string json = await GetStringFromUrl($"{Definition.RepoDistUrlChannels}/{channelFile}");
                string json = await File.ReadAllTextAsync(Path.Join(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\channel", channelFile));
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

        public static async Task<Dictionary<string, ProfileManifest>> GetProfiles()
        {
            Dictionary<string, ProfileManifest> result = [];
            try
            {
                //JsonNode node = await GetJsonFromUrl(Definition.RepoDistUrlProfileFile);
                JsonNode node = JsonSerializer.Deserialize<JsonNode>(File.ReadAllText(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\profiles\profile-repo.json"));
                await Task.Delay(25);

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
                //string json = await GetStringFromUrl($"{Definition.RepoDistUrlProfiles}/{profileFile}");
                string json = File.ReadAllText(Path.Join(@"C:\Users\Fragtality\source\repos\Any2GSX-Plugins\dist\profiles", profileFile));
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

                if (Config.ImportProfile(json, out bool wasDefault))
                {
                    Config.SaveConfiguration();
                    result = true;
                }

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
