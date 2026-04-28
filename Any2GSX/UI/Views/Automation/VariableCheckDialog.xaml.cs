using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Automation
{
    public partial class VariableCheckDialog : Window
    {
        protected virtual ModelVariables ViewModel { get; }
        protected virtual DispatcherTimer VariableUpdateTimer { get; }
        protected virtual Dictionary<string, object> PluginSettings => ViewModel?.SettingProfile?.PluginSettings ?? [];
        protected virtual ConcurrentDictionary<string, ISimResourceSubscription> VariableSubscriptions { get; } = [];
        protected virtual ConcurrentDictionary<string, TextBlock> VariableElements { get; } = [];
        protected virtual bool WasActivated { get; set; } = false;

        public VariableCheckDialog()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            VariableUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(ViewModel.Config.UiRefreshInterval),
            };
            VariableUpdateTimer.Tick += VariableUpdateTimer_Tick;

            this.Loaded += OnDialogLoaded;
            this.Unloaded += OnDialogUnloaded;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (!WasActivated)
            {
                var appWindow = AppService.Instance.App.AppWindow;
                Top = appWindow.Top + (appWindow.ActualHeight / 2.0) - (this.ActualHeight / 2.0);
                Left = appWindow.Left + (appWindow.ActualWidth / 2.0) - (this.ActualWidth / 2.0);
                MaxWidth = appWindow.ActualWidth + 96;
                WasActivated = true;
            }
        }

        protected virtual void OnDialogLoaded(object sender, RoutedEventArgs e)
        {
            VariableUpdateTimer.Start();
            AddGenericVariables();
            InputVariableName.UpdateBindingOnEnter();
            InputVariableUnit.UpdateBindingOnEnter();
        }

        protected virtual void OnDialogUnloaded(object sender, RoutedEventArgs e)
        {
            VariableUpdateTimer.Stop();
            ViewModel.Dispose();
        }

        protected virtual void AddGenericVariables()
        {
            PanelProfileVariables.Children.Clear();
            var genericVariables = PluginSettings.Where(p => p.Key.StartsWith("Generic.Var.", StringComparison.InvariantCultureIgnoreCase));

            string name;
            foreach (var variable in genericVariables)
            {
                if (!variable.Key.EndsWith(".Name", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                name = variable.Value.ToString();

                if (!PluginSettings.TryGetValue(variable.Key.Replace(".Name", ".Unit", StringComparison.InvariantCultureIgnoreCase), out object unit))
                {
                    Logger.Debug($"No Unit found for '{variable.Key}' - assuming number");
                    unit = SimUnitType.Number;
                }

                if (!ViewModel.SimStore.TryGet(name, out ISimResourceSubscription subscription))
                {
                    Logger.Warning($"No Subscription found for Variable '{variable.Key}'");
                    continue;
                }

                PanelProfileVariables.Children.Add(CreateVariablePanel(name, unit.ToString()));
                VariableSubscriptions.Add(name, subscription);
            }
        }

        protected virtual StackPanel CreateVariablePanel(string name, string unit)
        {
            var panel = new StackPanel() { Orientation = Orientation.Horizontal };

            var textblock = new TextBlock() { Text = name, Margin = new Thickness(6, 6, 6, 0), MinHeight = 24, MinWidth = 240, MaxWidth = 240, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(textblock);

            textblock = new TextBlock() { Text = unit, Margin = new Thickness(6, 6, 6, 0), MinHeight = 24, MinWidth = 160, MaxWidth = 160, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(textblock);

            textblock = new TextBlock() { Margin = new Thickness(40, 6, 6, 0), MinHeight = 24, MinWidth = 96, MaxWidth = 96, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(textblock);
            VariableElements.Add(name, textblock);

            return panel;
        }

        protected virtual void VariableUpdateTimer_Tick(object? sender, EventArgs e)
        {
            foreach (var variable in VariableSubscriptions)
                ModelVariables.SetTextBlock(variable.Value, VariableElements[variable.Key]);

            if (ViewModel.IsMonitorActive)
                ViewModel.Refresh();
        }
    }
}
