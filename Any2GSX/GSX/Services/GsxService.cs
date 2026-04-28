using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Services
{
    public abstract class GsxService : IGsxService
    {
        public abstract GsxServiceType Type { get; }
        public virtual GsxController Controller { get; }
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        protected virtual Flightplan Flightplan => AppService.Instance.Flightplan;

        public virtual bool IsCalled { get; protected set; } = false;
        protected virtual bool SequenceResult => CallSequence?.IsSuccess ?? false;
        protected virtual GsxMenuSequence CallSequence { get; }
        protected virtual GsxMenuSequence CancelSequence { get; set; }
        protected abstract ISimResourceSubscription SubStateVar { get; }
        public virtual GsxServiceState State => StateOverride != GsxServiceState.Unknown ? StateOverride : GetState();
        public virtual string TextState => GetStateString();
        public virtual GsxServiceState StateOverride { get; set; } = GsxServiceState.Unknown;
        public virtual bool IsStateOverridden => StateOverride != GsxServiceState.Unknown;
        public virtual bool IsRunning => State == GsxServiceState.Requested || State == GsxServiceState.Active;
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => State == GsxServiceState.Completed || WasCompleted;
        public virtual bool IsCompleting => State == GsxServiceState.Completing;
        protected virtual double NumStateCompleted { get; } = 6;
        public virtual bool IsSkipped { get; set; } = false;
        public virtual bool WasActive { get; protected set; } = false;
        public virtual DateTime ActivationTime { get; protected set; } = DateTime.MaxValue;
        public virtual bool WasCompleted { get; protected set; } = false;
        protected virtual bool CompleteNotified { get; set; } = false;

        public event Func<IGsxService, Task> OnActive;
        public event Func<IGsxService, Task> OnCompleted;
        public event Func<IGsxService, Task> OnStateChanged;

        public GsxService(GsxController controller)
        {
            Controller = controller;
            CallSequence = InitCallSequence();
            CancelSequence = InitCancelSequence();
            InitSubscriptions();
            Controller.GsxServices.Add(Type, this);
        }

        protected abstract GsxMenuSequence InitCallSequence();

        protected abstract GsxMenuSequence InitCancelSequence();

        public abstract void InitSubscriptions();

        protected virtual bool EvaluateOverride(ISimResourceSubscription sub)
        {
            if (sub?.GetNumber() == NumStateCompleted && IsStateOverridden)
            {
                Logger.Debug($"Reset State Override for {Type}");
                StateOverride = GsxServiceState.Unknown;
            }

            return !IsStateOverridden;
        }

        protected virtual bool EvaluateComplete(ISimResourceSubscription sub)
        {
            return (sub?.GetNumber() == NumStateCompleted && WasActive) || WasCompleted;
        }

        protected virtual void RunStateRequested()
        {
            WasActive = true;
        }

        protected virtual void RunStateActive()
        {
            WasActive = true;
            CompleteNotified = false;
            ActivationTime = DateTime.Now;
            NotifyActive();
        }

        protected virtual void RunStateCompleted()
        {

        }

        protected virtual Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!EvaluateOverride(sub))
            {
                Logger.Debug($"State Change ignored for {Type} (Override {IsStateOverridden})");
                return Task.CompletedTask;
            }

            if (sub.GetNumber() == 2)
            {
                WasActive = false;
            }
            else if (sub.GetNumber() == 3)
            {
                WasActive = false;
            }
            else if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
                RunStateRequested();
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                RunStateActive();
            }
            else if (EvaluateComplete(sub))
            {
                Logger.Information($"{Type} Service completed");
                WasCompleted = true;
                RunStateCompleted();
                NotifyCompleted();
            }
            NotifyStateChange();
            return Task.CompletedTask;
        }

        public virtual void ResetState(bool resetVariable = false)
        {
            IsCalled = false;
            IsSkipped = false;
            WasActive = false;
            ActivationTime = DateTime.MaxValue;
            WasCompleted = false;
            CompleteNotified = false;
            StateOverride = GsxServiceState.Unknown;
            CallSequence.Reset();
            if (resetVariable)
                SetStateVariable(GsxServiceState.Callable);
            DoReset();
        }

        protected abstract void DoReset();

        public abstract void FreeResources();

        protected virtual GsxServiceState ReadState()
        {
            try
            {
                if (StateOverride != GsxServiceState.Unknown)
                    return StateOverride;

                return (GsxServiceState)(SubStateVar?.GetNumber() ?? 0);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected virtual GsxServiceState GetState()
        {
            var state = ReadState();

            if (((state == GsxServiceState.NotAvailable || state == GsxServiceState.Bypassed || EvaluateComplete(SubStateVar)) && WasActive) || WasCompleted)
                return GsxServiceState.Completed;
            else
                return state;
        }

        protected virtual string GetStateString()
        {
            try
            {
                return State.ToString();
            }
            catch
            {
                return GsxServiceState.Unknown.ToString();
            }
        }

        protected virtual void SetStateVariable(GsxServiceState state)
        {
            Logger.Debug($"Resetting State L-Var for Service {Type} to '{state}'");
            SubStateVar.WriteValue((int)state);
        }

        protected virtual bool CheckCalled()
        {
            IsCalled = IsRunning;
            return IsCalled;
        }

        public virtual async Task Call()
        {
            if (CheckCalled() || CallSequence.IsExecuting)
                return;

            if (await DoCall() == false)
                return;
            else
                await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.Token);

            CheckCalled();
            Logger.Debug($"{Type} Sequence completed: {(IsCalled ? "Success" : "Failed")}");
        }

        protected virtual async Task<bool> DoCall()
        {
            Controller.Tracker.TrackMessage(AppNotification.ServiceCall, Type.ToString());
            Logger.Debug($"Executing Call Sequence for Service {Type}");
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Controller.Tracker.Clear(AppNotification.ServiceCall);

            return result;
        }

        public virtual Task Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            if (Controller.AutomationController.HasSmartButtonRequest && Profile.SmartButtonAbortService > GsxCancelService.Never)
                option = Profile.SmartButtonAbortService;

            if (option == GsxCancelService.Never)
                return Task.CompletedTask;

            string line = option == GsxCancelService.Complete ? GsxConstants.MenuLineCompleteService : GsxConstants.MenuLineAbortService;
            var sequence = new GsxMenuSequence(CancelSequence.Commands);
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuCancelService, [line]));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.State(GsxMenuState.HIDE));
            sequence.EnableMenuCheck = () => false;
            sequence.EnableMenuAfterResetCheck = () => false;

            Logger.Debug($"Executing Cancel Sequence for Service {Type}");
            return Controller.Menu.RunSequence(sequence);
        }

        protected virtual void NotifyStateChange()
        {
            if (State == GsxServiceState.Unknown)
            {
                Logger.Debug($"Ignoring State Change - State for {Type} is unknown");
                return;
            }

            Logger.Debug($"Notify State Change for {Type}: {TextState} (Var: {(int)ReadState()})");
            _ = TaskTools.RunPool(() => OnStateChanged?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyActive()
        {
            Logger.Debug($"Notify Active for {Type}: {TextState}");
            _ = TaskTools.RunPool(() => OnActive?.Invoke(this), Controller.Token);
            Controller.Tracker.TrackTimeout(AppNotification.ServiceActive, 0, Type.ToString());
        }

        protected virtual void NotifyCompleted()
        {
            if (CompleteNotified)
                return;
            CompleteNotified = true;

            Logger.Debug($"Notify Completed for {Type}: {TextState}");
            _ = TaskTools.RunPool(() => OnCompleted?.Invoke(this), Controller.Token);
            Controller.Tracker.TrackTimeout(AppNotification.ServiceComplete, 0, Type.ToString());
        }

        public virtual void ForceComplete()
        {
            if (!WasCompleted)
            {
                Logger.Debug($"Force Complete for {Type}");
                WasCompleted = true;
            }
        }
    }
}
