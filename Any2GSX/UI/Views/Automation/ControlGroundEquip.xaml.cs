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

            Tag = "Ground Equipment";
            ToolTip = "Configure how Ground Equipment like Chocks, GPU and PCA is handled.";
        }
    }
}
