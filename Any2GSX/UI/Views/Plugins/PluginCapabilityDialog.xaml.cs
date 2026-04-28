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
        public virtual bool NeedsConfirmation { get; set; } = false;
        protected virtual bool ReadConfirmed { get; set; } = false;

        public PluginCapabilityDialog(PluginManifest plugin, bool needConfirm = false)
        {
            InitializeComponent();
            Plugin = plugin;
            this.DataContext = Plugin;
            NeedsConfirmation = needConfirm;

            Title = $"{Plugin.Id} Plugin Capabilities";
            ButtonClose.Click += OnButtonCloseClicked;
            CheckBoxConfirm.Checked += OnCheckboxConfirmChecked;
            this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
            SetCapabilities();
            MaxHeight = SystemParameters.PrimaryScreenHeight - 196;
        }

        protected virtual void OnCheckboxConfirmChecked(object sender, RoutedEventArgs e)
        {
            ReadConfirmed = CheckBoxConfirm?.IsChecked == true;
        }

        protected virtual void OnButtonCloseClicked(object sender, RoutedEventArgs e)
        {
            if ((NeedsConfirmation && ReadConfirmed) || !NeedsConfirmation)
                this.Close();
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
            if (Capabilities.FuelSync != SyncType.ManualNone)
                sb.AppendLine($"• Fuel-Sync by {GetSyncString(Capabilities.FuelSync)}");
            if (Capabilities.CanSetPayload)
                sb.AppendLine("• Reset Payload");
            if (Capabilities.PayloadSync != SyncType.ManualNone)
                sb.AppendLine($"• Payload-Sync by {GetSyncString(Capabilities.PayloadSync)}");
            if (Capabilities.DoorHandling != SyncType.ManualNone)
                sb.AppendLine($"• Door-Handling by {GetSyncString(Capabilities.DoorHandling)}");
            if (Capabilities.DoorsSynced != DoorTypeSynced.None)
                sb.AppendLine($"• Door Types {GetDoorsSyncString(Capabilities.DoorsSynced)}");
            if (Capabilities.DoorsCloseAll)
                sb.AppendLine($"• Door Close All");
            if (Capabilities.GroundEquipmentHandling != GroundEquipment.ManualNone)
                sb.AppendLine($"• Equipment-Handling of {GetEquipString(Capabilities.GroundEquipmentHandling)}");

            BlockCapabilities.Text = sb.ToString();
        }

        protected virtual string GetSyncString(SyncType types)
        {
            StringBuilder sb = new();
            bool first = true;
            foreach (var value in Enum.GetValues<SyncType>())
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

        protected virtual string GetDoorsSyncString(DoorTypeSynced types)
        {
            StringBuilder sb = new();
            bool first = true;
            foreach (var value in Enum.GetValues<DoorTypeSynced>())
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
            MaxWidth = appWindow.ActualWidth + 196;
            BorderBrush = SystemColors.ActiveBorderBrush;
            BorderThickness = new Thickness(2);
            if (NeedsConfirmation)
                CheckBoxConfirm.Visibility = Visibility.Visible;
        }
    }
}
