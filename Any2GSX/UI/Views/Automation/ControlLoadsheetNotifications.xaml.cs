using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlLoadsheetNotifications : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlLoadsheetNotifications(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.FinalDelayMin), InputFinalMinimum, new RealInvariantConverter("90"), new ValidationRuleRange<int>(1, 30));
            ViewModel.BindElement(nameof(ViewModel.FinalDelayMax), InputFinalMaximum, new RealInvariantConverter("150"), new ValidationRuleRange<int>(2, 180));

            Tag = "LS & Notifications";
            ToolTip = "Configure the Final Loadsheet Delay and Notifications to the Cockpit.";
        }
    }
}
