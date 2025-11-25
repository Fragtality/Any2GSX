namespace Any2GSX.PluginInterface.Interfaces
{
    public enum DisplayUnit
    {
        KG = 0,
        LB = 1,
    }

    public enum DisplayUnitSource
    {
        App = 0,
        Simbrief = 1,
        Aircraft = 2,
    }

    public enum PluginType
    {
        BinaryV1 = 0,
        LuaV1 = 1,
    }
    public enum PluginStartMode
    {
        WaitConnected = 0,
        PreWalkaround = 1,
    }


    public enum AutomationState
    {
        Unknown = -1,
        SessionStart = 0,
        Preparation = 1,
        Departure = 2,
        Pushback = 3,
        TaxiOut = 4,
        Flight = 5,
        TaxiIn = 6,
        Arrival = 7,
        TurnAround = 8,
    }

    public enum GsxMenuState
    {
        UNKNOWN = 0,
        READY = 1,
        HIDE = 2,
        TIMEOUT = 3,
        DISABLED = 4
    }

    public enum GsxServiceType
    {
        Unknown = 0,
        Reposition = 1,
        Refuel = 2,
        Catering = 3,
        Boarding = 4,
        Pushback = 5,
        Deice = 6,
        Deboarding = 7,
        GPU = 8,
        Water = 9,
        Lavatory = 10,
        Jetway = 11,
        Stairs = 12,
        Cleaning = 13,
    }

    public enum GsxServiceActivation
    {
        Skip = 0,
        Manual = 1,
        AfterCalled = 2,
        AfterRequested = 3,
        AfterActive = 4,
        AfterPrevCompleted = 5,
        AfterAllCompleted = 6,
    }

    public enum GsxServiceConstraint
    {
        NoneAlways = 0,
        FirstLeg = 1,
        TurnAround = 2,
        CompanyHub = 3,
        NonCompanyHub = 4,
    }

    public enum GsxServiceState
    {
        Unknown = 0,
        Callable = 1,
        NotAvailable = 2,
        Bypassed = 3,
        Requested = 4,
        Active = 5,
        Completed = 6,
    }

    public enum GsxDoor
    {
        PaxDoor1 = 1,
        PaxDoor2 = 2,
        PaxDoor3 = 3,
        PaxDoor4 = 4,
        ServiceDoor1 = 5,
        ServiceDoor2 = 6,
        CargoDoor1 = 7,
        CargoDoor2 = 8,
        CargoDoor3Main = 9,
    }

    public enum BroadcastFlag
    {
        JS = 1,
        WASM = 2,
        DEFAULT = 3,
        SELF = 4,
        ALLWASM = 6,
        ALL = 7,
    }

    public enum Comparison
    {
        LESS,
        LESS_EQUAL,
        GREATER,
        GREATER_EQUAL,
        EQUAL,
        NOT_EQUAL,
    }
}
