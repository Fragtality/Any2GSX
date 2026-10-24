using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.Aircraft
{
    public class GenericAircraft(IAppResources appResources) : AircraftBase(appResources)
    {
        public virtual SettingProfile Profile => AppService.Instance.SettingProfile;

        public override Task RunInterval()
        {
            return Task.CompletedTask;
        }

        public override Task<bool> GetIsConnected()
        {
            return Task.FromResult(AppService.Instance.SimConnect.IsSessionRunning && FuelCapacityGallon > 0);
        }

        public override Task CheckConnection()
        {
            return Task.CompletedTask;
        }

        protected override Task DoInit()
        {
            return Task.CompletedTask;
        }

        protected override Task DoStop()
        {
            return Task.CompletedTask;
        }
    }
}
