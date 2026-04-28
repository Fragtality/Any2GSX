using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceReposition(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        public virtual ISimResourceSubscription SubRepositioning { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => null;
        public virtual bool IsRepositioning => SubRepositioning?.GetNumber() > 0;
        public virtual bool WasRepoReceived { get; protected set; } = false;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(10, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuParkingSelect));
            sequence.Commands.Add(GsxMenuCommand.Wait(6));

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        public override void InitSubscriptions()
        {
            SubRepositioning = SimStore.AddVariable(GsxConstants.VarServiceReposition);
            SubRepositioning?.OnReceived += OnRepositionState;
        }

        protected virtual Task OnRepositionState(ISimResourceSubscription sub, object data)
        {
            if (!WasRepoReceived && sub?.GetNumber() > 0)
            {
                WasRepoReceived = true;
                Logger.Debug("Reposition Signal received");
            }

            return Task.CompletedTask;
        }

        protected override void DoReset()
        {
            WasRepoReceived = false;
        }

        public override void FreeResources()
        {
            SubRepositioning?.OnReceived -= OnRepositionState;
            SimStore.Remove(GsxConstants.VarServiceReposition);
        }

        protected override GsxServiceState GetState()
        {
            if (SequenceResult)
                return GsxServiceState.Completed;
            else
                return GsxServiceState.Callable;
        }

        protected override Task<bool> DoCall()
        {
            WasRepoReceived = false;
            return base.DoCall();
        }

        protected override bool CheckCalled()
        {
            IsCalled = SequenceResult && (IsRepositioning || WasRepoReceived);
            return IsCalled;
        }

        protected override void SetStateVariable(GsxServiceState state)
        {

        }
    }
}
