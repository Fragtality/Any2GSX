using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlBriefing : UserControl
    {
        protected virtual ModelAutomation MainModel { get; }
        protected virtual ModelBriefing ViewModel { get; }
        protected virtual bool SearchActive { get; set; } = false;
        protected virtual DispatcherTimer DepartureTimer { get; }

        public ControlBriefing(ModelAutomation model)
        {
            InitializeComponent();
            MainModel = model;
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ComboboxPushDirection.SelectionChanged += OnPushDirectionChange;
            ListGates.SelectionChanged += OnGateSelectionChanged;
            InputGate.TextChanged += OnGateInputChanged;
            InputGate.KeyUp += OnInputKeyUp;

            this.KeyUp += OnKeyUp;
            this.LostFocus += OnLostFocus;
            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            DepartureTimer = new()
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            DepartureTimer.Tick += OnDepartureTimerTick;

            Tag = "Flight Briefing";
            ToolTip = "Review Weights used by Any2GSX and pre-Select Pushback Direction and Arrival Gate.";
        }

        protected virtual void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            DepartureTimer.Start();
            ViewModel.WeightRefreshTimer.Start();
        }

        protected virtual void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            DepartureTimer.Stop();
            ViewModel.WeightRefreshTimer.Stop();
        }

        protected virtual void OnDepartureTimerTick(object? sender, EventArgs e)
        {
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.TimeToDepart));
            ViewModel.NotifyPropertyChanged(nameof(ViewModel.SchedOut));
        }

        protected virtual void EndSearch(string newGate)
        {
            if (!string.IsNullOrWhiteSpace(newGate) && !newGate.Equals(ViewModel.NotAssigned))
            {
                if (ComboGateLabel.SelectedIndex > 0)
                    ViewModel.AssignedArrivalGate = $"{ComboGateLabel.SelectionBoxItem} {newGate}";
                else
                    ViewModel.AssignedArrivalGate = newGate;
            }

            ComboGateLabel.SelectedIndex = 0;
            TextGate.Visibility = Visibility.Visible;
            PanelInputGate.Visibility = Visibility.Collapsed;
            ListGates.Visibility = Visibility.Collapsed;
            SearchActive = false;
        }

        protected virtual void OnGateSearchClicked(object sender, RoutedEventArgs e)
        {
            if (!SearchActive)
            {
                TextGate.Visibility = Visibility.Collapsed;
                PanelInputGate.Visibility = Visibility.Visible;
                if (ViewModel.ArrivalGates.Count > 0 && ViewModel.AutomationController.State >= AutomationState.TaxiIn)
                    ListGates.Visibility = Visibility.Visible;
                ListGates.SelectedIndex = -1;
                InputGate.Text = ViewModel.AssignedArrivalGate?.Equals(ViewModel.NotAssigned) == false ? ViewModel.AssignedArrivalGate : "";
                string? match = ViewModel.GateLabels.FirstOrDefault(InputGate.Text.TrimStart().StartsWith);
                if (match != null)
                {
                    InputGate.Text = InputGate.Text.TrimStart()[match.Length..].TrimStart();
                    ComboGateLabel.SelectedValue = match;
                }
                ViewModel.SetGateFilter("");
                SearchActive = true;
            }
            else
            {
                EndSearch(InputGate?.Text);
            }
        }

        protected virtual void OnGateInputChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SetGateFilter(InputGate?.Text);
        }

        protected virtual void OnPushDirectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (ComboboxPushDirection.SelectedIndex == -1)
                ComboboxPushDirection.SelectedIndex = 0;
        }

        protected virtual void OnGateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListGates.SelectedIndex >= 0)
                EndSearch(ListGates?.SelectedValue?.ToString());
        }

        protected virtual void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender != this)
                EndSearch("");
        }

        protected virtual void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                EndSearch(InputGate?.Text);
        }

        protected void OnInputKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.Enter)
                EndSearch(InputGate?.Text);
        }
    }
}
