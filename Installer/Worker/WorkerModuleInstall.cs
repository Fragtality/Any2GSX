using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.LibFunc;
using CFIT.Installer.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Installer.Worker
{
    public class ModuleItem
    {
        public Simulator Simulator { get; set; }
        public SimulatorStore Store { get; set; }
        public string PackagePath { get; set; }
        public bool Exists { get; set; }
    }

    public class WorkerModuleInstall<C> : TaskWorker<Config>
    {
        public virtual CommModuleOption ModuleInstallOption { get; }
        public virtual string ModuleVersion => Config.ModuleVersion;
        public virtual bool ForceCommModuleUpdate { get; set; } = false;
        public virtual string CommModuleName => Config.CommModuleName;
        public virtual bool IsOptionAll => ModuleInstallOption == CommModuleOption.AllSim;
        public virtual bool IsOptionSteam => ModuleInstallOption == CommModuleOption.Only2020Steam || ModuleInstallOption == CommModuleOption.Only2024Steam;
        public virtual bool IsOptionMStore => ModuleInstallOption == CommModuleOption.Only2020MStore || ModuleInstallOption == CommModuleOption.Only2024MStore;

        public virtual List<ModuleItem> ModuleItems { get; } = new List<ModuleItem>();
        
        public WorkerModuleInstall(Config config) : base(config, $"CommBus Sim Module", "Check State of Module ...")
        {
            if (Config.HasOption<int>(Config.OptionCommModuleInstallation, out int value))
                ModuleInstallOption = (CommModuleOption)value;
            else
            {
                Logger.Warning($"No Option/Value set for {Config.OptionCommModuleInstallation}");
                ModuleInstallOption = CommModuleOption.AllSim;
            }
            SetPropertyFromOption<bool>(Config.OptionForceCommModuleUpdate);
            Model.DisplayCompleted = true;
        }

        public static string GetModulePath(string packagePath)
        {
            return Path.Combine(packagePath, Config.CommModuleName);
        }

        public static bool HasModulePath(string packagePath)
        {
            return Directory.Exists(GetModulePath(packagePath));
        }

        protected override async Task<bool> DoRun()
        {
            bool result = false;            

            CheckSimulators();
            if (ModuleItems.Count == 0)
            {
                Model.SetError("No Simulators found for Module Installation!");
                return result;
            }
            else
            {
                Model.AddMessage(new TaskMessage($"Found {ModuleItems.Count} Simulators/Installations.", false, FontWeights.Normal), true, false);
                await Task.Delay(300);
            }

            CheckModuleVersion();
            if (ModuleItems.Count > 0)
            {
                await Task.Delay(750);
                result = await InstallUpdateModules();
                if (result)
                    Model.State = TaskState.COMPLETED;
            }
            else
            {
                Model.SetSuccess("CommBus Modules installed & updated for configured Simulators.");
                result = true;
            }
                
            return result;
        }

        protected virtual void CheckSimulators()
        {
            if (IsOptionAll || ModuleInstallOption == CommModuleOption.Only2020Steam || ModuleInstallOption == CommModuleOption.Only2020MStore)
                CheckSimulator(Simulator.MSFS2020);
            if (IsOptionAll || ModuleInstallOption == CommModuleOption.Only2024Steam || ModuleInstallOption == CommModuleOption.Only2024MStore)
                CheckSimulator(Simulator.MSFS2024);
            if (ModuleInstallOption == CommModuleOption.UpdateExisting)
            {
                Logger.Debug($"UpdateExisting only");                
                var list = FindInstalledModules();
                Logger.Debug($"Found {list.Count} Modules");
                ModuleItems.AddRange(list);
            }
        }

        public static List<ModuleItem> FindInstalledModules()
        {
            var list = new List<ModuleItem>();

            Simulator[] simulators = { Simulator.MSFS2020, Simulator.MSFS2024 };
            foreach (var simulator in simulators)
            {
                if (FuncMsfs.CheckInstalledMsfs(simulator, SimulatorStore.All, out Dictionary<SimulatorStore, string> paths))
                {
                    foreach (var path in paths)
                    {
                        if (HasModulePath(path.Value))
                        {
                            list.Add(new ModuleItem() { Simulator = simulator, Store = path.Key, PackagePath = path.Value });
                            Logger.Debug($"Found installed CommModule on {simulator} {path.Key}");
                        }
                    }
                }
            }

            return list;
        }

        protected virtual void CheckSimulator(Simulator simulator)
        {
            var store = SimulatorStore.All;
            if (IsOptionSteam)
                store = SimulatorStore.Steam;
            else if (IsOptionMStore)
                store = SimulatorStore.MsStore;

            if (FuncMsfs.CheckInstalledMsfs(simulator, store, out Dictionary<SimulatorStore, string> paths))
            {
                foreach (var path in paths)
                {
                    if (path.Key == SimulatorStore.Steam && (IsOptionAll || IsOptionSteam))
                        ModuleItems.Add(new ModuleItem() { Simulator = simulator, Store = SimulatorStore.Steam, PackagePath = path.Value });
                    if (path.Key == SimulatorStore.MsStore && (IsOptionAll || IsOptionMStore))
                        ModuleItems.Add(new ModuleItem() { Simulator = simulator, Store = SimulatorStore.MsStore, PackagePath = path.Value });
                }
            }
        }

        protected virtual void CheckModuleVersion()
        {
            var items = new List<ModuleItem>();
            foreach (var item in ModuleItems)
            {
                if (HasModulePath(item.PackagePath))
                {
                    item.Exists = true;
                    if (!FuncMsfs.CheckPackageVersion(item.PackagePath, CommModuleName, ModuleVersion))
                    {
                        Logger.Debug($"CommModule on {item.Simulator} {item.Store} is outdated");
                        Model.AddMessage(new TaskMessage($"CommBus Module on {item.Simulator} {item.Store} is outdated!", false, FontWeights.DemiBold), false, false);
                        Model.State = TaskState.WAITING;
                        items.Add(item);
                    }
                    else
                    {
                        if (ForceCommModuleUpdate)
                        {
                            Model.AddMessage(new TaskMessage($"Force Update for CommBus Module on {item.Simulator} {item.Store}.", false, FontWeights.Normal), false, false);
                            Model.State = TaskState.WAITING;
                            items.Add(item);
                        }
                        else
                            Model.AddMessage(new TaskMessage($"CommBus Module on {item.Simulator} {item.Store} is installed and updated.", false, FontWeights.Normal), false, false);
                    }
                }
                else
                {
                    Logger.Debug($"CommModule on {item.Simulator} {item.Store} is not installed");
                    Model.AddMessage(new TaskMessage($"CommBus Module on {item.Simulator} {item.Store} is not installed!", false, FontWeights.DemiBold), false, false);
                    Model.State = TaskState.WAITING;
                    items.Add(item);
                }
            }

            ModuleItems.Clear();
            ModuleItems.AddRange(items);
        }

        public static bool SetupPossible()
        {
            return !FuncMsfs.IsRunning();
        }

        protected virtual async Task<bool> InstallUpdateModules()
        {
            bool setupAllowed = false;
            bool result = false;
            if (!SetupPossible())
            {
                Model.AddMessage("Installation not possible while MSFS is running!", false, false, false, FontWeights.DemiBold);
                Model.AddMessage("Click Retry when MSFS is closed (or cancel the Installation).");
                var interaction = new TaskInteraction(Model);
                interaction.AddInteraction("Retry", InteractionResponse.RETRY);

                if (await interaction.WaitOnResponse(Token, InteractionResponse.RETRY) && SetupPossible())
                {
                    Model.Links.Clear();
                    setupAllowed = true;
                }
                else
                {
                    Model.Links.Clear();
                    Model.SetError("MSFS is still running!");
                }
            }
            else
                setupAllowed = true;

            if (!setupAllowed)
                return result;

            foreach (var item in ModuleItems)
            {
                Model.AddMessage(new TaskMessage($"{(item.Exists ? "Updating" : "Installing")} CommBus Module on {item.Simulator} {item.Store} ...", false, FontWeights.Normal), false, false);
                if (!ExtractModuleArchive(item))
                    return result;
                else
                {
                    Model.AddMessage(new TaskMessage($"CommBus Module on {item.Simulator} {item.Store} {(item.Exists ? "updated" : "installed")}!", true, FontWeights.DemiBold), false, false);
                    Model.DisplayInSummary = true;
                }
            }

            result = true;
            return result;
        }

        protected virtual string GetAssemblyPath(string file)
        {
            return $"Installer.Payload.{file}";
        }

        protected virtual Stream GetModuleArchive(Simulator simulator)
        {
            string sim = "2020";
            if (simulator == Simulator.MSFS2024)
                sim = "2024";
            return AssemblyTools.GetStreamFromAssembly(GetAssemblyPath($"{Config.CommModuleName}-{sim}.zip"));
        }

        protected virtual bool ExtractModuleArchive(ModuleItem item)
        {
            bool result = false;

            string modulePath = GetModulePath(item.PackagePath);
            if (Directory.Exists(modulePath))
            {
                Logger.Debug($"Delete Module {modulePath}");
                Directory.Delete(modulePath, true);
            }

            using (var stream = GetModuleArchive(item.Simulator))
            {
                if (stream == null)
                {
                    Model.SetError("Could not retrieve ModuleArchive Stream from Assembly!");
                    return result;
                }

                Logger.Debug($"Extract Module to {item.PackagePath}");
                result = FuncZip.ExtractZipStream(item.PackagePath, stream, modulePath, true);
            }

            return result;
        }
    }
}
