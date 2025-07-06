using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.SimbriefUser), InputSimBriefUser, null, new ValidationRuleString());
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetPercent), InputFuelPercent, "2", new ValidationRuleRange<double>(0,50));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelCompareVariance), InputFuelVariance);
            ViewModel.BindStringInteger(nameof(ViewModel.PortBase), InputPortBase, "60060");
            ViewModel.BindStringInteger(nameof(ViewModel.PortRange), InputPortRange, "10");
            ViewModel.BindElement(nameof(ViewModel.DeckUrlBase), InputDeckUrl, null, new ValidationRuleString());

            InputSimBriefUser.KeyUp += OnSimbriefKeyUp;

            ViewModelSelector = new(ListSavedFuel, ViewModel.ModelSavedFuel);
            ViewModelSelector.BindRemoveButton(ButtonRemove);
        }

        protected virtual void OnSimbriefKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.BrushSimbrief));
        }

        public virtual void Start()
        {
            ViewModel.InhibitConfigSave = true;
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.SimbriefUser));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.BrushSimbrief));
            ViewModel.ModelSavedFuel.NotifyCollectionChanged();
            ViewModel.InhibitConfigSave = false;
        }

        public virtual void Stop()
        {

        }
    }
}
