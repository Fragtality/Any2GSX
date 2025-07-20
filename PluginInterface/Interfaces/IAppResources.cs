using CFIT.AppFramework.ResourceStores;
using CFIT.SimConnectLib.InputEvents;
using System.Threading;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IAppResources
    {
        public IConfig AppConfig { get; }
        public IProductDefinition ProductDefinition { get; }
        public CancellationToken RequestToken { get; }
        public ReceiverStore ReceiverStore { get; }
        public SimStore SimStore { get; }
        public InputEventManager InputEventManager { get; }
        public IGsxController IGsxController { get; }
        public bool IsProfileLoaded { get; }
        public ISettingProfile ISettingProfile { get; }
        public string AircraftString { get; }
        public bool IsMsfs2020 { get; }
        public bool IsMsfs2024 { get; }
        public IWeightConverter WeightConverter { get; }
        public double FuelWeightKgPerGallon { get; }
        public IFlightplan IFlightplan { get; }
        public CancellationToken Token { get; }
        public ICommBus ICommBus { get; }

    }
}
