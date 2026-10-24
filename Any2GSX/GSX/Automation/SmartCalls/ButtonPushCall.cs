using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonPushCall() : SmartButton(SmartButtonCall.PushCall)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.Pushback && !ExternalServiceControl && !ServicePushBack.IsCompleted && !ServicePushBack.IsRunning;
        }

        public override bool CanExecute()
        {
            return CheckActive();
        }

        public override async Task<bool> Run()
        {
            Logger.Debug($"SmartButton on Push ({ServicePushBack.PushStatus})");
            if (ServicePushBack.PushStatus < 5)
            {
                await EquipManager.RemoveGroundEquip("SmartButton");
                if (!AutomationController.ExternalServiceControl || AutomationController.ExternalSmartButton)
                {
                    Logger.Information($"Automation: Call Pushback on SmartButton");
                    await ServicePushBack.Call();
                    if (ServicePushBack.IsWaitingForDirection)
                    {
                        GsxMenu.ExternalSequence = true;
                        await GsxMenu.WaitInterval(5);

                        if (GsxMenu.IsReady && GsxMenu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && GsxMenu.MatchMenuLine(2, GsxConstants.MenuPushbackDirection))
                            await GsxMenu.RunCommand(GsxMenuCommand.Select(3), Profile.EnableMenuForSelection);

                        await GsxMenu.WaitInterval(2);
                        GsxMenu.ExternalSequence = false;
                    }
                }
            }

            return true;
        }
    }
}
