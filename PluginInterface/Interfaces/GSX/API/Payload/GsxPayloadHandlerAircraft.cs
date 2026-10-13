using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006
    public class GsxPayloadHandlerAircraft
    {
        public virtual string aircraftDbKey { get; set; }
        public virtual string dataSourceUsed { get; set; }
        public virtual List<string> dataSourcesAvailable { get; set; }
        public virtual Dictionary<string, List<GsxPayloadAircraftExit>> exits { get; set; }
        public virtual List<double> fuelPos { get; set; }
        [JsonIgnore]
        public virtual bool IsRefuelOnStairSide => fuelPos!?[0] < 0.0;
        public virtual string icaoType { get; set; }
        public virtual int idMajor { get; set; }
        public virtual int idMinor { get; set; }
        public virtual bool isCargo { get; set; }
        public virtual List<double> lavatoryPos { get; set; }
        [JsonIgnore]
        public virtual bool IsLavatorOnStairSide => lavatoryPos!?[0] < 0.0;
        public virtual int numberOfEngines { get; set; }
        public virtual int preferredExit { get; set; }
        public virtual bool refueling { get; set; }
        public virtual List<double> waterPos { get; set; }
        [JsonIgnore]
        public virtual bool IsWaterOnStairSide => waterPos!?[0] < 0.0;

        public virtual bool CalculateStairClearence(List<double> pos)
        {
            pos ??= [0, 0, 0];
            if (pos[0] >= 0.0)
                return true;

            List<GsxPayloadAircraftExit> list = null;
            if (exits?.TryGetValue("0", out list) == false)
                return true;
            if (list?.Count < 4 || list!?[3] == null || list!?[3]!?.height == null || list![3]!?.height <= 0)
                return true;

            return pos[0] > list![3].y && Math.Abs(pos[0]) < ((list![3]!?.height / Math.Tan(GsxConstants.ConstantStairAngle)) - GsxConstants.ConstantStairWorkArea + list![3].y);
        }

        public virtual bool HasLavatoryConflict()
        {
            return !CalculateStairClearence(lavatoryPos);
        }

        public virtual bool HasWaterConflict()
        {
            return !CalculateStairClearence(waterPos);
        }
    }

    public class GsxPayloadAircraftExit
    {
        public virtual JsonNode code { get; set; }
        public virtual bool? doorState { get; set; }
        public virtual bool? front { get; set; }
        public virtual bool? hasCondition { get; set; }
        public virtual double? height { get; set; }
        public virtual JsonValue? index { get; set; }
        public virtual string? name { get; set; }
        public virtual int? side { get; set; }
        public virtual string? toggle { get; set; }
        public virtual double? x { get; set; }
        public virtual double? y { get; set; }
        public virtual double? z { get; set; }

        [JsonIgnore]
        public virtual bool HasDoor => index != null && index!?.GetValueKind() == JsonValueKind.Number;
        [JsonIgnore]
        public virtual bool IsTypeNoCheck => HasDoor && code?.GetValueKind() == JsonValueKind.Null;
        [JsonIgnore]
        public virtual bool IsTypeStandard => HasDoor && code?.GetValueKind() == JsonValueKind.Number;
        [JsonIgnore]
        public virtual bool IsTypeCustom => HasDoor && code?.GetValueKind() == JsonValueKind.String;
        [JsonIgnore]
        public virtual bool HasToggle => HasDoor && !string.IsNullOrWhiteSpace(toggle);
    }

#pragma warning restore IDE1006
}
