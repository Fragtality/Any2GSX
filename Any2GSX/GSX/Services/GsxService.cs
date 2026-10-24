using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Any2GSX.GSX.Services
{
    public abstract class GsxService() : IGsxService
    {
        protected virtual GsxController Controller => AppService.Instance.GsxController;
        protected virtual SimStore SimStore => AppService.Instance.SimStore;
        protected virtual Config Config => AppService.Instance.Config;
        protected virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        protected virtual Flightplan Flightplan => AppService.Instance.Flightplan;

        public abstract GsxServiceType Type { get; }
        public virtual GsxMenuCommandOperatorType OperatorType => GsxMenuCommandOperatorType.Handling;
        public virtual bool RequiresAircraftDoor => false;
        public virtual bool RequiresStair => false;
        public virtual bool HasStairConflict => false;
        public virtual bool HasSubmenu => Type == GsxServiceType.GPU || Type == GsxServiceType.Water || Type == GsxServiceType.Lavatory || Type == GsxServiceType.Cleaning || Type == GsxServiceType.Deice;

        protected virtual GsxMenuSequence CallSequence { get; set; } = new();
        protected virtual GsxMenuSequence CancelSequence { get; set; } = new();
        protected abstract int MenuSelectionService { get; }
        public virtual bool IsCalled => EvaluateCalled();
        public virtual bool SequenceIsSuccess => CallSequence.IsSuccess;

        protected abstract string StateVarName { get; }
        protected virtual ISimResourceSubscription SubStateVar { get; set; }
        protected abstract List<string> ServiceVariables { get; }
        public virtual GsxServiceState State => ReadState();
        public virtual GsxServiceState RawState => ReadState(true);
        public virtual string TextState => GetStateString();
        public virtual GsxServiceState StateOverride { get; protected set; } = GsxServiceState.Unknown;
        protected virtual ConcurrentDictionary<GsxServiceState, bool> StatesReceived { get; } = [];
        public virtual DateTime ActivationTime { get; protected set; } = DateTime.MaxValue;

        public virtual bool InhibitStateUpdates { get; protected set; } = false;
        public virtual bool IsStateOverridden => StateOverride != GsxServiceState.Unknown;
        public virtual bool IsCallable => State == GsxServiceState.Callable && CallSequence?.IsExecuting == false;
        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable && State != GsxServiceState.Unknown;
        public virtual bool IsBypassed => State == GsxServiceState.NotAvailable || State == GsxServiceState.Bypassed;
        public virtual bool IsRequested => State == GsxServiceState.Requested;
        public virtual bool IsRunning => EvaluateRunning(State);
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => EvaluateCompleted(State);
        public virtual bool IsCompleting => State == GsxServiceState.Completing;
        public virtual bool WasRunning => StatesReceived.Any(state => EvaluateRunning(state.Key));
        public virtual bool WasCanceled { get; protected set; } = false;

        public event Func<IGsxService, Task> OnStateChanged;

        protected virtual GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            if (!HasSubmenu)
                sequence.Commands.Add(GsxMenuCommand.Select(MenuSelectionService, GsxConstants.MenuGate));
            else
            {
                sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
                sequence.Commands.Add(GsxMenuCommand.Select(MenuSelectionService, GsxConstants.MenuAdditionalServices));
            }
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Operator(OperatorType));

            sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && !Profile.OperatorAutoSelect && !Controller.Menu.OperatorSelectionState[OperatorType];
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => IsCalled;

            return sequence;
        }

        protected virtual GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            if (!HasSubmenu)
                sequence.Commands.Add(GsxMenuCommand.Select(MenuSelectionService, GsxConstants.MenuGate));
            else
            {
                sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
                sequence.Commands.Add(GsxMenuCommand.Select(MenuSelectionService, GsxConstants.MenuAdditionalServices));
            }
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.EnableMenuCheck = () => false;
            sequence.EnableMenuAfterResetCheck = () => false;
            sequence.ResetMenuCheck = () => true;

            return sequence;
        }

        public virtual void InitSubscriptions()
        {
            SubStateVar = SimStore.AddVariable(StateVarName);
            SubStateVar?.OnReceived += OnStateChange;

            foreach (var name in ServiceVariables)
                SimStore.AddVariable(name);

            Controller.OnCouatlStarted += OnCouatlStarted;
            Controller.OnCouatlStopped += OnCouatlStopped;

            CallSequence = InitCallSequence();
            CancelSequence = InitCancelSequence();

            DoInit();
        }

        protected abstract void DoInit();

        protected virtual Task OnCouatlStarted(IGsxController gsxController)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnCouatlStopped(IGsxController gsxController)
        {
            return ResetVehicleState(true);
        }

        public virtual void ReleaseSubscriptions()
        {
            DoRelease();

            Controller.OnCouatlStopped -= OnCouatlStopped;
            Controller.OnCouatlStarted -= OnCouatlStarted;

            foreach (var name in ServiceVariables)
                SimStore.Remove(name);

            SubStateVar?.OnReceived -= OnStateChange;
            SubStateVar = SimStore.Remove(StateVarName);
        }

        protected abstract void DoRelease();

        public async Task ResetState(bool resetVariable = false)
        {
            StatesReceived.Clear();
            ActivationTime = DateTime.MaxValue;
            CallSequence.Reset();
            CancelSequence.Reset();
            WasCanceled = false;
            if (resetVariable)
            {
                StateOverride = GsxServiceState.Callable;
                await ResetStateVariable(GsxServiceState.Callable);
                await ResetVehicleState(true);
            }
            else
                await ResetVehicleState(false);
            StateOverride = GsxServiceState.Unknown;
            InhibitStateUpdates = false;
            await DoReset(resetVariable);
        }

        protected abstract Task DoReset(bool resetVariable);

        public virtual Task ResetVehicleState(bool force = false)
        {
            return Task.CompletedTask;
        }

        protected virtual bool EvaluateCalled()
        {
            return EvaluateRunning(State);
        }

        protected virtual bool EvaluateRunning(GsxServiceState state)
        {
            return state == GsxServiceState.Requested || state == GsxServiceState.Active || state == GsxServiceState.Completing;
        }

        protected virtual bool EvaluateCompleted(GsxServiceState state)
        {
            return state == GsxServiceState.Completed || StatesReceived.Any(s => s.Key == GsxServiceState.Completed) || (!IsStateOverridden && WasRunning && state <= GsxServiceState.Bypassed && state != GsxServiceState.Unknown);
        }

        protected virtual GsxServiceState ReadState(bool raw = false)
        {
            try
            {
                var value = (GsxServiceState)(SubStateVar?.GetNumber() ?? 0);
                if (IsStateOverridden && !raw)
                    return StateOverride;
                else if (EvaluateCompleted(value) && !raw)
                    return GsxServiceState.Completed;
                else
                    return value;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected virtual string GetStateString()
        {
            try { return State.ToString(); } catch { return GsxServiceState.Unknown.ToString(); }
        }

        protected virtual Task ResetStateVariable(GsxServiceState state)
        {
            Logger.Debug($"Resetting State L-Var for Service {Type} to '{state}'");
            return SubStateVar.WriteValue((int)state);
        }

        public virtual void SetStateOverride(GsxServiceState overrideState)
        {
            Logger.Debug($"Overriding State for Service {Type} to '{overrideState}' (Raw: {RawState})");
            StateOverride = overrideState;
        }

        public virtual void ClearStateOverride()
        {
            if (IsStateOverridden)
            {
                StateOverride = GsxServiceState.Unknown;
                Logger.Debug($"Cleared State override for Service {Type} (Raw: {RawState})");
            }
        }

        protected virtual bool CheckOverride()
        {
            if (IsStateOverridden && (RawState >= GsxServiceState.Completed || RawState <= GsxServiceState.Callable))
                ClearStateOverride();

            return !IsStateOverridden;
        }

        protected virtual async Task OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!Controller.ApiClient.IsReady)
            {
                Logger.Debug($"Ignoring State Change for {Type}: API not ready");
                return;
            }

            if (!CheckOverride() || InhibitStateUpdates)
            {
                Logger.Debug($"State {RawState} ignored for {Type} (Override {IsStateOverridden} | InhibitUpdates {InhibitStateUpdates})");
                return;
            }

            var state = ReadState();
            if (state == GsxServiceState.Unknown)
            {
                Logger.Debug($"Ignoring State Change for {Type}: Unknown");
                return;
            }

            if (state <= GsxServiceState.Bypassed)
            {
                ActivationTime = DateTime.MaxValue;
                CallSequence.Reset();
                CancelSequence.Reset();
                if (state == GsxServiceState.Callable)
                    await ResetVehicleState();
                WasCanceled = false;
            }
            else if (state == GsxServiceState.Requested || state == GsxServiceState.Active)
            {
                ActivationTime = DateTime.Now;
            }

            await DoStateChange(state);
            NotifyStateChange(state);
        }

        protected virtual Task DoStateChange(GsxServiceState state)
        {
            return Task.CompletedTask;
        }

        protected virtual void NotifyStateChange(GsxServiceState state)
        {
            if (!StatesReceived.ContainsKey(state))
            {
                Logger.Debug($"Notify State Change for {Type}: {TextState} (Raw: {ReadState(true)})");
                _ = TaskTools.RunPool(() => OnStateChanged?.Invoke(this), Controller.RequestToken);
                StatesReceived.TryAdd(state, true);
            }
        }

        public virtual async Task<bool> Call()
        {
            if (EvaluateCalled() || CallSequence.IsExecuting)
                return false;

            if (await DoCall() == false)
                return false;
            else
                await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.RequestToken);

            Logger.Debug($"{Type} Sequence completed: {(IsCalled ? "Success" : "Failed")}");
            return IsCalled;
        }

        protected virtual async Task<bool> DoCall()
        {
            Controller.Tracker.TrackMessage(AppNotification.ServiceCall, Type.ToString());
            Logger.Debug($"Executing Call Sequence for Service {Type}");
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Controller.Tracker.Clear(AppNotification.ServiceCall);

            return result;
        }

        protected virtual string GetCancelSelection(GsxCancelService option)
        {
            return option == GsxCancelService.Complete ? GsxConstants.MenuLineCompleteService : GsxConstants.MenuLineAbortService;
        }

        public virtual Task<bool> Cancel(GsxCancelService option = GsxCancelService.Complete)
        {
            if (option == GsxCancelService.Never)
                return Task.FromResult(false);

            if (IsCompleted || IsCompleting)
            {
                WasCanceled = true;
                return Task.FromResult(true);
            }

            var sequence = new GsxMenuSequence(CancelSequence.Commands);
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuCancelService, [GetCancelSelection(option)]));
            sequence.Commands.Add(GsxMenuCommand.Wait());

            WasCanceled = true;
            return DoCancel(sequence, option);
        }

        protected virtual async Task<bool> DoCancel(GsxMenuSequence sequence, GsxCancelService option)
        {
            Controller.Tracker.TrackMessage(AppNotification.ServiceCancel, Type.ToString());
            Logger.Debug($"Executing Cancel ({option}) Sequence for Service {Type}");
            bool result = await Controller.Menu.RunSequence(sequence);
            Controller.Tracker.Clear(AppNotification.ServiceCancel);

            return result;
        }
    }
}
