using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServiceReposition() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        protected override int MenuSelectionService => 1;
        protected override string StateVarName => GsxConstants.VarServiceReposition;
        protected override List<string> ServiceVariables => [];
        protected virtual ISimResourceSubscription SubRepositioning { get; set; }
        public virtual bool IsRepositioning => SubStateVar?.GetNumber() > 0;
        public virtual bool WasRepoReceived { get; protected set; } = false;

        public event Func<bool, Task> OnRepositionSignal;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Select(10, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuParkingSelect));
            sequence.Commands.Add(GsxMenuCommand.Wait(6));
            sequence.Commands.Add(GsxMenuCommand.Disable());
            sequence.EnableMenuCheck = () => false;
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => true;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void DoInit()
        {

        }

        protected override Task DoReset(bool resetVariable)
        {
            WasRepoReceived = false;

            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {

        }

        protected override Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            int num = (int)(sub?.GetNumber() ?? 0);
            if (!WasRepoReceived && num > 0)
            {
                WasRepoReceived = true;
                StatesReceived.TryAdd(GsxServiceState.Active, true);
                Logger.Debug("Reposition Signal received");
            }
            else
                Logger.Debug($"Reposition Signal: {num}");

            _ = TaskTools.RunPool(() => OnRepositionSignal?.Invoke(sub?.GetNumber() > 0), Controller.RequestToken);

            return Task.CompletedTask;
        }

        protected override GsxServiceState ReadState(bool raw = false)
        {
            if (EvaluateCompleted(GsxServiceState.Unknown))
                return GsxServiceState.Completed;
            else if (IsRepositioning)
                return GsxServiceState.Active;
            else
                return GsxServiceState.Callable;
        }

        protected override Task<bool> DoCall()
        {
            WasRepoReceived = false;
            return base.DoCall();
        }

        protected override bool EvaluateCalled()
        {
            return WasRepoReceived;
        }

        protected override bool EvaluateRunning(GsxServiceState state)
        {
            return CallSequence.IsExecuting || IsRepositioning;
        }

        protected override bool EvaluateCompleted(GsxServiceState state)
        {
            return SequenceIsSuccess && WasRepoReceived && !IsRepositioning;
        }
    }
}
