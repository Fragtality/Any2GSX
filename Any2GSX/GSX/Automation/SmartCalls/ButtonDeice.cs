using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonDeice() : SmartButton(SmartButtonCall.Deice)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.TaxiOut && GsxGate.HasGateSelection && GsxController.IsDeiceAvail && !GsxController.ServiceDeice.IsCompleted && AircraftState.Speed < Config.SpeedTresholdTaxiOut && AircraftState.BrakeSet;
        }

        public override bool CanExecute()
        {
            return AutomationState == AutomationState.TaxiOut && AircraftState.BrakeSet && !GsxController.ServiceDeice.IsCompleted;
        }

        public override async Task<bool> Run()
        {
            GsxMenu.ExternalSequence = true;
            Logger.Debug($"Refresh Menu for Deice Pad ({GsxGate.LastGateSelection}) ...");
            await GsxMenu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection);
            await GsxMenu.WaitInterval(2);

            bool stop = GsxController.ServiceDeice.IsActive;
            bool result = false;
            if (GsxMenu.IsDeicePad)
            {
                if (!stop)
                {
                    Logger.Information("SmartButton: Start Deice on Pad");
                    result = await GsxMenu.RunCommand(GsxMenuCommand.Select(1, GsxConstants.MenuGate), Profile.EnableMenuForSelection);
                }
                else
                {
                    Logger.Information("SmartButton: Stop Deice on Pad");
                    var sequence = new GsxMenuSequence();
                    sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuGate));
                    sequence.Commands.Add(GsxMenuCommand.Wait(2));
                    sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuCancelService));
                    sequence.Commands.Add(GsxMenuCommand.Wait());
                    sequence.EnableMenuCheck = () => false;
                    sequence.EnableMenuAfterResetCheck = () => false;
                    result = await GsxMenu.RunSequence(sequence);
                }
            }
            GsxMenu.ExternalSequence = false;

            return result;
        }
    }
}
