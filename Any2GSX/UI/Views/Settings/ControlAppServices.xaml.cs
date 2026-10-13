using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ControlAppServices : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ControlAppServices(ModelSettings viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
            ViewModel.BindStringInteger(nameof(ViewModel.DeckClearedMenuRefresh), InputClearDelay, "5", new ValidationRuleRange<int>(0, 60));
            ViewModel.BindStringInteger(nameof(ViewModel.AudioDeviceCheckInterval), InputAudioScanDevice, "60000", new ValidationRuleRange<int>(5000, 600000));
            ViewModel.BindStringInteger(nameof(ViewModel.AudioProcessCheckInterval), InputAudioScanProcess, "2500", new ValidationRuleRange<int>(1000, 120000));
            ViewModel.BindStringInteger(nameof(ViewModel.PortFindRetries), InputPortRetries, "5", new ValidationRuleRange<int>(1, 10));
            ViewModel.BindStringInteger(nameof(ViewModel.PortFindRetryInterval), InputPortRetryInterval, "1", new ValidationRuleRange<int>(1, 10));
            ViewModel.BindStringInteger(nameof(ViewModel.PortMin), InputPortMin, "10000", new ValidationRuleRange<int>(1025, 49151));
            ViewModel.BindStringInteger(nameof(ViewModel.PortMax), InputPortMax, "49151", new ValidationRuleRange<int>(1025, 49151));
            ViewModel.BindElement(nameof(ViewModel.DeckUrlBase), InputDeckUrl, null, new ValidationRuleString());
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
