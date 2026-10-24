using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceDeice() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Deice;
        protected override int MenuSelectionService => 2;
        protected override string StateVarName => GsxConstants.VarServiceDeice;
        protected override List<string> ServiceVariables => [];
        public override bool HasStairConflict => true;

        protected override void DoInit()
        {

        }

        protected override Task DoReset(bool resetVariable)
        {
            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {

        }

        protected override async Task DoStateChange(GsxServiceState state)
        {
            if (state == GsxServiceState.Unknown)
            {
                var automationState = Controller.AutomationState;
                if (automationState == AutomationState.Pushback)
                    Controller.Tracker.Clear(Notifications.AppNotification.UpdatesBlocked);
                else if (!IsCompleted && Controller.IsDeiceAvail && automationState == AutomationState.TaxiOut)
                {
                    Logger.Debug($"Fixing unknown State for {Type}");
                    await SubStateVar.WriteValue(1);
                }
            }
        }
    }
}
