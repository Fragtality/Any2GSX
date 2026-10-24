using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces.GSX
{
    public interface IGsxGate
    {
        public string AutoSelectionPushback { get; }
        public bool HasSelectionPushback { get; }
        public JsonNode AutoSelectionArrivalGate { get; }
        public bool HasSelectionArrivalGate { get; }
        public string LastGateSelection { get; }
        public bool HasGateSelection { get; }
        public string LastGateActive { get; }
        public bool HasGateActive => !string.IsNullOrWhiteSpace(LastGateActive);

        public event Func<string, Task> OnPushDirectionChanged;
        public event Func<string, Task> OnArrivalGateChanged;

        public void SetPushDirection(string direction);

        public void SetArrivalGate(JsonNode node);

        public void SetArrivalGateName(string name);

        public void SetArrivalGateNumber(int number);
    }
}
