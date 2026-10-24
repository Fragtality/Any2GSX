using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.SmartCalls
{
    public class ButtonNextService() : SmartButton(SmartButtonCall.NextService)
    {
        public override bool CheckActive()
        {
            return AutomationState == AutomationState.Departure && !ExternalServiceControl && (DepartureQueue.HasNext || DepartureQueue.HasNextQueued);
        }

        public override bool CanExecute()
        {
            return CheckActive();
        }

        public override async Task<bool> Run()
        {
            var next = DepartureQueue.HasNext ? DepartureQueue.Next : DepartureQueue.NextQueued;

            if (next.Service.Type == GsxServiceType.Boarding)
                await GsxController.ServiceBoard.SetPaxTarget(Flightplan.CountPax);

            if (Profile.SmartButtonAbortService > GsxCancelService.Never)
                await CanceLastService();

            Logger.Information($"SmartButton: Call next Departure Service {next.Service.Type}");
            if (DepartureQueue.HasNext)
                return await DepartureQueue.CallNext();
            else
                return await DepartureQueue.CallNextQueued();
        }

        protected virtual async Task CanceLastService()
        {
            if (DepartureQueue.HasLastService)
            {
                Logger.Information($"Automation: Cancel last Service {DepartureQueue.LastCalled.Service.Type} on SmartButton Call");
                await DepartureQueue.LastCalled.Service.Cancel();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }
            else if (AutomationState == AutomationState.Arrival && GsxController.ServiceDeboard.IsActive)
            {
                Logger.Information($"Automation: Cancel last Service {GsxController.ServiceDeboard.Type} on SmartButton Call");
                await GsxController.ServiceDeboard.Cancel();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }
        }
    }
}
