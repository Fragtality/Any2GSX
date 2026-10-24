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
    public class GsxServiceJetway() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Jetway;
        protected override int MenuSelectionService => 6;
        protected override string StateVarName => GsxConstants.VarServiceJetway;
        protected override List<string> ServiceVariables => [GsxConstants.VarServiceJetwayOperation];
        protected virtual ISimResourceSubscription SubOperating { get; set; }
        public virtual GsxServiceState OperatingState => ReadOperatingState();

        public override bool IsAvailable => Controller.GateServicesValid && State != GsxServiceState.NotAvailable && State != GsxServiceState.Unknown && Controller.StateStore[StateVar.NoJetwayDetected] == false;
        public virtual bool IsConnected => (ReadState() == GsxServiceState.Active && (ReadOperatingState() < GsxServiceState.Bypassed || ReadOperatingState() == GsxServiceState.Completed)) || (IsActive && ReadState() != GsxServiceState.Bypassed && ReadOperatingState() == GsxServiceState.Completed);
        public virtual bool IsOperating => ReadState() == GsxServiceState.Requested || (ReadOperatingState() > GsxServiceState.Bypassed && ReadOperatingState() != GsxServiceState.Completed);
        public virtual bool IsConnectable => IsAvailable && !IsConnected && !EvaluateCalled() && Controller.StateStore[StateVar.NoJetwayDetected] == false;

        public event Func<GsxServiceState, Task> OnOperationChanged;

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void DoInit()
        {
            SubOperating = SimStore[GsxConstants.VarServiceJetwayOperation];
            SubOperating?.OnReceived += OnOperationChange;
        }

        protected override Task DoReset(bool resetVariable)
        {
            if (resetVariable)
                return SubOperating.WriteValue(1);
            else
                return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubOperating?.OnReceived -= OnOperationChange;
        }

        protected override async Task OnCouatlStopped(IGsxController gsxController)
        {
            await base.OnCouatlStopped(gsxController);
            await ResetState(true);
        }

        protected override bool EvaluateCompleted(GsxServiceState state)
        {
            return state == GsxServiceState.Completed;
        }

        protected virtual GsxServiceState ReadOperatingState()
        {
            try
            {
                return (GsxServiceState)(SubOperating?.GetNumber() ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected override bool EvaluateCalled()
        {
            return base.EvaluateCalled() || IsOperating;
        }

        protected virtual Task OnOperationChange(ISimResourceSubscription sub, object data)
        {
            if (InhibitStateUpdates)
                return Task.CompletedTask;

            Logger.Debug($"Operation State Change for {Type}: {OperatingState}");
            _ = TaskTools.RunPool(() => OnOperationChanged?.Invoke(OperatingState), Controller.RequestToken);
            return Task.CompletedTask;
        }

        protected override Task<bool> DoCall()
        {
            if (IsAvailable)
                return base.DoCall();
            else
                return Task.FromResult(IsConnected || IsOperating);
        }

        public virtual Task<bool> Remove()
        {
            if (!IsConnected || !IsAvailable || IsOperating)
            {
                Logger.Debug($"Jetway Remove call blocked");
                return Task.FromResult(false);
            }

            return base.DoCall();
        }

        public override Task<bool> Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            return Remove();
        }
    }
}
