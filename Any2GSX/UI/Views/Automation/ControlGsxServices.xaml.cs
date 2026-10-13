using Any2GSX.PluginInterface.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlGsxServices : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlGsxServices(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
            this.PreviewKeyDown += OnPreviewKeyDown;

            Tag = "GSX Services";
            ToolTip = "Configure the GSX Services to be called.";
            this.Loaded += OnControlLoaded;

            AppService.Instance.GsxController.AutomationController.OnStateChange += (_) => ViewModel.RunOnDispatcher(OnAutomationStateChanged);
            AppService.Instance.Flightplan.OnImport += (_) => ViewModel.RunOnDispatcher(OnAutomationStateChanged);
            AppService.Instance.GsxController.AutomationController.DepartureQueue.ExternalControlChanged += () => ViewModel.RunOnDispatcher(OnExternalControlChanged);
            AppService.Instance.MessageService.Subscribe<MsgAppSessionStopped>(() => ViewModel.RunOnDispatcher(() => SetButtonResetQueueVisibility(Visibility.Visible)));
        }

        protected virtual void OnAutomationStateChanged()
        {
            try
            {
                var state = AppService.Instance.GsxController.AutomationState;
                var rundepart = AppService.Instance.GsxController.AutomationController.RunDepartureOnArrival;

                if ((state == AutomationState.Departure || (state == AutomationState.Arrival && rundepart)) && !AppService.Instance.GsxController.AutomationController.ExternalServiceControl)
                    LabelQueueInUse.Visibility = Visibility.Visible;
                else
                    LabelQueueInUse.Visibility = Visibility.Collapsed;

                if (state < AutomationState.Preparation || state == AutomationState.TurnAround)
                    ButtonResetQueue.Visibility = Visibility.Visible;
                else
                    ButtonResetQueue.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        protected virtual void SetButtonResetQueueVisibility(Visibility target)
        {
            ButtonResetQueue.Visibility = target;
        }

        protected virtual void OnExternalControlChanged()
        {
            try
            {
                var state = AppService.Instance.GsxController.AutomationController.ExternalServiceControl;
                if (state && LabelQueueInUse.Visibility == Visibility.Visible)
                    LabelQueueInUse.Visibility = Visibility.Collapsed;

                if (state)
                {
                    LabelExternalControl.Visibility = Visibility.Visible;
                    ListDepartureServices.IsEnabled = false;
                    ListDepartureServices.IsHitTestVisible = false;
                    ButtonResetQueue.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LabelExternalControl.Visibility = Visibility.Collapsed;
                    ListDepartureServices.IsEnabled = true;
                    ListDepartureServices.IsHitTestVisible = true;
                    var phase = AppService.Instance.GsxController.AutomationState;
                    var rundepart = AppService.Instance.GsxController.AutomationController.RunDepartureOnArrival;
                    if (phase == AutomationState.Departure || (phase == AutomationState.Arrival && rundepart))
                        LabelQueueInUse.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        protected virtual void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            Any2GSX.Instance.AppWindow.Deactivated += OnAppWindowDeactivated;
            ListDepartureServices.SelectionChanged += OnDepartureSelectionChanged;
            ListDepartureServices.IsKeyboardFocusWithinChanged += OnListKeyboardFocusChanged;
            OnAutomationStateChanged();
        }

        protected virtual void OnDepartureSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListDepartureServices.SelectedIndex == -1 && ViewModel.DepartureServices.WasMoved)
            {

                ListDepartureServices.SelectedIndex = ViewModel.DepartureServices.ToIndex;
                ViewModel.DepartureServices.ToIndex = -1;
            }
        }

        protected virtual void OnAppWindowDeactivated(object? sender, System.EventArgs e)
        {
            var focusRequest = new TraversalRequest(FocusNavigationDirection.Next);
            this.MoveFocus(focusRequest);
        }

        protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && ListDepartureServices.SelectedIndex != -1)
            {
                ListDepartureServices.UnselectAll();
                e.Handled = true;
            }
        }

        protected virtual void OnListKeyboardFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ListDepartureServices.SelectedIndex != -1)
            {
                ListDepartureServices.UnselectAll();
            }
        }

        protected virtual void OnServiceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ListDepartureServices.SelectedIndex != -1)
            {
                ListDepartureServices.UnselectAll();
                e.Handled = true;
            }
        }
    }
}
