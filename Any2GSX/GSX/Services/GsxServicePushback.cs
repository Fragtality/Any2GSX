using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
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
    public class GsxServicePushback() : GsxService()
    {
        public override GsxServiceType Type => GsxServiceType.Pushback;
        protected override int MenuSelectionService => 5;
        protected override string StateVarName => GsxConstants.VarServiceDeparture;
        protected override List<string> ServiceVariables => [GsxConstants.VarPushbackStatus, GsxConstants.VarBypassPin];
        public override bool HasStairConflict => true;
        protected virtual ISimResourceSubscription SubPushStatus { get; set; }
        protected virtual ISimResourceSubscription SubBypassPin { get; set; }
        public virtual bool IsPinInserted => SubBypassPin?.GetNumber() == 1;
        public virtual int PushStatus => (int)(SubPushStatus?.GetNumber() ?? 0);
        public virtual bool IsTugConnected => SubPushStatus?.GetNumber() >= 3;
        public virtual bool IsWaitingForDirection => SubPushStatus?.GetNumber() == 3 || SubPushStatus?.GetNumber() == 4;
        public virtual bool TugAttachedOnBoarding { get; protected set; } = false;

        public event Func<GsxServicePushback, Task> OnBypassPin;
        public event Func<GsxServicePushback, Task> OnPushStatus;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = base.InitCallSequence();
            sequence.Commands.Add(GsxMenuCommand.Wait(5));
            if (!Controller.IsDeiceAvail || Controller.Menu.DeiceGateQuestionAnswered)
                sequence.Commands.Add(GsxMenuCommand.Close());
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && ((Controller.IsDeiceAvail && !Controller.Menu.DeiceGateQuestionAnswered) || !Controller.HasSelectionPushback);
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => false;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void DoInit()
        {
            SubPushStatus = SimStore[GsxConstants.VarPushbackStatus];
            SubPushStatus.OnReceived += OnPushChange;

            SubBypassPin = SimStore[GsxConstants.VarBypassPin];
            SubBypassPin.OnReceived += NotifyBypassPin;
        }

        protected override Task DoReset()
        {
            TugAttachedOnBoarding = false;

            return Task.CompletedTask;
        }

        protected override void DoRelease()
        {
            SubBypassPin.OnReceived -= NotifyBypassPin;
            SubPushStatus.OnReceived -= OnPushChange;
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
            _ = TaskTools.RunPool(() => OnPushStatus?.Invoke(this), Controller.RequestToken);
            return Task.CompletedTask;
        }

        protected virtual Task NotifyBypassPin(ISimResourceSubscription sub, object data)
        {
            Logger.Debug($"BypassPin changed to {sub?.GetNumber() ?? 0}");
            _ = TaskTools.RunPool(() => OnBypassPin?.Invoke(this), Controller.RequestToken);
            return Task.CompletedTask;
        }

        protected override bool EvaluateCalled()
        {
            return base.EvaluateCalled() || (PushStatus > 0 && State >= GsxServiceState.Requested);
        }

        public virtual Task<bool> OpenPushMenu()
        {
            Logger.Debug($"Open Push Menu ({PushStatus})");
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Wait(2));
            sequence.Commands.Add(GsxMenuCommand.Select(3, GsxConstants.MenuPushbackInterrupt, [GsxConstants.MenuPushbackDirection]));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Close(GsxConstants.MenuPushbackInterrupt));
            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && IsTugConnected && !Controller.HasSelectionPushback && !Controller.Tracker.IsActive(AppNotification.PushDirectionSelected);
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => false;

            return Controller.Menu.RunSequence(sequence);
        }

        public override Task<bool> Call()
        {
            if (!EvaluateCalled())
                return base.Call();
            else if (PushStatus > 0 && PushStatus < 5)
                return OpenPushMenu();

            return Task.FromResult(false);
        }

        public override Task<bool> Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            return EndPushback();
        }

        public virtual Task<bool> EndPushback()
        {
            if (PushStatus < 5)
                return Task.FromResult(false);
            Logger.Debug($"End Pushback ({PushStatus})");

            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Disable());
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Wait());
            if (PushStatus == 8)
                sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuPushbackInterrupt, [GsxConstants.MenuLineConfirm]));
            else if (PushStatus < 8)
                sequence.Commands.Add(GsxMenuCommand.Select((int)Profile.StopPushMenuOption, GsxConstants.MenuPushbackInterrupt, GsxConstants.MenuLinesPush[Profile.StopPushMenuOption]));
            else
                sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuPushbackInterrupt));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Disable());
            sequence.EnableMenuCheck = () => false;
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => false;

            return Controller.Menu.RunSequence(sequence);
        }
    }
}
