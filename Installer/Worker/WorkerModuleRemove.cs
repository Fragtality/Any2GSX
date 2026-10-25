using CFIT.AppLogger;
using CFIT.Installer.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Installer.Worker
{
    public class WorkerModuleRemove<C> : TaskWorker<Config>
    {
        public virtual List<ModuleItem> ModuleItems { get; } = new List<ModuleItem>();
        protected virtual List<string> RemovedPaths { get; } = new List<string>();

        public WorkerModuleRemove(Config config) : base(config, $"CommBus Sim Module", "Finding Module Installations ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
        }

        protected override async Task<bool> DoRun()
        {
            bool result;

            CheckSimulators();
            if (ModuleItems.Count > 0)
            {
                Model.AddMessage(new TaskMessage($"Found {ModuleItems.Count} Simulators/Installations.", false, FontWeights.Normal), true, false);
                await Task.Delay(500);
                result = RemoveModules();
            }
            else
                result = true;

            if (result)
                Model.SetSuccess("All CommBus Modules removed!");
            return result;
        }

        protected virtual void CheckSimulators()
        {
            var list = WorkerModuleInstall<Config>.FindInstalledModules(Config, true, out _);
            Logger.Debug($"Found {list.Count} Modules");
            ModuleItems.AddRange(list);
        }

        protected virtual bool RemoveModules()
        {
            foreach (var item in ModuleItems)
            {
                if (!RemoveModule(item))
                {
                    Model.SetError($"Module on Simulator {item.Simulator} {item.Store} still exists!");
                    return false;
                }
            }

            return true;
        }

        protected virtual bool RemoveModule(ModuleItem item)
        {
            string modulePath = WorkerModuleInstall<Config>.GetModulePath(item.PackagePath);
            if (RemovedPaths.Contains(modulePath) && !Directory.Exists(modulePath))
                return true;

            Model.Message = $"Removing Module on Simulator {item.Simulator} {item.Store} ...";
            Logger.Debug($"Using ModulePath {modulePath}");
            Directory.Delete(modulePath, true);
            if (!Directory.Exists(modulePath))
            {
                RemovedPaths.Add(modulePath);
                return true;
            }
            else
                return false;
        }
    }
}
