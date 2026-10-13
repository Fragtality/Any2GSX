using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Installer.Worker
{
    public class WorkerInstallUpdate : WorkerAppInstall<Config>
    {
        public bool ResetConfiguration { get; set; } = false;

        public WorkerInstallUpdate(Config config) : base(config)
        {
            SetPropertyFromOption<bool>(Config.OptionResetConfiguration);
        }

        protected override void CreateFileExclusions()
        {

        }

        protected override bool DeleteOldFiles()
        {
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "log"), true, true);
            FuncIO.DeleteDirectory(InstallerExtractDir, true, true);

            if (File.Exists(Config.ProductConfigPath) && ResetConfiguration)
            {
                Logger.Debug($"Deleting Config File '{Config.ProductConfigPath}'");
                FuncIO.DeleteFile(Config.ProductConfigPath);
            }

            return Directory.Exists(InstallerExtractDir);
        }

        protected override bool CreateDefaultConfig()
        {
            using (var stream = GetAppConfig())
            {
                var confStream = File.Create(Config.ProductConfigPath);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(confStream);
                confStream.Flush(true);
                confStream.Close();
            }
            Thread.Sleep(250);
            return Config.HasConfigFile;
        }

        protected override bool FinalizeSetup()
        {
            string logDir = Path.Combine(Config.ProductPath, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string channelDir = Path.Combine(Config.ProductPath, "audio-channel");
            if (!Directory.Exists(channelDir))
                Directory.CreateDirectory(channelDir);

            string pluginDir = Path.Combine(Config.ProductPath, "plugins");
            if (!Directory.Exists(pluginDir))
                Directory.CreateDirectory(pluginDir);

            CheckCouatlConfig();

            return Directory.Exists(logDir) && Directory.Exists(channelDir) && Directory.Exists(pluginDir);
        }

        protected virtual void CheckCouatlConfig()
        {
            string pathConfig = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Virtuali\CouatlAddons.ini");
            string pathBackup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Virtuali\CouatlAddons.ini.bak");

            if (!File.Exists(pathConfig))
                return;

            string content = File.ReadAllText(pathConfig);
            var sectionRegex = new Regex(@"(?ms)^[ \t]*\[gsx\][ \t]*\r?\n.*?(?=^[ \t]*\[.*?\][ \t]*(?:\r?$)|\z)", RegexOptions.IgnoreCase);

            if (sectionRegex.IsMatch(content))
            {
                content = sectionRegex.Replace(content, match =>
                {
                    string section = match.Value;

                    var settingRegex = new Regex(@"(?m)^[ \t]*remote_server_enabled[ \t]*=[ \t]*(.*?)[ \t]*$", RegexOptions.IgnoreCase);
                    var settingMatch = settingRegex.Match(section);

                    if (settingMatch.Success)
                    {
                        if (settingMatch.Groups[1].Value.Trim() != "1")
                        {
                            section = settingRegex.Replace(section, "remote_server_enabled = 1", 1);
                        }
                    }
                    else
                    {
                        section = section.TrimEnd('\r', '\n') + Environment.NewLine + "remote_server_enabled = 1" + Environment.NewLine + Environment.NewLine;
                    }

                    return section;
                }, 1);
            }

            string originalContent = File.ReadAllText(pathConfig);
            if (content != originalContent)
            {
                if (File.Exists(pathBackup))
                    File.Delete(pathBackup);
                File.Copy(pathConfig, pathBackup);

                File.WriteAllText(pathConfig, content);
            }
        }
    }
}
