using Any2GSX.PluginInterface.Interfaces;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonConnect() : SmartButton(SmartButtonCall.Connect)
    {
        public override bool CheckActive()
        {
            bool prep = AutomationState == AutomationState.Preparation && (ServiceJetway.IsConnectable || ServiceStairs.IsConnectable) && !Profile.CallJetwayStairsOnPrep;
            bool arrival = AutomationState == AutomationState.Arrival && !GsxController.ServiceDeboard.IsRunning && (ServiceJetway.IsConnectable || ServiceStairs.IsConnectable) && !Profile.CallJetwayStairsOnArrival;

            return prep || arrival;
        }

        public override bool CanExecute()
        {
            return GsxMenu.IsGateMenu && !GsxController.IsGateConnected;
        }

        public override async Task<bool> Run()
        {
            await EquipManager.CheckSetAirstairs(true);

            if (ServiceJetway.IsConnectable && !AircraftState.AirstairsState)
                await ServiceJetway.Call();

            if (ServiceStairs.IsConnectable)
                await ServiceStairs.Call();

            return ServiceJetway.IsCalled || ServiceStairs.IsCalled || AircraftState.AirstairsState;
        }
    }
}
