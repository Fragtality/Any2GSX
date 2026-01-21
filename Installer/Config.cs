using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Product;
using System;
using System.IO;

namespace Installer
{
    public enum CommModuleOption
    {
        UpdateExisting = 0,
        AllSim = 1,
        Only2020Steam = 2,
        Only2020MStore = 3,
        Only2024Steam = 4,
        Only2024MStore = 5,
    }

    public class Config : ConfigBase
    {
        public override string ProductName { get { return "Any2GSX"; } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, "bin", ProductExe); } }
        public virtual string InstallerExtractDir { get { return Path.Combine(ProductPath, "bin"); } }
        public static string CommModuleName { get { return "fragtality-commbus-module"; } }

        public static readonly string OptionResetConfiguration = "ResetConfiguration";

        //CommBus Module
        public static readonly string OptionCommModuleInstallation = "CommModuleInstallation";
        public static readonly string OptionForceCommModuleUpdate = "ForceCommModuleUpdate";
        public virtual string ModuleVersion { get; set; } = "0.3.5";

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "10.0.2";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.2/windowsdesktop-runtime-10.0.2-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-10.0.2-win-x64.exe";

        public Config() : base()
        {
            SetOption<int>(OptionCommModuleInstallation, (int)CommModuleOption.AllSim);
            SetOption(OptionForceCommModuleUpdate, false);
        }

        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //ResetConfig
            SetOption(OptionResetConfiguration, false);
        }

        public static bool PilotsdeckInstalled()
        {
            bool result;
            try
            {
                result = File.Exists(Path.Combine(Sys.FolderAppDataRoaming(), @"Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin\PilotsDeck.exe"));
            }
            catch (Exception ex)
            {
                result = false;
                Logger.LogException(ex);
            }
            return result;
        }
    }
}
