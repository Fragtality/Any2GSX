using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class TimedMenuOpen
    {
        protected virtual GsxMenu Menu => AppService.Instance.GsxController.Menu;
        public virtual bool IsQueued => OpenTime != DateTime.MaxValue;
        protected virtual string Message { get; set; } = "";
        protected virtual DateTime OpenTime { get; set; } = DateTime.MaxValue;
        protected virtual Func<bool> ExecuteCheck { get; set; }
        protected virtual Func<bool> EnableCheck { get; set; }
        protected virtual Func<bool> DisableCheck { get; set; }

        public TimedMenuOpen()
        {
            Set("", DateTime.MaxValue, null, null, null);
        }

        public virtual void Queue(string message, TimeSpan delay, Func<bool> enableCheck = null, Func<bool> disableCheck = null, Func<bool> executeCheck = null)
        {
            if (IsQueued)
                return;

            Set(message, DateTime.Now + delay, enableCheck, disableCheck, executeCheck);
        }

        protected virtual void Set(string message, DateTime time, Func<bool> enableCheck, Func<bool> disableCheck, Func<bool> executeCheck)
        {
            Message = message;
            OpenTime = time;
            ExecuteCheck = executeCheck ?? (() => Menu.MenuCommandsAllowed);
            EnableCheck = enableCheck ?? (() => false);
            DisableCheck = disableCheck ?? (() => false);
        }

        public virtual async Task Call()
        {
            if (OpenTime <= DateTime.Now && ExecuteCheck())
            {
                Menu.ExternalSequence = true;
                Logger.Information(Message);
                await Menu.RunCommand(GsxMenuCommand.Open(), EnableCheck());

                if (DisableCheck())
                {
                    await Menu.WaitInterval();
                    await Menu.RunCommand(GsxMenuCommand.Disable(), false);
                }

                Menu.ExternalSequence = false;
                Clear();
            }
        }

        public virtual void Clear()
        {
            Set("", DateTime.MaxValue, null, null, null);
        }
    }
}
