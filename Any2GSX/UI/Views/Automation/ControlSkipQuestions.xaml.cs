﻿using System.Windows.Controls;

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
        }
    }
}
