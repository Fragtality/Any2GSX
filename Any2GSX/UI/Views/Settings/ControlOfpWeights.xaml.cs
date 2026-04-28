using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ControlOfpWeights : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, ModelSavedFuelItem> ViewModelSelector { get; }

        public ControlOfpWeights(ModelSettings viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindElement(nameof(ViewModel.SimbriefUser), InputSimBriefUser, null, new ValidationRuleString());
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetPercent), InputFuelPercent, "2", new ValidationRuleRange<double>(0, 50));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelCompareVariance), InputFuelVariance, "50", new ValidationRuleRange<double>(10, 100));

            InputSimBriefUser.KeyUp += OnSimbriefKeyUp;

            ViewModelSelector = new(ListSavedFuel, ViewModel.ModelSavedFuel);
            ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, GetValuePair, () => !string.IsNullOrWhiteSpace(InputSavedFuel?.Text ?? ""));
            ViewModelSelector.BindTextElement(InputSavedFuel, "Value", "", new TextUnitConverter(), true);
            ViewModelSelector.BindRemoveButton(ButtonRemove, () => ListSavedFuel?.SelectedIndex != -1);
        }

        protected virtual KeyValuePair<string, double> GetValuePair()
        {
            double value = 1;
            try
            {
                value = (double)new TextUnitConverter().Convert(InputSavedFuel?.Text ?? "", typeof(double), null, null);
            }
            catch { }

            return new KeyValuePair<string, double>(ViewModelSelector.SelectedItem.Key, value);
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
