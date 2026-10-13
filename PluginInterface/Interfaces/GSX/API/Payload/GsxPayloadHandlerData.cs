namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadHandlerData
    {
        public virtual GsxPayloadHandlerAircraft aircraft { get; set; }
        public virtual GsxPayloadHandlerAirport airport { get; set; }
        public virtual GsxPayloadHandlerGate gate { get; set; }
    }

#pragma warning restore IDE1006
}
