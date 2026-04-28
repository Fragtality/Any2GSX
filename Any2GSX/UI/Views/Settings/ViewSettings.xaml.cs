using System.Collections.Generic;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public enum AppSettingControl
    {
        OfpWeights = 0,
        GsxSettings = 1,
        AppServices = 2,
    }

    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual Dictionary<AppSettingControl, UserControl> SettingControls { get; } = [];
        protected static Dictionary<AppSettingControl, string> SettingGroups { get; } = new()
        {
            { AppSettingControl.OfpWeights, "OFP & Weights" },
            { AppSettingControl.GsxSettings, "GSX Parameter" },
            { AppSettingControl.AppServices, "App Behavior" },
        };

        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            SettingControls.Add(AppSettingControl.OfpWeights, new ControlOfpWeights(ViewModel));
            SettingControls.Add(AppSettingControl.GsxSettings, new ControlGsxSettings(ViewModel));
            SettingControls.Add(AppSettingControl.AppServices, new ControlAppServices(ViewModel));

            SelectorSettingGroup.ItemsSource = SettingGroups;
            SelectorSettingGroup.SelectionChanged += OnSelectionChanged;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectorSettingGroup?.SelectedValue is AppSettingControl controlKey && SettingControls.TryGetValue(controlKey, out var control))
                ViewSettingGroup.Content = control;
        }

        public virtual void Start()
        {
            if (SelectorSettingGroup?.SelectedValue is not AppSettingControl)
                SelectorSettingGroup.SelectedIndex = 0;
        }

        public virtual void Stop()
        {

        }
    }
}
