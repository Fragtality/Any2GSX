using Any2GSX.AppConfig;
using Any2GSX.Audio;
using Any2GSX.GSX;
using Any2GSX.PluginInterface;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Any2GSX.Plugins
{
    public class PluginInstaller(string pluginUrl)
    {
        protected virtual Config Config => AppService.Instance.Config;
        protected virtual CancellationToken Token => AppService.Instance.Token;
        public virtual string PluginUrl { get; } = pluginUrl;
        protected virtual string FilePath { get; set; } 
        protected virtual PluginManifest Manifest {  get; set; } = null;
        protected virtual string FileName => Path.GetFileNameWithoutExtension(FilePath);
        protected virtual string PluginFolder => Config.Definition.PluginFolder;

        protected static void ShowDialog(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var scrollView = new ScrollViewer
            {
                Content = content,
                FontSize = 12.5,
                FontWeight = FontWeights.Normal,
                Padding = new Thickness(10),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            var panel = new StackPanel()
            {
                Orientation = Orientation.Vertical
            };
            var button = new Button()
            {
                Content = "Close",
                FontWeight = FontWeights.DemiBold,
                FontSize = 12,
                Margin = new Thickness(16, 0, 0, 12),
                Padding = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(scrollView);
            panel.Children.Add(button);
            var appWindow = AppService.Instance.App.AppWindow;
            var window = new Window
            {
                Title = title,
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                MaxHeight = SystemParameters.PrimaryScreenHeight - 256,
                MinWidth = 256,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(2)
            };
            button.Click += (_, _) => window.Close();
            window.Activated += (_, _) => {
                window.Top = appWindow.Top + (appWindow.ActualHeight / 2.0) - (window.ActualHeight / 2.0);
                window.Left = appWindow.Left + (appWindow.ActualWidth / 2.0) - (window.ActualWidth / 2.0);
            };

            window.ShowDialog();
        }

        public virtual async Task<bool> Install()
        {
            bool result = false;
            try
            {
                if (!await CheckUrl())
                    return result;
                result = ExtractPlugin();
                if (result && !string.IsNullOrWhiteSpace(Manifest?.Id))
                {
                    if (Manifest?.PluginDefaultProfile is JsonElement element)
                        SetPluginProfile(element);
                    if (!string.IsNullOrWhiteSpace(Manifest?.InstallUsageNotes))
                        ShowDialog($"Installation and Usage Notes", Manifest.InstallUsageNotes);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
                result = false;
            }

            return result;
        }

        protected virtual void SetPluginProfile(JsonElement element)
        {
            var profile = new SettingProfile();
            int count = 0;
            foreach (var property in element.EnumerateObject())
            {
                if (profile.HasProperty(property.Name))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        profile.SetPropertyValue<string>(property.Name, property.Value.GetString());
                    else if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out double dblValue) && profile.IsPropertyType<double>(property.Name))
                        profile.SetPropertyValue<double>(property.Name, dblValue);
                    else if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out int intValue))
                        profile.SetPropertyValue<int>(property.Name, intValue);
                    else if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                        profile.SetPropertyValue<bool>(property.Name, property.Value.GetBoolean());
                    else if (property.Name == "AudioMappings")
                        profile.SetPropertyValue<List<AudioMapping>>(property.Name, property.Value.Deserialize<List<AudioMapping>>());
                    else if (property.Name == "AudioStartupVolumes")
                        profile.SetPropertyValue<Dictionary<string, double>>(property.Name, property.Value.Deserialize<Dictionary<string, double>>());
                    else if (property.Name == "AudioStartupUnmute")
                        profile.SetPropertyValue<Dictionary<string, bool>>(property.Name, property.Value.Deserialize<Dictionary<string, bool>>());
                    else if (property.Name == "DepartureServices")
                        profile.SetPropertyValue<SortedDictionary<int, ServiceConfig>>(property.Name, property.Value.Deserialize<SortedDictionary<int, ServiceConfig>>());
                    else
                        profile.SetPropertyValue<object>(property.Name, property.Value);

                    count++;
                }
            }

            profile.Name = $"Plugin Default - {Manifest.Id}";
            if (count > 0 && !Config.SettingProfiles.Any(p => p.Name.Equals(profile.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                Logger.Debug($"Adding Default Plugin Profile");
                Config.SettingProfiles.Add(profile);
                Config.SaveConfiguration();
                Config.NotifyPropertyChanged(nameof(Config.SettingProfiles));
            }
        }

        protected virtual async Task<bool> CheckUrl()
        {
            if (PluginUrl.StartsWith("http"))
            {
                Logger.Debug($"Downloading Plugin '{PluginUrl}' ...");
                if (!await DownloadFile())
                    return false;
            }
            else if (File.Exists(PluginUrl))
            {
                Logger.Debug($"Using Plugin Archive '{PluginUrl}' ...");
                FilePath = PluginUrl;
            }
            else
            {
                Logger.Error("Invalid Plugin URL!");
                return false;
            }

            bool result = Path.GetExtension(FilePath)?.Equals(".zip", StringComparison.InvariantCultureIgnoreCase) == true;
            if (!result)
                Logger.Error("Invalid Plugin Extension!");
            return result;
        }

        protected virtual async Task<bool> DownloadFile()
        {
            string workdir = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

            Uri uri = new(PluginUrl);
            string file;
            if (Path.GetExtension(uri.LocalPath) == ".zip")
                file = Path.Join(workdir, Path.GetFileName(uri.LocalPath));
            else
            {
                Logger.Error($"Plugin URL does not point to a File!");
                return false;
            }

            if (File.Exists(file))
                File.Delete(file);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0");

            Logger.Debug($"Starting Download of {PluginUrl} to byte array ...");
            var task = httpClient.GetByteArrayAsync(PluginUrl);
            while (!task.IsCompleted && !task.IsFaulted && !Token.IsCancellationRequested)
                await Task.Delay(250, Token);

            int? length = task?.Result?.Length;
            if (task.IsCompleted && !task.IsFaulted && length > 1 && !Token.IsCancellationRequested)
            {
                Logger.Debug($"Download finished. Saving byte array to {file} ...");
                File.WriteAllBytes(file, task.Result);
                if (File.Exists(file) && (new FileInfo(file))?.Length > 1)
                {
                    FilePath = file;
                    return true;
                }
            }
            else
                Logger.Warning($"Download failed! (failed: {task.IsFaulted} | len: {length})");

            return false;
        }

        protected virtual bool ExtractPlugin()
        {
            using Stream stream = new FileStream(FilePath, FileMode.Open);
            if (stream == null)
            {
                Logger.Error("FileStream is NULL!");
                return false;
            }
            ZipArchive archive = new(stream);
            if (stream == null)
            {
                Logger.Error("ZipArchive is NULL!");
                return false;
            }
            Logger.Debug($"Zip File '{FileName}' opened ({archive.Entries.Count} Entries)");

            PluginManifest manifest = null;
            ZipArchiveEntry entryManifest = null;
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                if (file.Name.Equals(Config.Definition.PluginManifest, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Debug($"Found manifest");
                    manifest = JsonSerializer.Deserialize<PluginManifest>(file.Open());
                    Logger.Debug($"Plugin ID from manifest: {manifest.Id}");
                    Manifest = manifest;
                    entryManifest = file;
                    break;
                }
            }
            if (manifest == null || entryManifest == null)
            {
                Logger.Error("Manifest not found!");
                MessageBox.Show("Manifest not found!", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                Logger.Error("PluginID is empty!");
                MessageBox.Show("PluginID is empty!", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (manifest.VersionApp > Version.Parse(Config.Definition.ProductVersion.ToString(3)))
            {
                Logger.Error($"Plugin '{manifest}' requires Any2GSX Version '{manifest.VersionApp}'");
                MessageBox.Show($"Plugin '{manifest}' requires Any2GSX Version '{manifest.VersionApp}'", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (AppService.Instance.PluginController.LoadedPluginsBinary.Contains(manifest.Id))
            {
                MessageBox.Show($"The Plugin DLL is already loaded!\r\nPlease close Any2GSX and install/update the Plugin before it was loaded.", "Already loaded", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ZipArchiveEntry entryPluginFile = null;
            ZipArchiveEntry entryChannelFile = null;
            //ZipArchiveEntry entryAircraftProfile = null;
            bool hasAircraftProfiles = manifest?.AircraftProfileEntries?.Count > 0;
            int foundProfiles = 0;
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                if (file.Name.Equals(manifest.Filename, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Debug($"Found Plugin File '{manifest.Filename}'");
                    entryPluginFile = file;
                }

                if (!string.IsNullOrWhiteSpace(manifest.ChannelFile) && file.Name.Equals(manifest.ChannelFile, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.Debug($"Found Channel File '{manifest.ChannelFile}'");
                    entryChannelFile = file;
                }

                if (hasAircraftProfiles && manifest.AircraftProfileEntries?.Where(f => file.FullName.Equals($"{f}/", StringComparison.InvariantCultureIgnoreCase))?.Any() == true)
                {
                    Logger.Debug($"Found Aircraft Profile Entry '{file.FullName}'");
                    foundProfiles++;
                }
            }
            if (entryPluginFile == null)
            {
                Logger.Error("Plugin File not found!");
                MessageBox.Show("Plugin File not found!", "Installation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string pathPlugin = Path.Join(PluginFolder, manifest.Id);
            Logger.Debug($"Checking Dir '{pathPlugin}'");
            if (Directory.Exists(pathPlugin))
            {
                Logger.Debug($"Deleting Directory");
                Directory.Delete(pathPlugin, true);
            }
            Logger.Debug($"Creating Directory");
            Directory.CreateDirectory(pathPlugin);

            Logger.Debug($"Extracting Plugin Files ...");
            string pathManifest = Path.Join(pathPlugin, entryManifest.Name);
            entryManifest.ExtractToFile(pathManifest);
            entryPluginFile.ExtractToFile(Path.Join(pathPlugin, entryPluginFile.Name));

            if (entryChannelFile != null)
            {
                Logger.Debug($"Extracting Channel File ...");
                string pathChannel = Path.Join(Config.Definition.ChannelFolder, entryChannelFile.Name);
                entryChannelFile.ExtractToFile(pathChannel,true);
            }

            if (hasAircraftProfiles && foundProfiles == manifest.AircraftProfileEntries?.Count)
            {
                Logger.Debug($"Installing Aircraft Profiles ...");
                if (Config.AutoInstallGsxProfiles || MessageBox.Show($"The Plugin {manifest.Id} wants to install GSX Aircraft Profile(s) for:\r\n- {string.Join("\r\n- ", manifest.AircraftProfileEntries)}\r\nAny existing Profiles will be overwritten.\r\nDo you want to install these Profile(s)?", "Install GSX Aircraft Profile", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (var profile in manifest.AircraftProfileEntries)
                    {
                        Logger.Debug($"Install Profile '{profile}'");
                        InstallAircraftProfile(archive, profile);
                    }
                }
                else
                    Logger.Debug($"Installation denied.");
            }
            else if (hasAircraftProfiles)
                Logger.Warning($"Aircraft Profile Count in Manifest ({manifest.AircraftProfileEntries?.Count}) did not match found Count ({foundProfiles})");

            stream.Close();
            return File.Exists(pathManifest);
        }

        protected virtual void InstallAircraftProfile(ZipArchive archive, string profileEntryName)
        {
            string profilePath = Path.Join(GsxConstants.PathAircraftProfile, profileEntryName);
            if (Directory.Exists(profilePath))
                Logger.Debug($"The Aircraft Profile {profileEntryName} already exists and will be deleted");

            archive.ExtractArchiveDirectory(profileEntryName, GsxConstants.PathAircraftProfile);
        }
    }
}
