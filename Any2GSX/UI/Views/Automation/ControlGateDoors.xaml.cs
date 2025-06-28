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

            ViewModel.BindElement(nameof(ViewModel.DelayCallRefuelAfterStair), InputDelayRefuel, new RealInvariantConverter("30"), new ValidationRuleRange<int>(1,120));
        }
    }
}
