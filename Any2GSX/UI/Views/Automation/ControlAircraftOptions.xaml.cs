using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlAircraftOptions : UserControl
    {
        protected virtual DispatcherTimer UpdateTimer { get; }
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual Dictionary<string, PluginSetting> SettingsGeneric { get; } = [];
        protected virtual Dictionary<string, PluginSetting> SettingsPlugin { get; set; } = [];
        public const double HEIGHT = 24;

        public ControlAircraftOptions(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            UpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UpdateTimer.Tick += UpdateTimer_Tick;

            var settings = GenericSettings.GetGenericSettings();
            foreach (var setting in settings)
                SettingsGeneric.Add(setting.Key, setting);

            ViewModel.ModelUpdated += (_) => UpdateTimer.Start();
            
        }

        protected virtual void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            OnModelUpdated(AppService.Instance.SettingProfile);
            UpdateTimer.Stop();
        }

        protected virtual void OnModelUpdated(SettingProfile profile)
        {
            if (profile == null)
                return;

            SettingsPlugin = [];
            if (AppService.Instance.PluginController.Plugins.TryGetValue(profile.PluginId, out PluginManifest manifest))
            {
                foreach (var setting in manifest.Settings)
                {
                    if (!SettingsGeneric.ContainsKey(setting.Key))
                        SettingsPlugin.Add(setting.Key, setting);
                }
            }

            PanelSettingsPlugin.Children.Clear();
            foreach (var setting in SettingsPlugin.Values)
                PanelSettingsPlugin.Children.Add(CreateSetting(profile, setting));
            if (SettingsPlugin.Count > 0)
                GroupPluginOptions.Visibility = Visibility.Visible;
            else
                GroupPluginOptions.Visibility = Visibility.Collapsed;

            PanelSettingsGeneric.Children.Clear();
            foreach (var setting in SettingsGeneric.Values)
            {
                if (manifest?.HideGenericSettings != null && manifest.HideGenericSettings.Contains(setting.Key))
                    continue;

                if (setting.Key.StartsWith("Generic.Var.SmartButton") && manifest?.Capabilities?.HasSmartButton == true)
                    continue;

                if ((manifest == null || manifest?.Settings?.Where(s => s.Key == setting.Key)?.Any() == false) || setting.Key.StartsWith("Generic.Option."))
                    PanelSettingsGeneric.Children.Add(CreateSetting(profile, setting));
            }
            if (PanelSettingsGeneric.Children.Count > 0)
                GroupGenericOptions.Visibility = Visibility.Visible;
            else
                GroupGenericOptions.Visibility = Visibility.Collapsed;
        }

        protected virtual StackPanel CreateSetting(SettingProfile profile, PluginSetting setting)
        {
            var panel = new StackPanel() { Margin = new Thickness(2), Orientation = Orientation.Horizontal };

            panel.Children.Add(new TextBlock() { Text = setting.Description, Margin = new Thickness(0,8,0,0), MinWidth = 400, MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center });
            
            var child = CreateElement(profile, setting);
            if (child != null)
                panel.Children.Add(child);

            panel.Children.Add(new TextBlock() { Text = $"[ {setting.Key} ]", FontWeight = FontWeights.Light, Margin = new Thickness(8, 12, 0, 0), MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center });

            return panel;
        }

        protected virtual UIElement CreateElement(SettingProfile profile, PluginSetting setting)
        {
            UIElement child = null;
            double width = 196;
            if (setting.Type == PluginSettingType.Bool)
            {
                var check = new CheckBox() { IsChecked = profile.GetSetting<bool>(setting.Key), Margin = new Thickness(8, 8, 0, 0), MinWidth = width, MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
                check.Click += (_, _) => ChangeSetting(setting.Key, check?.IsChecked == true);
                child = check;
            }
            else if (setting.Type != PluginSettingType.Enum)
            {
                var box = new TextBox() { Margin = new Thickness(8, 8, 0, 0), MinWidth = width, MaxWidth = width, MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
                if (setting.Type == PluginSettingType.String)
                {
                    box.Text = profile.GetSetting<string>(setting.Key);
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, box?.Text);
                }
                else if (setting.Type == PluginSettingType.Integer)
                {
                    box.Text = profile.GetSetting<int>(setting.Key).ToString();
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, Convert.ToInt32(box?.Text));
                }
                else if (setting.Type == PluginSettingType.Number)
                {
                    box.Text = Conversion.ToString(profile.GetSetting<double>(setting.Key));
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, Conversion.ToDouble(box?.Text));
                }
                child = box;
            }
            else if (setting?.EnumValues?.Count > 0)
            {
                var combo = new ComboBox
                {
                    Margin = new Thickness(8, 8, 0, 0),
                    MinWidth = width,
                    MinHeight = HEIGHT,
                    ItemsSource = setting.EnumValues,
                    DisplayMemberPath = "Value",
                    SelectedValuePath = "Key",
                    SelectedValue = profile.GetSetting<int>(setting.Key),
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                combo.SelectionChanged += (_, _) => ChangeSetting(setting.Key, (int)combo.SelectedValue);
                child = combo;
            }

            return child;
        }

        protected virtual void ChangeSetting(string key, bool boolean)
        {
            AppService.Instance.SettingProfile.SetSetting(key, boolean);
        }

        protected virtual void ChangeSetting(string key, string text)
        {
            if (text != null)
                AppService.Instance.SettingProfile.SetSetting(key, text);
        }

        protected virtual void ChangeSetting(string key, int number)
        {
            AppService.Instance.SettingProfile.SetSetting(key, number);
        }

        protected virtual void ChangeSetting(string key, double number)
        {
            AppService.Instance.SettingProfile.SetSetting(key, number);
        }
    }
}
