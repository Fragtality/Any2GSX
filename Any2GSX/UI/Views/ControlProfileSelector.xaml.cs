using Any2GSX.AppConfig;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Any2GSX.UI.Views
{
    public partial class ControlProfileSelector : UserControl
    {
        protected virtual ModelSelector ViewModel { get; }
        protected virtual bool IsSelectionChanging { get; set; } = false;

        public ControlProfileSelector()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            GridProfile.MouseEnter += GridProfileOnMouseEnter;
            GridProfile.MouseLeave += GridProfileOnMouseLeave;

            AppService.Instance.ProfileChanged += (profile) => ViewModel.RunOnDispatcher(() => OnProfileChanged(profile));
            AppService.Instance.ProfileCollectionChanged += () => ViewModel.RunOnDispatcher(() => OnProfileCollectionChanged());
            SelectorProfiles.SelectionChanged += SelectorProfilesOnSelectionChanged;
            OnProfileCollectionChanged();
        }

        protected virtual void OnProfileChanged(SettingProfile profile)
        {
            IsSelectionChanging = true;
            SelectorProfiles.SelectedItem = profile;
            IsSelectionChanging = false;
        }

        protected virtual void OnProfileCollectionChanged()
        {
            IsSelectionChanging = true;
            SelectorProfiles.ItemsSource = null;
            SelectorProfiles.ItemsSource = AppService.Instance.Config.SettingProfiles;
            SelectorProfiles.SelectedItem = AppService.Instance.SettingProfile;
            IsSelectionChanging = false;
        }

        protected virtual void SelectorProfilesOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSelectionChanging)
                return;

            if (SelectorProfiles.SelectedItem is SettingProfile profile)
                ViewModel.SetProfile(profile);
        }

        protected virtual void GridProfileOnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!ViewModel.IsSessionRunning)
                SelectorProfiles.Visibility = Visibility.Visible;
        }

        protected virtual void GridProfileOnMouseLeave(object sender, MouseEventArgs e)
        {
            SelectorProfiles.Visibility = Visibility.Hidden;
        }
    }
}
