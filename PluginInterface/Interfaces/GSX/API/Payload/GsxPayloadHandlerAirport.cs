using CFIT.AppLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Any2GSX.PluginInterface.Interfaces.GSX.API.Payload
{
#pragma warning disable IDE1006

    public class GsxPayloadHandlerAirport
    {
        public virtual string USER_CUSTOMIZATION_DATASOURCE_NAME { get; set; }
        public virtual string icao { get; set; }
        public virtual string name { get; set; }
        public virtual List<string> dataSourcesAvailable { get; set; }
        public virtual List<GsxPayloadHandlerDeiceInfo> deIceAreas { get; set; }
        public virtual List<GsxPayloadHandlerGateInfo> parkings { get; set; }

        public virtual Dictionary<string, GsxPayloadHandlerGateInfo> GetParkings()
        {
            Dictionary<string, GsxPayloadHandlerGateInfo> dict = [];
            foreach (var parking in parkings)
                dict.TryAdd(parking.uiGateName.Trim(), parking);

            return dict;
        }

        public virtual Dictionary<string, Dictionary<string, GsxPayloadHandlerGateInfo>> GetParkingsGrouped()
        {
            Dictionary<string, Dictionary<string, GsxPayloadHandlerGateInfo>> dict = [];
            foreach (var parking in parkings)
            {
                var terminal = parking.uiTerminalName.Trim();
                dict.TryAdd(terminal, []);
                dict[terminal].TryAdd(terminal, parking);
            }

            return dict;
        }
    }

    public class GsxPayloadHandlerGateInfo
    {
        public virtual int index { get; set; }
        [JsonIgnore]
        public virtual string Name => !string.IsNullOrWhiteSpace(uiName) ? uiName : uiGateName;
        public virtual string uiName { get; set; } = null;
        public virtual string uiGateName { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
        public virtual string apiGateName => NormalizeName();
        public virtual string uiType { get; set; }
        public virtual int uiGroup { get; set; }
        public virtual string uiTerminalName { get; set; }
        public virtual string NormalizeName()
        {
            return NormalizeName(uiGateName);
        }

        [JsonIgnore]
        public static Regex RegexGate { get; } = new(@".*(\w*)\s*([A-Z]{0,2})\s*([0-9]{1,3})\s*([A-Z]{0,2})$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        public static string NormalizeName(string uiGateName)
        {
            string result = uiGateName;

            try
            {
                var matches = RegexGate.Matches(uiGateName);
                if (matches?.Count > 0 && matches[^1].Groups.Count == 5)
                {
                    var group = matches[^1].Groups;

                    var title = "Gate ";
                    if (!string.IsNullOrWhiteSpace(group[1].Value))
                        title = $"{group[1].Value} ";

                    if (!string.IsNullOrWhiteSpace(group[2].Value) && !string.IsNullOrWhiteSpace(group[4].Value))
                        result = $"{title}{group[2].Value} {group[3].Value}{group[4].Value}";
                    else if (string.IsNullOrWhiteSpace(group[2].Value) && !string.IsNullOrWhiteSpace(group[4].Value))
                        result = $"{title}{group[3].Value}{group[4].Value}";
                    else if (!string.IsNullOrWhiteSpace(group[2].Value))
                        result = $"{title}{group[2].Value} {group[3].Value}";
                    else
                        result = $"{title}{group[3].Value}";
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }
    }

    public class GsxPayloadHandlerDeiceInfo
    {
        public virtual int index { get; set; }
        public virtual bool isParking { get; set; }
        public virtual string uiName { get; set; }
    }

    public class GsxPayloadHandlerGate
    {
        public virtual JsonValue? pushback { get; set; } // 0 = none | 1 = left | 2 = right | 3 = both
        public virtual List<GsxPayloadPushLabel> pushbackAddPos { get; set; } = [];
        public virtual List<string> pushbackLabels { get; set; } = [];
        [JsonIgnore]
        public virtual List<string> PushbackDirections => [.. (pushbackAddPos?.Select(p => p.label) ?? []), .. GetDefaultLabelsFiltered(), .. GsxConstants.MenuPushStaticOptions];
        public virtual JsonValue? pushbackType { get; set; } // -1 = disabled | 0 = any | 1 = towbar | 2 = towbarless | 3 = mototok
        [JsonIgnore]
        public virtual bool HasLeftPush => pushback != null && pushback!?.GetValueKind() == JsonValueKind.Number && (pushback!?.GetValue<int>() == 1 || pushback!?.GetValue<int>() == 3);
        [JsonIgnore]
        public virtual bool HasRightPush => pushback != null && pushback!?.GetValueKind() == JsonValueKind.Number && (pushback!?.GetValue<int>() == 2 || pushback!?.GetValue<int>() == 3);
        [JsonIgnore]
        public virtual bool IsPushbackEnabled => pushbackType != null && pushbackType!?.GetValueKind() == JsonValueKind.Number && pushbackType!?.GetValue<int>() != -1;
        public virtual GsxDataGateType? type { get; set; }
        [JsonIgnore]
        public virtual bool IsCargo => type != null && (type == GsxDataGateType.RAMP_CARGO || type == GsxDataGateType.RAMP_MIL_CARGO);
        public virtual int? index { get; set; }
        public virtual string uiName { get; set; }
        public virtual string uiGateName { get; set; }
        public virtual string uiTerminalName { get; set; }
        public virtual string uiType { get; set; }
        public virtual bool? undergroundRefueling { get; set; }
        public virtual bool? noPassengerStairs { get; set; }

        public virtual List<string> GetDefaultLabelsFiltered()
        {
            List<string> list = [];
            if (HasLeftPush && pushbackLabels?.Count > 0)
                list.Add(pushbackLabels!?[0] ?? "LEFT");
            if (HasRightPush && pushbackLabels?.Count > 1)
                list.Add(pushbackLabels!?[1] ?? "RIGHT");

            return list;
        }

    }

    public class GsxPayloadPushLabel
    {
        public virtual string label { get; set; } = "";
    }

#pragma warning restore IDE1006
}
