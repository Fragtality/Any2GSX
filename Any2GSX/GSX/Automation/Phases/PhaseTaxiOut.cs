using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public class PhaseTaxiOut() : AutomationPhase(AutomationState.TaxiOut)
    {
        public override bool NeedsFlightplan => true;
        public override bool NeedsEquipRemoved => true;

        public override Task InitStore()
        {
            return Task.CompletedTask;
        }

        public override Task<AutomationState> Evaluate()
        {
            if (!IsOnGround)
                return Task.FromResult(AutomationState.Flight);
            else
                return Task.FromResult(Phase);
        }

        public override Task Run()
        {
            return Task.CompletedTask;
        }

        public override async Task SetPhase()
        {
            await base.SetPhase();

            GsxGate.Reset(false);

            if (Profile.RunAutomationService && GsxMenu.MenuCommandsAllowed)
            {
                GsxMenu.ExternalSequence = true;
                Logger.Debug($"Refresh (and disable) Menu after Pushback ...");
                await GsxMenu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection && GsxController.IsDeiceAvail && !GsxController.ServiceDeice.IsCompleted);
                await GsxMenu.WaitInterval(2);
                GsxMenu.ExternalSequence = false;
            }
        }
    }
}
