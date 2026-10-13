using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ControlGsxParameter : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ControlGsxParameter(ModelSettings viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringInteger(nameof(ViewModel.GsxMenuStartupMaxFail), InputGsxMaxFail, "4", new ValidationRuleRange<int>(1, 16));
            ViewModel.BindStringInteger(nameof(ViewModel.SpeedTresholdTaxiIn), InputSpeedTresholdTaxiIn, "30", new ValidationRuleRange<int>(1, 100));

            ViewModel.BindStringInteger(nameof(ViewModel.DelayOpenTaxiInMenu), InputDelayOpenMenuTaxiIn, "15", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelOpenDelayUnderground), InputRefuelOpenDelayUnderground, "82", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelCloseDelayUnderground), InputRefuelCloseDelayUnderground, "10", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelOpenDelayTanker), InputRefuelOpenDelayTanker, "75", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelRefuelCloseDelayTanker), InputRefuelCloseDelayTanker, "8", new ValidationRuleRange<int>(1, 120));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelLavatoryOpenDelay), InputLavatoryOpenDelay, "35", new ValidationRuleRange<int>(1, 60));
            ViewModel.BindStringInteger(nameof(ViewModel.PanelWaterOpenDelay), InputWaterOpenDelay, "35", new ValidationRuleRange<int>(1, 60));
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
