using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlGateDoors : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlGateDoors(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.DoorCargoOpenCloseDelay), InputDelayCargoDoors, new RealInvariantConverter("2"), new ValidationRuleRange<int>(0, 60));
            ViewModel.BindElement(nameof(ViewModel.DelayCallRefuelAfterStair), InputDelayCallRefuel, new RealInvariantConverter("30"), new ValidationRuleRange<int>(1, 120));

            Tag = "Gate & Doors";
            ToolTip = "Configure Doors and Jetway/Stairs Handling.";
        }
    }
}
