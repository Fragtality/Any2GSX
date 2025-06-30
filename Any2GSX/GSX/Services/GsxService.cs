using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
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
        protected virtual ReceiverStore ReceiverStore => Controller.ReceiverStore;
        protected virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        protected virtual Flightplan Flightplan => AppService.Instance.Flightplan;

        public virtual bool IsCalled { get; protected set; } = false;
        public virtual int FailedCalls { get; protected set; } = 0;
        protected virtual bool SequenceResult => CallSequence?.IsSuccess ?? false;
        protected virtual GsxMenuSequence CallSequence { get; }
        protected abstract ISimResourceSubscription SubStateVar { get; }
        public virtual GsxServiceState State => StateOverride != GsxServiceState.Unknown ? StateOverride : GetState();
        public virtual GsxServiceState StateOverride { get; set; } = GsxServiceState.Unknown;
        public virtual bool IsStateOverridden => StateOverride != GsxServiceState.Unknown;
        public virtual bool IsCalling => CallSequence.IsExecuting;
        public virtual bool IsRunning => State == GsxServiceState.Requested || State == GsxServiceState.Active;
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => State == GsxServiceState.Completed;
        protected virtual double NumStateCompleted { get; } = 6;
        public virtual bool IsSkipped { get; set; } = false;
        public virtual bool WasActive { get; protected set; } = false;
        public virtual bool WasCompleted { get; protected set; } = false;

        public event Func<IGsxService, Task> OnActive;
        public event Func<IGsxService, Task> OnCompleted;
        public event Func<IGsxService, Task> OnStateChanged;

        public GsxService(GsxController controller)
        {
            Controller = controller;
            CallSequence = InitCallSequence();
            InitSubscriptions();
            Controller.GsxServices.Add(Type, this);
        }

        protected abstract GsxMenuSequence InitCallSequence();

        protected abstract void InitSubscriptions();

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
            return sub?.GetNumber() == NumStateCompleted && WasActive;
        }

        protected virtual void RunStateRequested()
        {

        }

        protected virtual void RunStateActive()
        {

        }

        protected virtual void RunStateCompleted()
        {

        }

        protected virtual void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!EvaluateOverride(sub))
            {
                Logger.Debug($"State Change ignored for {Type} (Override {IsStateOverridden})");
                return;
            }

            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
                RunStateRequested();
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                WasActive = true;
                RunStateActive();
                NotifyActive();
            }
            else if (EvaluateComplete(sub))
            {
                Logger.Information($"{Type} Service completed");
                WasCompleted = true;
                RunStateCompleted();
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        public virtual void ResetState()
        {
            IsCalled = false;
            IsSkipped = false;
            WasActive = false;
            WasCompleted = false;
            FailedCalls = 0;
            StateOverride = GsxServiceState.Unknown;
            CallSequence.Reset();
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

            if (NumStateCompleted == 6)
                return ReadState();
            else
            {
                if (state == (GsxServiceState)NumStateCompleted && WasActive)
                    return GsxServiceState.Completed;
                else
                    return state;
            }
        }

        protected virtual bool CheckCalled()
        {
            return IsRunning;
        }

        public virtual async Task Call()
        {
            if (IsCalled)
                return;

            if (FailedCalls > 3 && Controller.Menu.IsGateSelectionMenu)
            {
                Logger.Warning($"Blocked Call for {Type}: GSX Gate Selection active");
                return;
            }

            if (await DoCall() == false)
            {
                FailedCalls++;
                return;
            }
            await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.Token);
            IsCalled = CheckCalled();
        }

        protected virtual async Task<bool> DoCall()
        {
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Logger.Debug($"{Type} Sequence completed: Success {result}");
            return result;
        }

        protected virtual void NotifyStateChange()
        {
            if (State == GsxServiceState.Unknown)
            {
                Logger.Debug($"Ignoring State Change - State for {Type} is unknown");
                return;
            }

            Logger.Debug($"Notify State Change for {Type}: {State}");
            TaskTools.RunLogged(() => OnStateChanged?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyActive()
        {
            Logger.Debug($"Notify Active for {Type}: {State}");
            TaskTools.RunLogged(() => OnActive?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyCompleted()
        {
            Logger.Debug($"Notify Completed for {Type}: {State}");
            TaskTools.RunLogged(() => OnCompleted?.Invoke(this), Controller.Token);
        }

        public virtual void ForceComplete()
        {
            Logger.Debug($"Force Complete for {Type}");
            WasCompleted = true;
        }
    }
}
