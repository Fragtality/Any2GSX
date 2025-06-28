using Any2GSX.PluginInterface.Interfaces;
using CFIT.SimConnectLib.Modules;
using System;

namespace Any2GSX.PluginInterface
{
    public abstract class AppPlugin(IAppResources appResources)
    {
        public abstract string Id { get; }
        public abstract PluginType Type { get; }
        public abstract Type GetAircraftInterface(string aircraftString);


        public static AppPlugin Instance { get; set; }
        public virtual IAppResources AppResources { get; } = appResources;
        public virtual Type SimConnectModuleType => null;
        public virtual SimConnectModule SimConnectModule { get; set; } = null;
        public virtual string AircraftString => AppResources.AircraftString;
    }
}
