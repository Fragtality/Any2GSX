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
            ViewModel.BindStringInteger(nameof(ViewModel.PortBase), InputPortBase, "60060");
            ViewModel.BindStringInteger(nameof(ViewModel.PortRange), InputPortRange, "10");
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
