using Any2GSX.AppConfig;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlAircraftOptions : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual Dictionary<string, PluginSetting> SettingsGeneric { get; } = [];
        protected virtual List<string> EssentialSettings { get; } = GenericSettings.GetEssentialIds();
        protected virtual Dictionary<string, PluginSetting> SettingsPlugin { get; set; } = [];
        protected virtual bool LayoutPluginUpdated { get; set; } = false;
        protected virtual bool LayoutGenericUpdated { get; set; } = false;

        public const double HEIGHT = 22;
        public const double MARGIN_TOP = 10;
        public const double WIDTH_DESC_MIN = 164;
        public const double WIDTH_ITEM_MAX = 360;

        public ControlAircraftOptions(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            var settings = GenericSettings.GetGenericSettings();
            foreach (var setting in settings)
                SettingsGeneric.Add(setting.Key, setting);

            AppService.Instance.ProfileChanged += (_) => OnModelUpdated(AppService.Instance.SettingProfile);
            AppService.Instance.PluginCapabilitiesChanged += (_) => OnModelUpdated(AppService.Instance.SettingProfile);
            PanelSettingsPlugin.LayoutUpdated += OnPanelPluginLayoutChanged;
            PanelSettingsGeneric.LayoutUpdated += OnPanelGenericLayoutChanged;

            Tag = "Plugin Options";
            ToolTip = "Configure the Aircraft Plugin used.\nLike the Variables used to check the Aircraft State.";
        }

        protected virtual void OnPanelPluginLayoutChanged(object sender, EventArgs e)
        {
            if (!LayoutPluginUpdated && this.IsVisible)
                LayoutPluginUpdated = ResizeSettingItems(PanelSettingsPlugin);
        }

        protected virtual void OnPanelGenericLayoutChanged(object sender, EventArgs e)
        {
            if (!LayoutGenericUpdated && this.IsVisible)
                LayoutGenericUpdated = ResizeSettingItems(PanelSettingsGeneric);
        }

        protected virtual bool ResizeSettingItems(StackPanel panel)
        {
            double maxWidth = 0;
            try
            {
                foreach (var settingPanel in panel.Children)
                {
                    var item = (settingPanel as StackPanel).Children[0] as TextBlock;
                    if (item?.ActualWidth > 0 && item?.ActualWidth > maxWidth)
                        maxWidth = item.ActualWidth;
                }
                if (maxWidth < WIDTH_DESC_MIN)
                    maxWidth = WIDTH_DESC_MIN;
                else if (maxWidth > WIDTH_ITEM_MAX)
                    maxWidth = WIDTH_ITEM_MAX;

                foreach (var settingPanel in panel.Children)
                    ((settingPanel as StackPanel).Children[0] as TextBlock).Width = maxWidth;


                maxWidth = 0;
                foreach (var settingPanel in panel.Children)
                {
                    var item = (settingPanel as StackPanel).Children[1] as Control;
                    if (item?.ActualWidth > 0 && item?.ActualWidth > maxWidth)
                        maxWidth = item.ActualWidth;
                }
                if (maxWidth > WIDTH_ITEM_MAX)
                    maxWidth = WIDTH_ITEM_MAX;

                foreach (var settingPanel in panel.Children)
                    ((settingPanel as StackPanel).Children[1] as Control).Width = maxWidth;
            }
            catch
            {
                return false;
            }

            return maxWidth > 0;
        }

        protected virtual void OnModelUpdated(SettingProfile profile)
        {
            if (profile == null)
                return;

            SettingsPlugin = [];
            if (AppService.Instance.PluginController.HasPlugin(profile.PluginId, out PluginManifest manifest))
            {
                foreach (var setting in manifest.Settings)
                {
                    if (!SettingsGeneric.ContainsKey(setting.Key))
                        SettingsPlugin.Add(setting.Key, setting);
                }
            }

            LayoutPluginUpdated = false;
            PanelSettingsPlugin.Children.Clear();
            foreach (var setting in SettingsPlugin.Values)
                PanelSettingsPlugin.Children.Add(CreateSetting(profile, setting));
            if (SettingsPlugin.Count > 0)
            {
                GroupPluginOptions.Visibility = Visibility.Visible;
                GroupPluginOptions.Header = $"{profile.PluginId} Plugin Options";
            }
            else
                GroupPluginOptions.Visibility = Visibility.Collapsed;

            LayoutGenericUpdated = false;
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

            if (profile.PluginId == SettingProfile.GenericId)
            {
                LabelPluginGeneric.Visibility = Visibility.Visible;
                LabelPluginAircraft.Visibility = Visibility.Collapsed;
            }
            else
            {
                LabelPluginGeneric.Visibility = Visibility.Collapsed;
                LabelPluginAircraft.Visibility = Visibility.Visible;
            }
        }

        protected virtual StackPanel CreateSetting(SettingProfile profile, PluginSetting setting)
        {
            var panel = new StackPanel() { Orientation = Orientation.Horizontal };

            var textblock = new TextBlock() { Text = setting.Description, Margin = new Thickness(0, MARGIN_TOP, 24, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = setting.Tooltip };
            if (EssentialSettings.Contains(setting.Key))
                textblock.FontWeight = FontWeights.Bold;
            panel.Children.Add(textblock);

            var child = CreateElement(profile, setting);
            if (child != null)
            {
                panel.Children.Add(child);
                panel.Children.Add(new TextBlock() { Text = setting.DescUnit, Margin = new Thickness(4, MARGIN_TOP, 0, 0), MinWidth = 32, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center });
            }

            return panel;
        }

        protected virtual UIElement CreateElement(SettingProfile profile, PluginSetting setting)
        {
            UIElement child = null;
            if (setting.Type == PluginSettingType.Bool)
            {
                var check = new CheckBox() { IsChecked = profile.GetSetting<bool>(setting.Key), Margin = new Thickness(8, MARGIN_TOP, 0, 0), MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, ToolTip = setting.Tooltip };
                check.Click += (_, _) => ChangeSetting(setting.Key, check?.IsChecked == true);
                child = check;
            }
            else if (setting.Type != PluginSettingType.Enum)
            {
                var box = new TextBox() { Margin = new Thickness(8, MARGIN_TOP, 0, 0), MinHeight = HEIGHT, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, ToolTip = setting.Tooltip };
                if (setting.Type == PluginSettingType.String)
                {
                    box.Text = profile.GetSetting<string>(setting.Key);
                    box.KeyUp += (_, e) => ChangeOnEnter(setting.Key, box?.Text, e);
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, box?.Text);
                }
                else if (setting.Type == PluginSettingType.Integer)
                {
                    box.Text = profile.GetSetting<int>(setting.Key).ToString();
                    box.KeyUp += (_, e) => ChangeOnEnter(setting.Key, box?.Text, e);
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, Convert.ToInt32(box?.Text));
                }
                else if (setting.Type == PluginSettingType.Number)
                {
                    box.Text = Conversion.ToString(profile.GetSetting<double>(setting.Key));
                    box.KeyUp += (_, e) => ChangeOnEnter(setting.Key, box?.Text, e);
                    box.LostFocus += (_, _) => ChangeSetting(setting.Key, Conversion.ToDouble(box?.Text));
                }
                child = box;
            }
            else if (setting?.EnumValues?.Count > 0)
            {
                var combo = new ComboBox
                {
                    Margin = new Thickness(8, MARGIN_TOP, 0, 0),
                    MinHeight = HEIGHT,
                    ItemsSource = setting.EnumValues,
                    DisplayMemberPath = "Value",
                    SelectedValuePath = "Key",
                    SelectedValue = profile.GetSetting<int>(setting.Key),
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip = setting.Tooltip
                };
                combo.SelectionChanged += (_, _) => ChangeSetting(setting.Key, (int)combo.SelectedValue);
                child = combo;
            }

            return child;
        }

        protected virtual void ChangeSetting(string key, bool boolean)
        {
            AppService.Instance.SettingProfile.SetSetting(key, boolean);
            AppService.Instance.Config.SaveConfiguration();
        }

        protected virtual void ChangeOnEnter(string key, string text, KeyEventArgs e)
        {
            if (Sys.IsEnter(e))
                ChangeSetting(key, text);
        }

        protected virtual void ChangeSetting(string key, string text)
        {
            if (text != null)
            {
                AppService.Instance.SettingProfile.SetSetting(key, text);
                AppService.Instance.Config.SaveConfiguration();
            }
        }

        protected virtual void ChangeSetting(string key, int number)
        {
            AppService.Instance.SettingProfile.SetSetting(key, number);
            AppService.Instance.Config.SaveConfiguration();
        }

        protected virtual void ChangeSetting(string key, double number)
        {
            AppService.Instance.SettingProfile.SetSetting(key, number);
            AppService.Instance.Config.SaveConfiguration();
        }
    }
}
