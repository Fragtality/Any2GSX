using System.Windows.Controls;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ControlGsxOptions : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }

        public ControlGsxOptions(ModelSettings viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
