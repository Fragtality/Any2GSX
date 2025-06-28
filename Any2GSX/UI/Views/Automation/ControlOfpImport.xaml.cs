using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlOfpImport : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlOfpImport(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.RandomizePaxMaxDiff), InputPaxMaxDiff, new RealInvariantConverter("5"), new ValidationRuleRange<int>(0, 10));
            ViewModel.BindElement(nameof(ViewModel.DelayTurnAroundSeconds), InputDelayTurn, new RealInvariantConverter("90"), new ValidationRuleRange<int>(20, 600));
            ViewModel.BindElement(nameof(ViewModel.DelayTurnRecheckSeconds), InputDelayRecheck, new RealInvariantConverter("30"), new ValidationRuleRange<int>(10, 300));
        }
    }
}
