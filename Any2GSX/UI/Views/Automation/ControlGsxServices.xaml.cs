using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlGsxServices : UserControl, INotifyPropertyChanged
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual ViewModelSelector<ServiceConfig, ServiceConfig> ViewModelSelector { get; }
        public virtual bool HasSelection => GridDepartureServices?.SelectedIndex != -1;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ControlGsxServices(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
            ImageEdit.Source = AppWindow.IconLoader.LoadIcon("edit");

            ViewModelSelector = new(GridDepartureServices, ViewModel.DepartureServices, AppWindow.IconLoader);
            ViewModelSelector.BindTextElement(LabelServiceName, nameof(ServiceConfig.ServiceType));
            ViewModelSelector.BindMember(SelectorActivation, nameof(ServiceConfig.ServiceActivation));
            ViewModelSelector.BindTextElement(InputActivateAt, nameof(ServiceConfig.MaxTimeBeforeDeparture), "0", new TimeSpanConverter(0, 120, true));
            ViewModelSelector.BindTextElement(InputDuration, nameof(ServiceConfig.MinimumFlightDuration), "0", new TimeSpanConverter(0, 1440, true));
            ViewModelSelector.BindMember(SelectorConstraint, nameof(ServiceConfig.ServiceConstraint));
            ViewModelSelector.BindMember(CheckboxCallCargo, nameof(ServiceConfig.CallOnCargo));
            ViewModelSelector.BindTextElement(InputRunTime, nameof(ServiceConfig.MaxRunTime), "0", new TimeSpanConverter(0, 900, true));
            ViewModelSelector.BindTextElement(InputCallDelay, nameof(ServiceConfig.CallDelay), "0", new TimeSpanConverter(0, 900, false));

            ViewModelSelector.BindAddUpdateButton(ButtonEdit, null, GetItem, () => HasSelection);
            ViewModelSelector.AddUpdateCommand.Subscribe(SelectorActivation);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputActivateAt);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputDuration);
            ViewModelSelector.AddUpdateCommand.Subscribe(SelectorConstraint);
            ViewModelSelector.AddUpdateCommand.Subscribe(CheckboxCallCargo);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputRunTime);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputCallDelay);

            GridDepartureServices.SelectionChanged += (_, _) => NotifyPropertyChanged(nameof(HasSelection));

            ButtonUp.Command = new CommandWrapper(() => ViewModel.DepartureServices.MoveItem(GridDepartureServices.SelectedIndex, -1), () => GridDepartureServices?.SelectedIndex != -1).Subscribe(GridDepartureServices);
            ButtonDown.Command = new CommandWrapper(() => ViewModel.DepartureServices.MoveItem(GridDepartureServices.SelectedIndex, 1), () => GridDepartureServices?.SelectedIndex != -1).Subscribe(GridDepartureServices);

            GridDepartureServices.SizeChanged += OnGridSizeChanged;

            Tag = "GSX Services";
            ToolTip = "Configure the GSX Services to be called.";
            this.Loaded += OnControlLoaded;

            AppService.Instance.GsxController.AutomationController.OnStateChange += (_) => ViewModel.RunOnDispatcher(OnAutomationStateChanged);
            AppService.Instance.Flightplan.OnImport += (_) => ViewModel.RunOnDispatcher(OnAutomationStateChanged);
        }

        protected virtual void OnAutomationStateChanged()
        {
            try
            {
                var state = AppService.Instance.GsxController.AutomationState;
                var rundepart = AppService.Instance.GsxController.AutomationController.RunDepartureOnArrival;

                if (state == AutomationState.Departure || (state == AutomationState.Arrival && rundepart))
                    LabelQueueInUse.Visibility = Visibility.Visible;
                else
                    LabelQueueInUse.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        protected virtual void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            var baseStyle = (Style)Application.Current.FindResource(typeof(DataGridColumnHeader));
            var centerStyle = new Style(typeof(DataGridColumnHeader), baseStyle);
            centerStyle.Setters.Add(
                new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)
            );

            GridDepartureServices.Columns[2].HeaderStyle = centerStyle;
            GridDepartureServices.Columns[3].HeaderStyle = centerStyle;
            GridDepartureServices.Columns[5].HeaderStyle = centerStyle;
            GridDepartureServices.Columns[6].HeaderStyle = centerStyle;
            GridDepartureServices.Columns[7].HeaderStyle = centerStyle;

            OnAutomationStateChanged();
        }

        protected virtual void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                LabelServiceName.Width = GridDepartureServices.Columns[0].ActualWidth;
                SelectorActivation.Width = GridDepartureServices.Columns[1].ActualWidth;
                SelectorConstraint.Width = GridDepartureServices.Columns[4].ActualWidth;
                CheckboxCallCargo.Width = GridDepartureServices.Columns[5].ActualWidth - 10;
            }
            catch { }
        }

        protected virtual ServiceConfig GetItem()
        {
            try
            {
                if (GridDepartureServices?.SelectedValue is ServiceConfig serviceConfig
                    && SelectorActivation?.SelectedValue is GsxServiceActivation activation
                    && !string.IsNullOrWhiteSpace(InputActivateAt?.Text)
                    && !string.IsNullOrWhiteSpace(InputDuration?.Text)
                    && SelectorConstraint?.SelectedValue is GsxServiceConstraint constraint
                    && CheckboxCallCargo?.IsChecked != null
                    && !string.IsNullOrWhiteSpace(InputRunTime?.Text)
                    && !string.IsNullOrWhiteSpace(InputCallDelay?.Text))

                    return new ServiceConfig(serviceConfig.ServiceType,
                                             activation,
                                             (TimeSpan)new TimeSpanConverter(0, 1440, true).ConvertBack(InputDuration.Text, typeof(TimeSpan), null, null),
                                             constraint,
                                             CheckboxCallCargo.IsChecked == true,
                                             (TimeSpan)new TimeSpanConverter(0, 120, true).ConvertBack(InputActivateAt.Text, typeof(TimeSpan), null, null),
                                             (TimeSpan)new TimeSpanConverter(0, 900, true).ConvertBack(InputRunTime.Text, typeof(TimeSpan), null, null),
                                             (TimeSpan)new TimeSpanConverter(0, 900, false).ConvertBack(InputCallDelay.Text, typeof(TimeSpan), null, null));
            }
            catch { }

            return null;
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void NotifyUpdate()
        {
            NotifyPropertyChanged(string.Empty);
        }
    }
}
