using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using System;

namespace Any2GSX.Plugins
{
    public class LuaPlugin(string id, PluginType type, IAppResources appResources) : AppPlugin(appResources)
    {
        public override string Id { get; } = id;
        public override PluginType Type { get; } = type;

        public override Type GetAircraftInterface(string aircraftString)
        {
            return null;
        }
    }
}
