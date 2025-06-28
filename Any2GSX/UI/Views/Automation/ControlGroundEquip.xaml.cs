using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlGroundEquip : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlGroundEquip(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.ChockDelayMin), InputChockMinimum, new RealInvariantConverter("10"), new ValidationRuleRange<int>(1, 30));
            ViewModel.BindElement(nameof(ViewModel.ChockDelayMax), InputChockMaximum, new RealInvariantConverter("20"), new ValidationRuleRange<int>(2, 120));
            ViewModel.BindElement(nameof(ViewModel.FinalDelayMin), InputFinalMinimum, new RealInvariantConverter("90"), new ValidationRuleRange<int>(1, 30));
            ViewModel.BindElement(nameof(ViewModel.FinalDelayMax), InputFinalMaximum, new RealInvariantConverter("150"), new ValidationRuleRange<int>(2, 180));
        }
    }
}
