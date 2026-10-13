using System.Collections.Generic;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
    public enum GsxMessageType
    {
        unknown = 0,
        hello = 1,
        snapshot = 2,
        patch = 3,
        result = 4,
        @event = 5,
    }

    public enum GsxPayloadName
    {
        unknown = 0,
        startup = 1,
        engine = 2,
        state = 3,
        stateText = 4,
        statusHtml = 5,
        settings = 6,
        handlerData = 7,
        airport = 8,
        parking = 9,
        gateProperties = 10,
        aircraft = 11,
        airline = 12,
        simbrief = 13,
        services = 14,
        menuShown = 15,
        menu = 16,
        message = 17,
        search = 18,
        commandIcons = 19,
        commandIconsSvg = 20,

    }

    public enum GsxDataGateType
    {
        RAMP_GA = 1,
        RAMP_GA_SMALL = 2,
        RAMP_GA_MEDIUM = 3,
        RAMP_GA_LARGE = 4,
        RAMP_CARGO = 5,
        RAMP_MIL_CARGO = 6,
        RAMP_MIL_COMBAT = 7,
        GATE_SMALL = 8,
        GATE_MEDIUM = 9,
        GATE_HEAVY = 10,
        DOCK_GA = 11,
        FUEL = 12,
        VEHICLE = 13,
        RAMP_GA_EXTRA = 14,
        GATE_EXTRA = 15,
    }

    public enum GsxIndexExitPax //-1
    {
        PaxDoor1 = 0,
        PaxDoor2 = 1,
        PaxDoor3 = 2,
        PaxDoor4 = 3,
    }

    public enum GsxIndexExitService //-4
    {
        ServiceDoor1 = 0,
        ServiceDoor2 = 1,
    }

    public enum GsxIndexExitCargo //-7  
    {
        CargoDoor1 = 0,
        CargoDoor2 = 1,
        CargoDoor3Main = 2,
    }

    public enum GsxCommandRunType
    {
        CustomizeAirport = 1,
        CustomizeAircraft = 2,
        RestartCouatl = 3,
        ReloadSimbrief = 4,
    }

    public static class GsxDoorIndexMappings
    {
        public static Dictionary<GsxDoor, GsxIndexExitPax> PaxDoors { get; } = new()
        {
            { GsxDoor.PaxDoor1, GsxIndexExitPax.PaxDoor1 },
            { GsxDoor.PaxDoor2, GsxIndexExitPax.PaxDoor2 },
            { GsxDoor.PaxDoor3, GsxIndexExitPax.PaxDoor3 },
            { GsxDoor.PaxDoor4, GsxIndexExitPax.PaxDoor4 },
        };
    }
}
