using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public enum AutomationSettingControl
    {
        GateDoors = 0,
        GroundEquip,
        OfpImport,
        FuelPayload,
        GsxServices,
        OperatorSelection,
        CompanyHubs,
        LoadsheetNotifications,
        SkipQuestions,
        AircraftOptions,
    }

    public partial class ViewAutomation : UserControl, IView
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual ControlProfileSelector ControlProfileSelector { get; }
        protected virtual Dictionary<AutomationSettingControl, UserControl> SettingControls { get; } = [];

        public ViewAutomation()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            SettingControls.Add(AutomationSettingControl.GateDoors, new ControlGateDoors(ViewModel));
            SettingControls.Add(AutomationSettingControl.FuelPayload, new ControlFuelPayload(ViewModel));
            SettingControls.Add(AutomationSettingControl.GroundEquip, new ControlGroundEquip(ViewModel));
            SettingControls.Add(AutomationSettingControl.OfpImport, new ControlOfpImport(ViewModel));
            SettingControls.Add(AutomationSettingControl.GsxServices, new ControlGsxServices(ViewModel));
            SettingControls.Add(AutomationSettingControl.OperatorSelection, new ControlOperatorSelection(ViewModel));
            SettingControls.Add(AutomationSettingControl.CompanyHubs, new ControlCompanyHubs(ViewModel));
            SettingControls.Add(AutomationSettingControl.LoadsheetNotifications, new ControlLoadsheetNotifications(ViewModel));
            SettingControls.Add(AutomationSettingControl.SkipQuestions, new ControlSkipQuestions(ViewModel));
            SettingControls.Add(AutomationSettingControl.AircraftOptions, new ControlAircraftOptions(ViewModel));

            SelectorSettingGroup.ItemsSource = SettingControls;
            SelectorSettingGroup.SelectionChanged += OnSelectionChanged;

            ControlProfileSelector = new();
            ViewProfileSelector.Content = ControlProfileSelector;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectorSettingGroup?.SelectedValue is AutomationSettingControl controlKey && SettingControls.TryGetValue(controlKey, out var control))
                ViewSettingGroup.Content = control;
        }

        public virtual void Start()
        {
            if (SelectorSettingGroup?.SelectedValue is not AutomationSettingControl)
                SelectorSettingGroup.SelectedIndex = 0;
        }

        public virtual void Stop()
        {

        }
    }
}
