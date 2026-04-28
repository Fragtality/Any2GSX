using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ControlSkipQuestions : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlSkipQuestions(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            Tag = "Skip Questions";
            ToolTip = "Configure which GSX Questions/Pop-Ups are skipped and if Walkaround is skipped.";
        }
    }
}
