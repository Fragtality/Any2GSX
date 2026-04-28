using Any2GSX.GSX.Menu;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public class GsxServicePushback(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Pushback;
        public virtual ISimResourceSubscription SubDepartService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDepartService;
        public virtual ISimResourceSubscription SubPushStatus { get; protected set; }
        public virtual bool IsPinInserted => SubBypassPin.GetNumber() == 1;
        public virtual int PushStatus => (int)SubPushStatus.GetNumber();
        public virtual bool IsTugConnected => SubPushStatus.GetNumber() >= 3;
        public virtual bool IsWaitingForDirection => SubPushStatus.GetNumber() == 3 || SubPushStatus.GetNumber() == 4;
        public virtual bool TugAttachedOnBoarding { get; protected set; } = false;
        public virtual ISimResourceSubscription SubBypassPin { get; protected set; }

        public event Func<GsxServicePushback, Task> OnBypassPin;
        public event Func<GsxServicePushback, Task> OnPushStatus;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(5, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Operator());
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection || Controller.IsDeiceAvail;
            sequence.ResetMenuCheck = () => false;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        public override void InitSubscriptions()
        {
            SubDepartService = SimStore.AddVariable(GsxConstants.VarServiceDeparture);
            SubDepartService?.OnReceived += OnStateChange;
            SubPushStatus = SimStore.AddVariable(GsxConstants.VarPusbackStatus);
            SubPushStatus?.OnReceived += OnPushChange;

            SubBypassPin = SimStore.AddVariable(GsxConstants.VarBypassPin);
            SubBypassPin?.OnReceived += NotifyBypassPin;
        }

        protected virtual Task OnPushChange(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"PushState changed to {sub?.GetNumber() ?? 0}");
            if (!TugAttachedOnBoarding && sub.GetNumber() > 0 && Controller.GsxServices[GsxServiceType.Boarding].IsRunning)
            {
                Logger.Information($"Tug attaching during Boarding");
                TugAttachedOnBoarding = true;
                Controller.Menu.BlockMenuUpdates(false);
            }
            _ = TaskTools.RunPool(() => OnPushStatus?.Invoke(this), Controller.Token);
            return Task.CompletedTask;
        }

        protected virtual Task NotifyBypassPin(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"BypassPin changed to {sub?.GetNumber() ?? 0}");
            _ = TaskTools.RunPool(() => OnBypassPin?.Invoke(this), Controller.Token);
            return Task.CompletedTask;
        }

        protected override void DoReset()
        {
            TugAttachedOnBoarding = false;
        }

        public override void FreeResources()
        {
            SubDepartService?.OnReceived -= OnStateChange;
            SubBypassPin?.OnReceived -= NotifyBypassPin;
            SubPushStatus?.OnReceived -= OnPushChange;

            SimStore.Remove(GsxConstants.VarServiceDeparture);
            SimStore.Remove(GsxConstants.VarBypassPin);
            SimStore.Remove(GsxConstants.VarPusbackStatus);
        }

        protected override GsxServiceState GetState()
        {
            var state = ReadState();

            if ((state == GsxServiceState.Callable || state == GsxServiceState.NotAvailable || state == GsxServiceState.Bypassed) && WasCompleted)
                return GsxServiceState.Completed;
            else
                return state;
        }

        protected override bool EvaluateComplete(ISimResourceSubscription sub)
        {
            var state = sub?.GetNumber() ?? 0;
            return (((state == NumStateCompleted || state == 1) && WasActive) || WasCompleted) && (PushStatus == 0 || PushStatus == 11);
        }

        protected override bool CheckCalled()
        {
            IsCalled = IsRunning || PushStatus > 0;
            return IsCalled;
        }

        public override Task Call()
        {
            if (PushStatus == 0 || !CheckCalled())
                return base.Call();
            else if (PushStatus > 0 && PushStatus < 5)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(GsxMenuCommand.Open());
                sequence.Commands.Add(GsxMenuCommand.Select(5, GsxConstants.MenuGate));
                sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection;
                sequence.ResetMenuCheck = () => false;

                return Controller.Menu.RunSequence(sequence);
            }

            return Task.CompletedTask;
        }

        public override Task Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            return EndPushback();
        }

        public virtual Task EndPushback()
        {
            Logger.Debug($"End Pushback ({PushStatus})");
            if (PushStatus < 5)
                return Task.CompletedTask;

            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.State(GsxMenuState.TIMEOUT));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Open());
            if (PushStatus == 8)
                sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuPushbackInterrupt, [GsxConstants.MenuLineConfirm]));
            else if (PushStatus < 8)
                sequence.Commands.Add(GsxMenuCommand.Select((int)Profile.StopPushMenuOption, GsxConstants.MenuPushbackInterrupt, GsxConstants.MenuLinesPush[Profile.StopPushMenuOption]));
            else
                sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuPushbackInterrupt));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.State(GsxMenuState.HIDE));
            sequence.EnableMenuCheck = () => false;
            sequence.ResetMenuCheck = () => false;

            return Controller.Menu.RunSequence(sequence);
        }
    }
}
