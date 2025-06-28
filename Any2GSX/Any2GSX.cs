using Any2GSX.AppConfig;
using Any2GSX.UI;
using Any2GSX.UI.NotifyIcon;
using CFIT.AppFramework;
using CFIT.AppLogger;
using System;

namespace Any2GSX
{
    public class Any2GSX(Type windowType) : SimApp<Any2GSX, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                var app = new Any2GSX(typeof(AppWindow));
                return app.Start(args);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return -1;
            }
        }

        protected override void InitAppWindow()
        {
            base.InitAppWindow();
            AppContext.SetSwitch("Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace", true);
        }
    }
}
