using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Any2GSX.UI
{
    public partial class ShowInfoDialog : Window
    {
        public virtual string Info { get; }
        public ShowInfoDialog(string info, bool fixedLength = false)
        {
            InitializeComponent();
            Info = info;
            this.DataContext = Info;

            this.MouseLeftButtonDown += (_, _) => this.Close();
            this.MouseLeave += (_, _) => this.Close();
            this.LostFocus += (_, _) => this.Close();

            if (fixedLength)
                TextBlock.FontFamily = new FontFamily("Consolas");
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            CenterOnMousePosition();
        }

        private void CenterOnMousePosition()
        {
            var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
            var mouse = transform.Transform(GetMousePosition());
            Left = mouse.X - (ActualWidth / 2.0);
            Top = mouse.Y - (ActualHeight / 2.0);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new Point(w32Mouse.X, w32Mouse.Y);
        }
    }
}
