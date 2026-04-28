using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
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

        public static readonly string OptionOpenApp = "OpenApp";
        public static readonly string OptionResetConfiguration = "ResetConfiguration";

        //CommBus Module
        public static readonly string OptionCommModuleInstallation = "CommModuleInstallation";
        public static readonly string OptionForceCommModuleUpdate = "ForceCommModuleUpdate";
        public static readonly string OptionSkipModuleUpdate = "SkipModuleUpdate";
        public virtual string ModuleVersion { get; set; } = "0.4.0";

        //Pilotsdeck
        public static readonly string OptionInstallDeckProfile = "InstallDeckProfile";
        public static readonly string PilotsdeckPath = Path.Combine(Sys.FolderAppDataRoaming(), @"Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin");

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "10.0.7";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.7/windowsdesktop-runtime-10.0.7-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-10.0.7-win-x64.exe";

        public Config() : base()
        {
            SetOption<int>(OptionCommModuleInstallation, (int)CommModuleOption.AllSim);
            SetOption(OptionForceCommModuleUpdate, false);
            SetOption(OptionSkipModuleUpdate, false);
            SetOption(OptionInstallDeckProfile, false);
            SetOption(OptionOpenApp, false);
        }

        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //ResetConfig
            SetOption(OptionResetConfiguration, false);

            if (FuncMsfs.IsRunning())
                SetOption(OptionOpenApp, true);
        }

        public static bool PilotsdeckInstalled()
        {
            bool result;
            try
            {
                result = File.Exists(Path.Combine(PilotsdeckPath, "PilotsDeck.exe"));
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
