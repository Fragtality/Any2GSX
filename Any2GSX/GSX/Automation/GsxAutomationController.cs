using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Automation.Phases;
using Any2GSX.GSX.Automation.SmartCalls;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using Any2GSX.PluginInterface.Interfaces.GSX;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class GsxAutomationController : IGsxAutomationController
    {
        public virtual AutomationState State { get; protected set; } = AutomationState.Unknown;
        protected virtual bool StateChanged { get; set; } = false;
        protected virtual ConcurrentDictionary<AutomationState, AutomationPhase> AutomationPhases { get; } = [];
        protected virtual StateStore StateStore => GsxController.StateStore;
        protected virtual ConcurrentDictionary<SmartButtonCall, SmartButton> SmartButtonCalls { get; } = [];
        public virtual SmartButtonCall SmartButtonCurrent { get; protected set; } = SmartButtonCall.None;
        public virtual GroundEquipManager EquipManager { get; }
        public virtual AircraftState AircraftState { get; } = new();
        public virtual TimedMenuOpen TimedMenuOpen { get; } = new();
        public virtual DepartureServiceQueue DepartureQueue { get; } = new();
        public virtual bool ExternalServiceControl => DepartureQueue.ExternalServiceControl;
        public virtual bool ExternalSmartButton { get; protected set; } = false;
        public virtual ulong Tick { get; protected set; } = 0;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool IsStarted { get; protected set; } = false;
        public virtual bool AutomationStateValid => AircraftController.IsAircraftConnected && !GsxController.IsWalkaround && GsxController.IsSessionReady && State != AutomationState.Unknown;

        protected virtual GsxController GsxController => AppService.Instance.GsxController;
        protected virtual GsxMenu Menu => GsxController.Menu;
        protected virtual GsxGate Gate => GsxController.Gate;
        protected virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        protected virtual AircraftBase Aircraft => AircraftController.Aircraft;
        protected virtual Config Config => GsxController.Config;
        protected virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        protected virtual NotificationTracker Tracker => AppService.Instance.NotificationTracker;
        protected virtual GsxServicePushback ServicePushBack => GsxController.ServicePushBack;

        public event Func<AutomationState, Task> OnStateChange;

        public GsxAutomationController()
        {
            EquipManager = new(AircraftState);
        }

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                foreach (var state in Enum.GetValues<AutomationState>())
                {
                    var phase = AutomationPhase.Create(state);
                    phase.InitStore();
                    AutomationPhases.Add(state, phase);
                }

                foreach (var call in Enum.GetValues<SmartButtonCall>())
                {
                    var button = SmartButton.Create(call);
                    SmartButtonCalls.Add(call, button);
                }

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            try
            {
                AutomationPhases.Clear();
                SmartButtonCalls.Clear();
            }
            catch { }

            IsInitialized = false;
        }

        public virtual void Reset()
        {
            State = AutomationState.Unknown;
            StateChanged = false;
            SmartButtonCurrent = SmartButtonCall.None;
            AircraftState.Reset();
            EquipManager.Reset();
            TimedMenuOpen.Clear();
            DepartureQueue.Reset();
            ExternalSmartButton = false;
            Tick = 0;
            IsStarted = false;
        }

        public virtual void Stop()
        {
            IsStarted = false;
            Reset();
        }

        public virtual async Task Run()
        {
            try
            {
                IsStarted = true;
                Logger.Information($"Automation Service started");
                while (IsStarted && GsxController.IsActive && !RequestToken.IsCancellationRequested)
                {
                    if (GsxController.IsGsxRunning)
                    {
                        await AircraftState.Refresh();
                        await EvaluatePhases();

                        if (Profile.RunAutomationService && !StateChanged)
                        {
                            SmartButtonCurrent = EvaluateSmartCalls();

                            await RunMenu();

                            await RunServices();

                            if (AircraftState.HasSmartButtonRequest || ExternalSmartButton)
                                await RunSmartCalls();
                        }
                        StateChanged = false;
                    }

                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                    Tick++;
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            try { Stop(); } catch { }

            Logger.Information($"Automation Service ended");
        }

        protected virtual async Task EvaluatePhases()
        {
            AutomationState evalState = await AutomationPhases[State].Evaluate();

            //Intercept Push, Taxi, Flight
            //Flight
            if (State != AutomationState.Unknown && !GsxController.IsWalkaround && evalState < AutomationState.Flight && (!GsxController.IsOnGround || (Config.DebugArrival && State == AutomationState.SessionStart)))
            {
                evalState = AutomationState.Flight;
                AutomationPhase.LogReason(evalState, "Aircraft in Flight", $"IsOnGround {GsxController.IsOnGround} | DebugArrival {Config.DebugArrival}");
            }
            //TaxiOut
            else if (AutomationStateValid && evalState < AutomationState.TaxiOut && AircraftState.AvionicsPowered && AircraftState.EnginesRunning && !ServicePushBack.IsRunning && AircraftState.Speed > Config.SpeedTresholdTaxiOut)
            {
                evalState = AutomationState.TaxiOut;
                AutomationPhase.LogReason(evalState, "Aircraft moving", $"EnginesRunning {AircraftState.EnginesRunning} | Speed {AircraftState.Speed} | Pushback {ServicePushBack.IsRunning}");

            }
            //Pushback
            else if (AutomationStateValid && evalState < AutomationState.Pushback && AircraftState.AvionicsPowered && ((ServicePushBack.IsRunning && ServicePushBack.PushStatus > 0) || AircraftState.LightBeacon))
            {
                evalState = AutomationState.Pushback;
                if (AircraftState.LightBeacon)
                    AutomationPhase.LogReason(evalState, "Beacon", $"Beacon {AircraftState.LightBeacon} | PushRunning {ServicePushBack.IsRunning} | PushStatus {ServicePushBack.PushStatus}");
                else
                    AutomationPhase.LogReason(evalState, "Pushback Service running", $"Beacon {AircraftState.LightBeacon} | PushRunning {ServicePushBack.IsRunning} | PushStatus {ServicePushBack.PushStatus}");
            }

            if (evalState != State)
                await StateChange(evalState);
        }

        public virtual async Task StateChange(AutomationState newState)
        {
            if (State == newState)
                return;

            await AutomationPhases[newState].SetPhase();
            StateChanged = true;
            Logger.Information($"State Change: {State} => {newState}");
            State = newState;
            NotifyState();
        }

        protected virtual void NotifyState()
        {
            if (OnStateChange != null)
                _ = TaskTools.RunPool(() => OnStateChange?.Invoke(State), RequestToken);
        }

        protected virtual SmartButtonCall EvaluateSmartCalls()
        {
            SmartButtonCall call = SmartButtonCall.None;

            if (AutomationStateValid)
            {
                foreach (var button in SmartButtonCalls)
                {
                    if (button.Value.CheckActive())
                    {
                        call = button.Key;
                        break;
                    }
                }
            }

            return call;
        }

        protected virtual async Task RunMenu()
        {
            if (TimedMenuOpen.IsQueued)
            {
                await TimedMenuOpen.Call();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }
            else
            {
                bool autoRefresh = ((Profile.RunAutomationService && Profile.KeepDirectionMenuOpen) || (Profile.PilotsDeckIntegration && Config.RefreshMenuForDeck)) && Menu.MenuCommandsAllowed;

                //Keep Direction Menu open
                if (autoRefresh && ServicePushBack.IsWaitingForDirection && !Tracker.IsActive(AppNotification.PushDirectionSelected) && Tracker.IsActive(AppNotification.PushDirectionDisplayed) && Menu.MenuState >= GsxMenuState.TIMEOUT && State == AutomationState.Pushback)
                {
                    Logger.Debug($"Menu Refresh: Reopen Direction Menu");
                    await ServicePushBack.Call();
                    Menu.ExternalSequence = true;
                    await Menu.WaitInterval(5);

                    if (Menu.IsReady && Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && Menu.MatchMenuLine(2, GsxConstants.MenuPushbackDirection))
                        await Menu.RunCommand(GsxMenuCommand.Select(3), Profile.EnableMenuForSelection);

                    await Menu.WaitInterval(2);
                    Menu.ExternalSequence = false;
                }
                //Deice Trigger on Brake Change
                else if (State == AutomationState.TaxiOut && SmartButtonCurrent == SmartButtonCall.Deice && Menu.MenuCommandsAllowed && AircraftState.BrakeChanged)
                {
                    TimedMenuOpen.Queue($"Automation: Refresh Menu on Brake-Change for Deice Pad ({Gate.LastGateSelection}) ...", TimeSpan.FromMilliseconds(500), () => Profile.EnableMenuForSelection);
                }
                //Arrival Trigger for Jetway/Stair State unknown 
                else if (State == AutomationState.Arrival && !Menu.IsGateMenu && Menu.MenuCommandsAllowed && (GsxController.ServiceJetway.State == GsxServiceState.Unknown || GsxController.ServiceStairs.State == GsxServiceState.Unknown))
                {
                    TimedMenuOpen.Queue("Automation: Refresh Menu for Arrival Services ...",
                                        TimeSpan.FromMilliseconds(Config.StateMachineInterval),
                                        () => Profile.RunAutomationService && Profile.EnableMenuForSelection && !Profile.OperatorAutoSelect && Menu.OperatorSelectionState[GsxMenuCommandOperatorType.Handling] == false);
                }
            }
        }

        protected virtual async Task RunServices()
        {
            if (Tick % 20 == 0)
            {
                if ((State <= AutomationState.Departure || State >= AutomationState.Arrival) && (!GsxController.GateServicesValid || !GsxController.GsxHandlerValid))
                    Logger.Debug($"Handler Data not valid in State {State}: Gate {GsxController.GateServicesValid} | Airport {GsxController.GsxHandlerValid}");
                else if (State != AutomationState.Flight && !GsxController.GsxHandlerValid)
                    Logger.Debug($"Handler Data not valid in State {State}: Airport {GsxController.GsxHandlerValid}");
            }

            if ((State == AutomationState.TaxiIn && GsxController.GsxHandlerValid) || GsxController.GateServicesValid)
                await AutomationPhases[State].Run();

            if (State == AutomationState.Arrival && StateStore[StateVar.RunDepartureOnArrival] == true && DepartureQueue.NextType != GsxServiceType.Boarding)
                await AutomationPhases[AutomationState.Departure].Run();
        }

        protected virtual async Task RunSmartCalls()
        {
            if (SmartButtonCalls[SmartButtonCurrent].CanExecute() && Menu.MenuCommandsAllowed)
                await SmartButtonCalls[SmartButtonCurrent].Run();

            SmartButtonCurrent = SmartButtonCall.None;
            if (AircraftState.HasSmartButtonRequest)
            {
                Logger.Debug($"Reset Smart Button");
                await Aircraft.ResetSmartButton();
                if (GsxController?.SubDefaultSmartButton?.GetNumber() != 0)
                    await GsxController.SubDefaultSmartButton.WriteValue(0);
            }
            if (ExternalSmartButton)
            {
                Logger.Debug($"Reset External Smart Button");
                ExternalSmartButton = false;
            }
        }

        public virtual Task SkipToTurnaround()
        {
            Logger.Debug("Skip to Turnaround requested");
            DepartureQueue.CountActivations();
            StateStore[StateVar.SkippedToTurn] = true;

            return StateChange(AutomationState.TurnAround);
        }

        public virtual Task SkipToPreparation()
        {
            Logger.Debug("Skip to Preparation requested");
            DepartureQueue.ResetActivations();

            return StateChange(AutomationState.Preparation);
        }

        public virtual void SendLoadsheet()
        {
            (AutomationPhases[AutomationState.Pushback] as PhasePushback).SendLoadsheet();
        }

        public virtual void SetExternalSmartButton()
        {
            ExternalSmartButton = true;
        }
    }
}
