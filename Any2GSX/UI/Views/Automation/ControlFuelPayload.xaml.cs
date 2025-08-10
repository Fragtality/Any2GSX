using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ValueConverter;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlFuelPayload : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlFuelPayload(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.RefuelRateKgSec), InputRefuelRate, "28");
            ViewModel.BindStringNumber(nameof(ViewModel.RefuelResetDeltaKg), InputRefuelDelta, "2500");
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetBaseKg), InputFuelReset, "2500");
            ViewModel.BindStringInteger(nameof(ViewModel.RefuelTimeTargetSeconds), InputTimeTarget, "150");
            ViewModel.BindElement(nameof(ViewModel.DefaultPilotTarget), InputPilotsDefault, new RealInvariantConverter("2"), new ValidationRuleRange<int>(0, 10));
            ViewModel.BindElement(nameof(ViewModel.DefaultCrewTarget), InputCrewDefault, new RealInvariantConverter("4"), new ValidationRuleRange<int>(0, 20));
        }
    }
}
