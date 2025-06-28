using Any2GSX.PluginInterface;
using CFIT.AppTools;
using System;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Any2GSX.UI.Views.Plugins
{
    public partial class PluginCapabilityDialog : Window
    {
        protected virtual PluginManifest Plugin { get; }
        protected virtual PluginCapabilities Capabilities => Plugin.Capabilities;

        public PluginCapabilityDialog(PluginManifest plugin)
        {
            InitializeComponent();
            Plugin = plugin;
            this.DataContext = Plugin;

            Title = $"{Plugin.Id} Plugin Capabilities";
            ButtonClose.Click += (_, _) => this.Close();
            this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
            SetCapabilities();
            MaxHeight = SystemParameters.PrimaryScreenHeight - 256;
        }

        protected virtual void SetCapabilities()
        {
            StringBuilder sb = new();

            if (Plugin.AircraftProfileEntries?.Count > 0)
                sb.AppendLine($"• Includes GSX Aircraft Profile(s) {string.Join(" & ", Plugin.AircraftProfileEntries)}");
            if (Capabilities.HasSmartButton)
                sb.AppendLine("• SmartButton Requests");
            if (Capabilities.VolumeControl)
                sb.AppendLine("• Volume Control");
            if (Capabilities.HasFobSaveRestore)
                sb.AppendLine("• Save/Load FOB");
            if (Capabilities.FuelSync != SynchType.ManualNone)
                sb.AppendLine($"• Fuel-Sync by {GetSynchString(Capabilities.FuelSync)}");
            if (Capabilities.CanSetPayload)
                sb.AppendLine("• Reset Payload");
            if (Capabilities.PayloadSync != SynchType.ManualNone)
                sb.AppendLine($"• Payload-Sync by {GetSynchString(Capabilities.PayloadSync)}");
            if (Capabilities.DoorHandling != SynchType.ManualNone)
                sb.AppendLine($"• Door-Handling by {GetSynchString(Capabilities.DoorHandling)}");
            if (Capabilities.GroundEquipmentHandling != GroundEquipment.ManualNone)
                sb.AppendLine($"• Equipment-Handling of {GetEquipString(Capabilities.GroundEquipmentHandling)}");

            BlockCapabilities.Text = sb.ToString();
        }

        protected virtual string GetSynchString(SynchType types)
        {
            StringBuilder sb = new ();
            bool first = true;
            foreach (var value in Enum.GetValues<SynchType>())
            {
                if (types.HasFlag(value))
                {
                    if (first)
                    {
                        sb.Append(value.ToString());
                        first = false;
                    }
                    else
                        sb.Append($" | {value}");
                }
            }

            return sb.ToString();
        }

        protected virtual string GetEquipString(GroundEquipment types)
        {
            StringBuilder sb = new();
            bool first = true;
            foreach (var value in Enum.GetValues<GroundEquipment>())
            {
                if (types.HasFlag(value))
                {
                    if (first)
                    {
                        sb.Append(value.ToString());
                        first = false;
                    }
                    else
                        sb.Append($", {value}");
                }
            }

            return sb.ToString();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            var appWindow = AppService.Instance.App.AppWindow;
            Top = appWindow.Top + (appWindow.ActualHeight / 2.0) - (this.ActualHeight / 2.0);
            Left = appWindow.Left + (appWindow.ActualWidth / 2.0) - (this.ActualWidth / 2.0);
            MaxWidth = appWindow.ActualWidth + 96;
            BorderBrush = SystemColors.ActiveBorderBrush;
            BorderThickness = new Thickness(2);
        }
    }
}
