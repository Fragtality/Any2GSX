using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;
using System.Collections.Generic;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            string word = "Install";
            if (Config.Mode == SetupMode.UPDATE)
                word = "Update";
            var options = new Dictionary<int, string>()
            {
                { (int)CommModuleOption.AllSim, $"{word} Module on all Simulators" },
            };
            if (Config.Mode == SetupMode.UPDATE)
                options.Add((int)CommModuleOption.UpdateExisting, "Update only existing Installations");
            if (Config.Mode == SetupMode.INSTALL)
            {
                if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2020, SimulatorStore.Steam))
                    options.Add((int)CommModuleOption.Only2020Steam, $"{word} Module only on MSFS 2020 Steam");
                if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2020, SimulatorStore.MsStore))
                    options.Add((int)CommModuleOption.Only2020MStore, $"{word} Module only on MSFS 2020 Microsoft Store");
                if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2024, SimulatorStore.Steam))
                    options.Add((int)CommModuleOption.Only2024Steam, $"{word} Module only on MSFS 2024 Steam");
                if (FuncMsfs.CheckInstalledMsfs(Simulator.MSFS2024, SimulatorStore.MsStore))
                    options.Add((int)CommModuleOption.Only2024MStore, $"{word} Module only on MSFS 2024 Microsoft Store");
            }
            Items.Add(new ConfigItemRadio($"{word} CommBus Module (required)", options, Config.OptionCommModuleInstallation, Config));

            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Force Module Update", "Overwrite existing CommBus Modules (regardless of installed Version)", Config.OptionForceCommModuleUpdate, Config));

            ConfigItemHelper.CreateRadioAutoStart(Config, Items);
            ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items);

            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Reset Configuration", "Reset App Configuration to Default (only for Troubleshooting)", Config.OptionResetConfiguration, Config));
        }
    }
}
