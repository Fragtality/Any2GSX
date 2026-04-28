using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ControlGsxSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ControlGsxSettings(ModelSettings viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringInteger(nameof(ViewModel.GsxMenuStartupMaxFail), InputGsxMaxFail, "4", new ValidationRuleRange<int>(1, 16));
            ViewModel.BindStringInteger(nameof(ViewModel.SpeedTresholdTaxiIn), InputSpeedTresholdTaxiIn, "30", new ValidationRuleRange<int>(1, 100));

            ViewModel.BindStringInteger(nameof(ViewModel.DelayOpenTaxiInMenu), InputDelayOpenMenuTaxiIn, "15", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelOpenDelayUnderground), InputRefuelOpenDelayUnderground, "80", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelCloseDelayUnderground), InputRefuelCloseDelayUnderground, "23", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelOpenDelayTanker), InputRefuelOpenDelayTanker, "24", new ValidationRuleRange<int>(1, 60));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelCloseDelayTanker), InputRefuelCloseDelayTanker, "20", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelLavatoryOpenDelay), InputLavatoryOpenDelay, "27", new ValidationRuleRange<int>(1, 60));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelWaterOpenDelay), InputWaterOpenDelay, "27", new ValidationRuleRange<int>(1, 60));
            ViewModel.BindStringInteger(nameof(ViewModel.OperatorSelectTimeout), InputOperatorTimeout, "25", new ValidationRuleRange<int>(1, 60));
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
