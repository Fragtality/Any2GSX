using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Installer.Worker
{
    public class WorkerPilotsdeckProfile : TaskWorker<Config>
    {
        public const string ProfileFile = "GSX.ppp";
        protected virtual string ExtractPath { get; set; } = "";

        public WorkerPilotsdeckProfile(Config config) : base(config, "Pilotsdeck Profile", "Extract Profile ...")
        {
            Model.DisplayCompleted = true;
            Model.DisplayInSummary = true;
        }

        protected override async Task<bool> DoRun()
        {
            bool result = ExtractProfileArchive();
            if (result)
            {
                Model.SetState("Open ProfileManager ...");
                result = await InstallProfile();
                if (result)
                    Model.SetSuccess("Use the ProfileManager App to install the Profile.");
            }

            return result;
        }

        protected virtual bool ExtractProfileArchive()
        {
            using (var stream = AssemblyTools.GetStreamFromAssembly($"Installer.Payload.{ProfileFile}"))
            {
                if (stream == null)
                {
                    Model.SetError("Could not retrieve ProfileArchive Stream from Assembly!");
                    return false;
                }

                ExtractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), ProfileFile);
                Logger.Debug($"Save Profile to {ExtractPath}");
                using (var fileStream = new FileStream(ExtractPath, FileMode.Create))
                    stream.CopyTo(fileStream);

                return File.Exists(ExtractPath);
            }
        }

        protected virtual async Task<bool> InstallProfile()
        {
            string managerBinary = Path.Combine(Config.PilotsdeckPath, "ProfileManager.exe");
            string path = ExtractPath;
            Sys.StartProcess(managerBinary, Config.PilotsdeckPath, path);
            await Task.Delay(1000);

            return Sys.GetProcessRunning("ProfileManager");
        }
    }
}
