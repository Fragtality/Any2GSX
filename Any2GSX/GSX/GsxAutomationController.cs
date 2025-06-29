using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX
{
    public class GsxAutomationController() : IGsxAutomationController
    {
        public virtual GsxController GsxController => AppService.Instance.GsxController;
        public virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController.Aircraft;
        public virtual Flightplan Flightplan => AppService.Instance.Flightplan;
        public virtual bool SmartButtonRequest => AircraftController?.Aircraft?.SmartButtonRequest == true || GsxController?.SubDefaultSmartButton?.GetNumber() != 0;
        public virtual SimConnectManager SimConnect => AppService.Instance.SimConnect;
        public virtual CancellationToken Token => GsxController.Token;
        public virtual SimStore SimStore => GsxController.SimStore;
        public virtual Config Config => GsxController.Config;
        public virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        public virtual bool IsInitialized { get; protected set; } = false;
        protected virtual bool RunFlag { get; set; } = true;
        public virtual bool IsStarted { get; protected set; } = false;
        public virtual AutomationState State { get; protected set; } = AutomationState.SessionStart;
        public virtual bool IsOnGround => GsxController.IsOnGround;

        public virtual IEnumerator DepartureServicesEnumerator { get; protected set; }
        public virtual ServiceConfig DepartureServicesCurrent
        {
            get
            {
                if (DepartureServicesEnumerator?.Current != null)
                    return ((KeyValuePair<int, ServiceConfig>)DepartureServicesEnumerator.Current).Value;
                else
                    return null;
            }
        }
        protected virtual List<GsxService> DepartureServicesCalled { get; } = [];
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController.GsxServices;
        protected virtual GsxServiceReposition ServiceReposition => GsxController.ServiceReposition;
        protected virtual GsxServiceRefuel ServiceRefuel => GsxController.ServiceRefuel;
        protected virtual GsxServiceCatering ServiceCatering => GsxController.ServiceCatering;
        protected virtual GsxServiceJetway ServiceJetway => GsxController.ServiceJetway;
        protected virtual GsxServiceStairs ServiceStairs => GsxController.ServiceStairs;
        protected virtual GsxServicePushback ServicePushBack => GsxController.ServicePushBack;
        protected virtual GsxServiceBoarding ServiceBoard => GsxController.ServiceBoard;
        protected virtual GsxServiceDeboarding ServiceDeboard => GsxController.ServiceDeboard;
        protected virtual GsxServiceDeice ServiceDeice => GsxController.ServiceDeice;
        public virtual bool IsGateConnected => GsxController.IsGateConnected;
        public virtual bool HasDepartBypassed => GsxController.ServiceRefuel.State == GsxServiceState.Bypassed || GsxController.ServiceBoard.State == GsxServiceState.Bypassed;
        public virtual bool HasGateJetway => GsxController.HasGateJetway;
        public virtual bool HasGateStair => GsxController.HasGateStair;
        public virtual bool ServicesValid => GsxController.ServicesValid;

        public virtual DateTime TimeNextTurnCheck { get; set; } = DateTime.MinValue;
        public virtual bool InitialTurnDelay { get; protected set; } = true;
        public virtual bool ExecutedReposition { get; protected set; } = false;
        public virtual bool DepartureServicesCompleted { get; protected set; } = false;
        public virtual bool GroundEquipmentPlaced { get; protected set; } = false;
        public virtual bool JetwayStairRemoved { get; protected set; } = false;
        public virtual bool IsFinalReceived { get; protected set; } = false;
        public virtual int FinalDelay { get; protected set; } = 0;
        public virtual int ChockDelay { get; protected set; } = 0;

        public event Func<AutomationState, Task> OnStateChange;
        public event Func<Task> OnFinalReceived;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {

        }

        public virtual void Reset()
        {
            IsStarted = false;
            RunFlag = true;
            State = AutomationState.SessionStart;

            foreach (var service in GsxServices)
                service.Value.ResetState();

            TimeNextTurnCheck = DateTime.MinValue;
            InitialTurnDelay = true;
            ExecutedReposition = false;
            GroundEquipmentPlaced = false;
            JetwayStairRemoved = false;
            IsFinalReceived = false;
            ChockDelay = 0;
            FinalDelay = 0;

            DepartureServicesCompleted = false;
            DepartureServicesCalled?.Clear();
            if (Profile?.DepartureServices != null)
            {
                DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
                DepartureServicesEnumerator.MoveNext();

                foreach (var activation in Profile.DepartureServices.Values)
                    activation.ActivationCount = 0;
            }
        }

        protected virtual void ResetFlight()
        {
            GsxController.Menu.ResetFlight();
            foreach (var service in GsxServices)
                service.Value.ResetState();

            TimeNextTurnCheck = DateTime.MinValue;
            InitialTurnDelay = true;
            GroundEquipmentPlaced = false;
            JetwayStairRemoved = false;
            IsFinalReceived = false;
            ChockDelay = 0;
            FinalDelay = 0;

            DepartureServicesCompleted = false;
            DepartureServicesCalled.Clear();
            DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
            DepartureServicesEnumerator.MoveNext();
        }

        public virtual async Task Run()
        {
            RunFlag = true;
            DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
            DepartureServicesEnumerator.MoveNext();
            foreach (var activation in Profile.DepartureServices.Values)
                activation.ActivationCount = 0;
            
            while (RunFlag && GsxController.IsActive && !GsxController.Token.IsCancellationRequested)
            {
                if (Aircraft?.IsConnected == true && GsxController.IsGsxRunning && GsxController.CanAutomationRun)
                {
                    if (!IsStarted)
                    {
                        Logger.Information($"Automation Service started");
                        IsStarted = true;
                    }
#if DEBUG
                    Logger.Verbose($"Automation Tick - State: {State} | ServicesValid: {ServicesValid}");
#endif
                    await EvaluateState();

                    if (Profile.RunAutomationService && Aircraft.IsConnected && GsxController.Menu.FirstReadyReceived)
                    {
                        if (ServicesValid)
                            await RunServices();
                        if (SmartButtonRequest)
                            await RunSmartCalls();
                    }

                    if (Profile.RunAutomationService && SmartButtonRequest)
                    {
                        await Aircraft.ResetSmartButton();
                        await AppService.Instance.CommBus.ResetSmartButton();
                    }
                }

                await Task.Delay(Config.StateMachineInterval, Token);
            }
            IsStarted = false;

            Logger.Information($"Automation Service ended");
        }

        public virtual void Stop()
        {
            IsStarted= false;
            RunFlag = false;
        }

        protected virtual async Task EvaluateState()
        {
            //Session Start => Prep / Push / Taxi-Out / Flight
            if (State == AutomationState.SessionStart)
            {
                if (!GsxController.IsOnGround || Config.DebugArrival)
                {
                    if (!Flightplan.IsLoaded)
                        await Flightplan.Import();
                    Logger.Debug($"Starting in {AutomationState.Flight} - IsOnGround {GsxController.IsOnGround} | DebugArrival {Config.DebugArrival}");
                    StateChange(AutomationState.Flight);
                }
                else if (Aircraft.IsEngineRunning || Aircraft.LightBeacon || ServicePushBack.PushStatus > 0)
                {
                    if (!Flightplan.IsLoaded)
                        await Flightplan.Import();
                    GroundEquipmentPlaced = EvaluateGroundEquip();
                    if ((Aircraft.LightBeacon && !Aircraft.IsEngineRunning) || ServicePushBack.PushStatus > 0)
                    {
                        Logger.Debug($"Starting in {AutomationState.Pushback} - Beacon {Aircraft.LightBeacon} | PushStatus {ServicePushBack.PushStatus > 0}");
                        SetPushback();
                    }
                    else
                    {
                        Logger.Debug($"Starting in {AutomationState.TaxiOut} - EnginesRunning {Aircraft.IsEngineRunning}");
                        StateChange(AutomationState.TaxiOut);
                    }
                        
                }
                else if (Aircraft.ReadyForDepartureServices
                    || ServiceRefuel.State > GsxServiceState.Requested
                    || ServiceCatering.State > GsxServiceState.Requested
                    || ServiceBoard.State > GsxServiceState.Requested)
                {
                    await Flightplan.Import();
                    GroundEquipmentPlaced = EvaluateGroundEquip();
                    if (Aircraft.WeightTotalKg >= Flightplan.WeightTotalRampKg - Config.FuelCompareVariance)
                    {
                        Logger.Debug($"Starting in {AutomationState.Pushback} - WeightTotalKg {Aircraft.WeightTotalKg} | WeightTotalRampKg {Flightplan.WeightTotalRampKg}");
                        StateChange(AutomationState.Pushback);
                    }
                    else
                    {
                        Logger.Debug($"Starting in {AutomationState.Departure} - ReadyForDepartureServices {Aircraft.ReadyForDepartureServices} | FlightplanLoaded {Flightplan.IsLoaded}");
                        StateChange(AutomationState.Departure);
                    }
                }
                else if (Aircraft?.IsConnected == true && GsxController?.SkippedWalkAround == true && GsxController?.Menu?.IsGateSelectionMenu == false)
                    StateChange(AutomationState.Preparation);
            }
            //intercept Flight
            else if (State < AutomationState.Flight && !IsOnGround)
            {
                StateChange(AutomationState.Flight);
                ResetFlight();
            }
            //intercept TaxiOut
            else if (State < AutomationState.TaxiOut && Aircraft.IsEngineRunning && ServicePushBack.State != GsxServiceState.Active && Aircraft.GroundSpeed > 1)
            {
                Logger.Debug($"Intercepting Taxi Out!");
                StateChange(AutomationState.TaxiOut);
            }
            //Preparation => Departure
            else if (State == AutomationState.Preparation)
            {
                GroundEquipmentPlaced = EvaluateGroundEquip();

                if ((!Profile.RunAutomationService || ExecutedReposition) && GroundEquipmentPlaced && Aircraft.ReadyForDepartureServices)
                {
                    Logger.Information($"Aircraft is ready for Departure Services");
                    await Flightplan.Import();
                    await RefreshGsxSimbrief();
                    await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                    StateChange(AutomationState.Departure);
                }
                else if (ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning)
                {
                    Logger.Information($"Departure Services already running - skipping Preparation");
                    await Flightplan.Import();

                    if (!ServiceBoard.IsRunning)
                        await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                    StateChange(AutomationState.Departure);
                }
            }
            //Departure => PushBack
            else if (State == AutomationState.Departure)
            {
                if (DepartureServicesCompleted)
                {
                    SetPushback();
                }
                else if (ServicePushBack.PushStatus > 0 && (ServicePushBack.IsActive || HasDepartBypassed))
                {
                    Logger.Information($"Pushback Service already running - skipping Departure");
                    SetPushback();
                }
            }
            //PushBack => TaxiOut
            else if (State == AutomationState.Pushback)
            {
                if (ServicePushBack.IsCompleted || (!GsxController.IsWalkaround && ServicePushBack.PushStatus == 0 && Aircraft.GroundSpeed >= Config.SpeedTresholdTaxiOut))
                    StateChange(AutomationState.TaxiOut);
            }
            //Flight => TaxiIn
            else if (State == AutomationState.Flight)
            {
                Logger.Verbose($"SimGround: {GsxController.IsOnGround} ControllerGround {IsOnGround} Speed: {Aircraft.GroundSpeed} SpeedTest: {Aircraft.GroundSpeed < Config.SpeedTresholdTaxiIn}");
                if (IsOnGround && Aircraft.GroundSpeed < Config.SpeedTresholdTaxiIn)
                {
                    if (Config.RestartGsxOnTaxiIn)
                    {
                        Logger.Information($"Restarting GSX on Taxi-In");
                        await AppService.Instance.RestartGsx();
                    }
                    StateChange(AutomationState.TaxiIn);
                }
            }
            //TaxiIn => Arrival
            else if (State == AutomationState.TaxiIn)
            {
                Logger.Verbose($"EnginesRunning: {Aircraft.IsEngineRunning} IsBrakeSet {Aircraft.IsBrakeSet} LightBeacon: {Aircraft.LightBeacon}");
                if (!Aircraft.IsEngineRunning && Aircraft.IsBrakeSet && !Aircraft.LightBeacon)
                {
                    await GsxController.Menu.OpenHide();
                    await ServiceDeboard.SetPaxTarget(Flightplan.CountPax);
                    StateChange(AutomationState.Arrival);                    
                }
                else if (ServiceDeboard.IsRunning)
                {
                    Logger.Information($"Deboard Service already running - skipping TaxiIn");
                    StateChange(AutomationState.Arrival);
                }
            }
            //Arrival => Turnaround
            else if (State == AutomationState.Arrival)
            {
                if (ServiceDeboard.IsCompleted)
                    SetTurnaround();
            }
            //Turnaround => Departure
            else if (State == AutomationState.TurnAround)
            {
                if (Aircraft.ReadyForDepartureServices && IsGateConnected && TimeNextTurnCheck <= DateTime.Now && await Flightplan.CheckNewOfp())
                {
                    await Flightplan.Import();
                    await RefreshGsxSimbrief();
                    await ServiceBoard.SetPaxTarget(Flightplan.CountPax);
                    StateChange(AutomationState.Departure);
                }
                else if (SmartButtonRequest)
                {
                    Logger.Information("Skip Turnaround Phase (SmarButton Request)");
                    await SkipTurn(AutomationState.Departure);
                }
                else if (ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning)
                {
                    Logger.Warning($"Departure Services already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.Departure);
                }
                else if (ServicePushBack.IsRunning)
                {
                    Logger.Warning($"Pushback Service already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.Pushback);
                }

                if (TimeNextTurnCheck <= DateTime.Now)
                {
                    TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnRecheckSeconds);
                    InitialTurnDelay = false;
                }
            }
        }

        protected virtual async Task RefreshGsxSimbrief()
        {
            if (Profile.RunAutomationService)
            {
                await GsxController.ReloadSimbrief();
                await Task.Delay(Config.TimerGsxStartupMenuCheck / 2, Token);
            }
        }

        protected virtual void SetPushback()
        {
            StateChange(AutomationState.Pushback);
            _ = FinalLoadsheet();
        }

        protected virtual void SetTurnaround()
        {
            TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnAroundSeconds);
            InitialTurnDelay = true;
            Flightplan.Unload();
            StateChange(AutomationState.TurnAround);
            GsxController.Menu.SuppressMenuRefresh = false;
        }

        protected virtual async Task SkipTurn(AutomationState state)
        {
            await Flightplan.Import();
            if (state == AutomationState.Departure && !ServiceBoard.IsRunning)
            {
                await RefreshGsxSimbrief();
                await ServiceBoard.SetPaxTarget(Flightplan.CountPax);
            }
            StateChange(state);
        }

        protected virtual void StateChange(AutomationState newState)
        {
            if (State == newState)
                return;

            Logger.Information($"State Change: {State} => {newState}");
            State = newState;
            TaskTools.RunLogged(() => OnStateChange?.Invoke(State), Token);
        }

        public virtual void OnCouatlStarted()
        {
            if (!IsStarted)
                return;

            if (State == AutomationState.Departure && !DepartureServicesCompleted && !DepartureServicesEnumerator.CheckEnumeratorValid())
            {
                if (ServiceBoard.WasActive && !ServiceBoard.WasCompleted)
                    ServiceBoard.ForceComplete();
                Logger.Information($"GSX Restart on last Departure Service detected - skip to Pushback");
                SetPushback();
            }

            if (State == AutomationState.Arrival && ServiceDeboard.WasActive && !ServiceDeboard.WasCompleted)
            {
                ServiceDeboard.ForceComplete();
                Logger.Information($"GSX Restart on Deboarding Service detected - skip to Turnaround");
                SetTurnaround();
            }
        }

        public virtual void OnCouatlStopped()
        {
            if (!IsStarted)
                return;
        }

        protected long tickCounter = 0;

        protected virtual async Task RunServices()
        {
            if (State == AutomationState.Preparation)
            {
                await RunPreparation();
            }
            else if (State == AutomationState.Departure)
            {
                await RunDeparture();
            }
            else if (State == AutomationState.Pushback)
            {
                await RunPushback();
            }
            else if (State == AutomationState.Arrival)
            {
                await RunArrival();
            }
        }

        protected virtual async Task RunSmartCalls()
        {
            var manager = AppService.Instance.NotificationManager;

            if (State == AutomationState.TaxiOut && manager.ReportedCall == SmartButtonCall.Deice && !GsxController.ServiceDeice.IsRunning)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(new(1, GsxConstants.MenuGate, true));
                sequence.Commands.Add(GsxMenuCommand.CreateDummy());
                sequence.Commands.Add(GsxMenuCommand.CreateReset());

                await GsxController.Menu.RunSequence(sequence);
            }
            else if (State == AutomationState.TaxiIn && manager.ReportedCall == SmartButtonCall.ClearGate)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(new(Profile.ClearGateMenuOption, GsxConstants.MenuParkingChange, true));
                sequence.Commands.Add(GsxMenuCommand.CreateDummy());
                sequence.Commands.Add(GsxMenuCommand.CreateReset());

                await GsxController.Menu.RunSequence(sequence);
            }
        }

        protected virtual bool EvaluateGroundEquip()
        {
            return Aircraft.EquipmentPower && ((Aircraft.HasChocks && Aircraft.EquipmentChocks) || (!Aircraft.HasChocks && Aircraft.IsBrakeSet));
        }

        protected virtual async Task RunPreparation()
        {
            if (!ExecutedReposition && !IsGateConnected && !Aircraft.ReadyForDepartureServices)
            {
                if (Profile.CallReposition)
                {
                    Logger.Information("Automation: Reposition Aircraft on Gate");
                    await ServiceReposition.Call();
                    await Task.Delay(1000, Token);
                }
            }
            if (!ExecutedReposition)
            {
                ExecutedReposition = ServiceReposition.IsCompleted || IsGateConnected || !Profile.CallReposition || Aircraft.ReadyForDepartureServices;
                if (ExecutedReposition)
                    Logger.Debug($"Reposition executed");
            }

            if (ExecutedReposition && GsxController.SkippedWalkAround)
            {
                if (Aircraft.HasChocks && !Aircraft.EquipmentChocks)
                {
                    Logger.Information($"Automation: Placing Chocks on Preparation");
                    await Aircraft.SetEquipmentChocks(true);
                    await Task.Delay(500, GsxController.Token);
                }

                if (Aircraft.HasCones && !Aircraft.EquipmentCones)
                {
                    Logger.Information($"Automation: Placing Cones on Preparation");
                    await Aircraft.SetEquipmentCones(true);
                    await Task.Delay(500, GsxController.Token);
                }

                if (Aircraft.HasGpuInternal && !Aircraft.EquipmentPower)
                {
                    Logger.Information($"Automation: Placing GPU on Preparation");
                    await Aircraft.SetEquipmentPower(true);
                    await Task.Delay(500, GsxController.Token);
                }

                if (Aircraft.HasPca && !Aircraft.EquipmentPca && Profile.ConnectPca > 0)
                {
                    if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && HasGateJetway))
                    {
                        Logger.Information($"Automation: Placing PCA on Preparation");
                        await Aircraft.SetEquipmentPca(true);
                        await Task.Delay(500, GsxController.Token);
                    }
                }

                if (Aircraft.UseGpuGsx && !GsxServices[GsxServiceType.GPU].IsCalled && !GsxServices[GsxServiceType.GPU].IsRunning)
                {
                    Logger.Information($"Automation: Calling GSX GPU on Preparation");
                    await GsxServices[GsxServiceType.GPU].Call();
                    await Task.Delay(500, GsxController.Token);
                }

                GroundEquipmentPlaced = EvaluateGroundEquip();
            }

            if (ExecutedReposition && GsxController.SkippedWalkAround && ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest)
                && !Aircraft.HasAirStairForward)
            {
                Logger.Information("Automation: Call Jetway on Preparation");
                await ServiceJetway.Call();
            }

            if (ExecutedReposition && GsxController.SkippedWalkAround && ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest)
                && (!Aircraft.IsFuelOnStairSide || SmartButtonRequest)
                && ((!ServiceJetway.IsAvailable && !Aircraft.HasAirStairForward && !Aircraft.HasAirStairAft) || (ServiceJetway.IsAvailable && !Aircraft.HasAirStairAft)))
            {
                Logger.Information("Automation: Call Stairs on Preparation");
                await ServiceStairs.Call();
            }
        }

        protected virtual async Task RunDeparture()
        {
            if (Aircraft.HasPca && Aircraft.IsApuRunning && Aircraft.IsApuBleedOn && Aircraft.EquipmentPca)
            {
                Logger.Information($"Automation: Disconnecting PCA");
                await Aircraft.SetEquipmentPca(false);
            }

            if (!DepartureServicesCompleted)
            {
                if (!DepartureServicesEnumerator.CheckEnumeratorValid())
                {
                    DepartureServicesCompleted = DepartureServicesCalled.All(s => s.IsCompleted || s.IsSkipped);
                    if (DepartureServicesCompleted)
                    {
                        Logger.Information($"Automation: All Departure Services completed");
                        if (ServiceStairs.IsConnected && (ServiceJetway.IsConnected && Profile.RemoveStairsAfterDepature == 2) || (!ServiceJetway.IsConnected && Profile.RemoveStairsAfterDepature == 1))
                        {
                            Logger.Information($"Automation: Remove Stairs after Departure Services");
                            await ServiceStairs.Remove();
                        }
                    }
                }
                else if (Aircraft.IsBoardingCompleted && !ServiceBoard.IsCalled)
                {
                    Logger.Information($"Plane already boarded - skipping all Departure Services");
                    DepartureServicesCompleted = true;
                }
            }

            if (!DepartureServicesCompleted && Aircraft.IsFuelOnStairSide
                && ServiceStairs.IsAvailable && !ServiceStairs.IsCalled
                && !(ServiceBoard.IsCompleted || ServiceBoard.IsSkipped)
                && Profile.DepartureServices.Values.Any(s => s.ServiceType == GsxServiceType.Refuel && s.ServiceActivation > GsxServiceActivation.Skip)
                && ServiceRefuel.IsCompleted)
            {
                if (DepartureServicesEnumerator.CheckEnumeratorValid() && DepartureServicesCurrent.ServiceType != GsxServiceType.Boarding
                    && ((!ServiceJetway.IsAvailable && !Aircraft.HasAirStairForward && !Aircraft.HasAirStairAft) || (ServiceJetway.IsAvailable && !Aircraft.HasAirStairAft)))
                {
                    Logger.Information("Automation: Call Stairs after Refuel");
                    await ServiceStairs.Call();
                }
            }
            else if (!DepartureServicesCompleted && !Aircraft.IsFuelOnStairSide && !IsGateConnected && Profile.CallJetwayStairsDuringDeparture
                     && !(ServiceBoard.IsCompleted || ServiceBoard.IsSkipped))
            {
                if (ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled && !Aircraft.HasAirStairForward)
                {
                    Logger.Information("Automation: Call Jetway during Departure");
                    await ServiceJetway.Call();
                }

                if (ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled
                    && ((!ServiceJetway.IsAvailable && !Aircraft.HasAirStairForward && !Aircraft.HasAirStairAft) || (ServiceJetway.IsAvailable && !Aircraft.HasAirStairAft)))
                {
                    Logger.Information("Automation: Call Stairs during Departure");
                    await ServiceStairs.Call();
                }
            }

            if (DepartureServicesEnumerator.CheckEnumeratorValid() && !DepartureServicesCompleted)
            {
                GsxService current = GsxServices[DepartureServicesCurrent.ServiceType];
                GsxServiceActivation activation = DepartureServicesCurrent.ServiceActivation;
                if (!current.IsCalled && !current.IsCompleted && !current.IsRunning && (!DepartureServicesCurrent.HasDurationConstraint || Flightplan.Duration >= DepartureServicesCurrent.MinimumFlightDuration))
                {
                    if (Profile.SkipFuelOnTankering && activation != GsxServiceActivation.Skip && current is GsxServiceRefuel && Flightplan.IsLoaded && Aircraft.FuelOnBoardKg >= Flightplan.FuelRampKg - Config.FuelCompareVariance)
                    {
                        Logger.Information($"Automation: Skip Refuel because FOB is greater than planned");
                        MoveDepartureQueue(current, true);
                    }
                    else if (activation == GsxServiceActivation.Skip)
                    {
                        Logger.Debug($"Skipping Service {DepartureServicesCurrent.ServiceType}");
                        DepartureServicesEnumerator.MoveNext();
                    }
                    else if (DepartureServicesCurrent.ActivationCount > 0 && DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.FirstLeg)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (DepartureServicesCurrent.ActivationCount == 0 && DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.TurnAround)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.CompanyHub && !Profile.IsCompanyHub(Flightplan.Origin))
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (SmartButtonRequest
                            || activation == GsxServiceActivation.AfterCalled
                            || activation == GsxServiceActivation.AfterRequested && (DepartureServicesCalled.Last()?.State >= GsxServiceState.Requested || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0)
                            || activation == GsxServiceActivation.AfterActive && (DepartureServicesCalled.Last()?.State >= GsxServiceState.Active || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0)
                            || (activation == GsxServiceActivation.AfterPrevCompleted && (DepartureServicesCalled.Last()?.IsCompleted == true || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0))
                            || (activation == GsxServiceActivation.AfterAllCompleted && DepartureServicesCalled.All(s => s.IsCompleted || s.IsSkipped)))
                    {
                        if (DepartureServicesCurrent.ServiceType == GsxServiceType.Boarding)
                            await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                        if (!DepartureServicesCurrent.CallOnCargo && Aircraft.IsCargo)
                        {
                            Logger.Information($"Skip Service {DepartureServicesCurrent.ServiceType} because not enabled for Cargo Airplane");
                            MoveDepartureQueue(current, true);
                        }
                        else if (current is GsxServiceRefuel && Aircraft.IsFuelOnStairSide && Profile.AttemptConnectStairRefuel
                                 && ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled)
                        {
                            Logger.Information($"Automation: Call Departure Service {DepartureServicesCurrent.ServiceType} (with Stairs attached)");
                            await ServiceStairs.Call();
                            await Task.Delay(Profile.DelayCallRefuelAfterStair * 1000, Token);
                            await current.Call();
                            if (current.IsCalled)
                                MoveDepartureQueue(current);
                        }
                        else
                        {
                            Logger.Information($"Automation: Call Departure Service {DepartureServicesCurrent.ServiceType}");
                            await current.Call();
                            if (current.IsCalled)
                                MoveDepartureQueue(current);
                        }
                    }
                }
                else
                {
                    bool skipped = false;
                    if (current.IsCompleted)
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} already completed");
                    else if (current.IsCalled || current.IsRunning)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} called externally");
                        if (Aircraft.HasFuelSynch && current.Type == GsxServiceType.Refuel
                            && !AircraftController.RefuelTimer.IsEnabled  && current.IsActive && GsxController.ServiceRefuel.IsHoseConnected)
                        {
                            Logger.Debug($"Starting Refuel Sync for already running GSX Service");
                            await AircraftController.OnRefuelHoseChanged(true);
                        }
                    }
                    else
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Time Constraint");
                        skipped = true;
                    }

                    MoveDepartureQueue(current, skipped);
                }
            }
        }

        protected virtual void MoveDepartureQueue(GsxService service, bool asSkipped = false)
        {
            DepartureServicesCalled.Add(service);
            DepartureServicesCurrent.ActivationCount++;
            if (asSkipped)
                service.IsSkipped = true;
            DepartureServicesEnumerator.MoveNext();
        }

        protected virtual async Task FinalLoadsheet()
        {
            try
            {
                if (IsFinalReceived)
                    return;

                FinalDelay = new Random().Next(Profile.FinalDelayMin, Profile.FinalDelayMax);
                Logger.Debug($"Final LS in {FinalDelay}s");
                Logger.Information($"Waiting for Final Loadsheet (ETA {FinalDelay}s) ...");
                while (FinalDelay > 0)
                {
                    await Task.Delay(1000, Token);
                    FinalDelay--;
                }
                Logger.Information($"Final Loadsheet received!");

                if (Profile.RunAutomationService && Aircraft.HasOpenDoors && Profile.CloseDoorsOnFinal)
                {
                    Logger.Information($"Automation: Close Doors on Final Loadsheet");
                    await AircraftController.Aircraft.DoorsAllClose();
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }

                if (Profile.RunAutomationService && !JetwayStairRemoved && IsGateConnected && Profile.RemoveJetwayStairsOnFinal)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Final Loadsheet");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }

                await TaskTools.RunLogged(() => OnFinalReceived?.Invoke(), GsxController.Token);

                IsFinalReceived = true;
                Logger.Debug($"Final LS handled");
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);

                FinalDelay = 0;
            }
        }

        protected virtual async Task RunPushback()
        {
            if (Aircraft.HasPca && Aircraft.IsApuRunning && Aircraft.IsApuBleedOn && Aircraft.EquipmentPca)
            {
                Logger.Information($"Automation: Disconnecting PCA");
                await Aircraft.SetEquipmentPca(ServicePushBack.PushStatus >= 3 && ServicePushBack.IsActive);
            }

            if (GroundEquipmentPlaced && !Aircraft.IsExternalPowerConnected && Aircraft.IsBrakeSet && Aircraft.LightBeacon)
            {
                if (Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Ground Equipment on Beacon");
                    SetGroundEquip(false);
                }
                GroundEquipmentPlaced = false;
                if (Profile.CallPushbackOnBeacon && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
                {
                    Logger.Information($"Automation: Call Pushback (Beacon / Prepared for Push)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }
                else if (IsGateConnected && Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Beacon");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }
            }

            if (ServicePushBack.TugAttachedOnBoarding && Profile.CallPushbackWhenTugAttached > 0 && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
            {
                if (Profile.CallPushbackWhenTugAttached == 1)
                {
                    Logger.Information($"Automation: Call Pushback after Departure Services (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, Token);
                }
                else if (Profile.CallPushbackWhenTugAttached == 2 && IsFinalReceived)
                {
                    Logger.Information($"Automation: Call Pushback after Final LS (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, Token);
                }
            }

            if (Aircraft.HasOpenDoors && ((ServicePushBack.PushStatus > 0 && ServicePushBack.IsRunning) || ServiceDeice.IsActive || ServicePushBack.IsRunning))
            {
                Logger.Information($"Automation: Close Doors on Pushback");
                await Aircraft.DoorsAllClose();
                await Task.Delay(Config.StateMachineInterval, Token);
            }

            if (GroundEquipmentPlaced && ((ServicePushBack.PushStatus > 1 && ServicePushBack.IsRunning) || ServiceDeice.IsActive || ServicePushBack.IsRunning))
            {
                Logger.Information($"Automation: Remove Ground Equipment on Pushback");
                SetGroundEquip(false);
                GroundEquipmentPlaced = false;
                await Task.Delay(Config.StateMachineInterval, Token);
            }

            bool clearBeacon = Profile.ClearGroundEquipOnBeacon && !Aircraft.IsExternalPowerConnected && Aircraft.IsBrakeSet && Aircraft.LightBeacon;
            if (((ServicePushBack.PushStatus > 1 && ServicePushBack.IsRunning) || ServiceDeice.IsActive || Aircraft.IsEngineRunning || ServicePushBack.IsRunning || clearBeacon) && !GroundEquipmentPlaced)
            {
                string reason = "for Pushback";
                if (ServiceDeice.IsActive)
                    reason = "for De-Ice";
                else if (clearBeacon)
                    reason = "on Beacon";
                else if (Aircraft.IsEngineRunning)
                    reason = "because Engines Running";

                if (Aircraft.HasGpuInternal && Aircraft.EquipmentPower && !Aircraft.IsExternalPowerConnected)
                {
                    Logger.Information($"Automation: Remove GPU {reason}");
                    await Aircraft.SetEquipmentPower(false);
                }

                if (Aircraft.HasChocks && Aircraft.EquipmentChocks && Aircraft.IsBrakeSet)
                {
                    Logger.Information($"Automation: Remove Chocks {reason}");
                    await Aircraft.SetEquipmentChocks(false);
                }

                if (Aircraft.HasCones && Aircraft.EquipmentCones)
                {
                    Logger.Information($"Automation: Remove Cones {reason}");
                    await Aircraft.SetEquipmentCones(false);
                }

                if (Aircraft.HasOpenDoors)
                {
                    Logger.Information($"Automation: Close all Doors {reason}");
                    await Aircraft.DoorsAllClose();
                }
            }

            if (SmartButtonRequest)
            {
                Logger.Debug($"SmartButton on Push ({ServicePushBack.PushStatus})");
                if (!ServicePushBack.IsCalled || (ServicePushBack.State == GsxServiceState.Callable && ServicePushBack.IsTugConnected && GsxController.Menu.MenuState == GsxMenuState.TIMEOUT))
                {
                    Logger.Information($"Automation: Call Pushback on SmartButton");
                    await ServicePushBack.Call();
                    
                    if (GroundEquipmentPlaced)
                    {
                        Logger.Debug($"Remove Ground Equipment on SmartButton");
                        SetGroundEquip(false);
                        GroundEquipmentPlaced = false;
                    }
                }
                else if (ServicePushBack.PushStatus >= 5 && ServicePushBack.PushStatus < 9)
                    await ServicePushBack.EndPushback();
            }
            else if (Profile.KeepDirectionMenuOpen && ServicePushBack.IsTugConnected && (GsxController.Menu.MenuState == GsxMenuState.TIMEOUT || (GsxController.Menu.IsMenuReady && GsxController.ServicePushBack.State == GsxServiceState.Callable)))
            {
                Logger.Debug($"Reopen Direction Menu");
                await ServicePushBack.Call();
                await GsxController.Menu.MsgMenuReady.ReceiveAsync();
                if (GsxController.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && GsxController.Menu.MenuLines[2].StartsWith(GsxConstants.MenuPushbackChange, StringComparison.InvariantCultureIgnoreCase))
                    await GsxController.Menu.Select(3, false, false);
            }
        }

        protected virtual void SetGroundEquip(bool set)
        {
            if (Aircraft.HasChocks)
                Aircraft.SetEquipmentChocks(set);
            if (Aircraft.HasCones)
                Aircraft.SetEquipmentCones(set);
            if (Aircraft.HasPca && !set)
                Aircraft.SetEquipmentPca(set);
            if (Aircraft.HasGpuInternal)
                Aircraft.SetEquipmentPower(set);
            if (Aircraft.HasChocks)
                Aircraft.SetEquipmentChocks(set);
        }

        protected virtual async Task RunArrival()
        {
            if (Aircraft.HasChocks && ChockDelay == 0)
            {
                ChockDelay = new Random().Next(Profile.ChockDelayMin, Profile.ChockDelayMax);
                Logger.Information($"Automation: Placing Chocks on Arrival (ETA {ChockDelay}s)");
                _ = Task.Delay(ChockDelay * 1000, GsxController.Token).ContinueWith((_) => Aircraft.SetEquipmentChocks(true));
                if (Aircraft.HasGpuInternal)
                {
                    _ = Task.Delay(60000, Token).ContinueWith(async (_) =>
                    {
                        if (!Aircraft.EquipmentPower)
                        {
                            Logger.Warning($"Failback: Setting GPU after Deboard was called");
                            await Aircraft.SetEquipmentPower(true, true);
                        }
                    });
                }
            }

            if (Aircraft.HasCones && !Aircraft.EquipmentCones && IsGateConnected)
            {
                Logger.Information($"Automation: Placing Cones on Arrival");
                await Aircraft.SetEquipmentCones(true);
                await Task.Delay(500, GsxController.Token);
            }

            if (Aircraft.HasGpuInternal && !Aircraft.EquipmentPower && IsGateConnected)
            {
                Logger.Information($"Automation: Placing GPU on Arrival");
                await Aircraft.SetEquipmentPower(true);
                await Task.Delay(500, GsxController.Token);
            }

            if (Aircraft.HasPca && !Aircraft.EquipmentPca && Profile.ConnectPca > 0 && IsGateConnected)
            {
                if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && HasGateJetway))
                {
                    Logger.Information($"Automation: Placing PCA on Arrival");
                    await Aircraft.SetEquipmentPca(true);
                    await Task.Delay(500, GsxController.Token);
                }
            }

            if (Aircraft.UseGpuGsx && !GsxServices[GsxServiceType.GPU].IsCalled && !GsxServices[GsxServiceType.GPU].IsRunning)
            {
                Logger.Information($"Automation: Calling GSX GPU on Arrival");
                await GsxServices[GsxServiceType.GPU].Call();
                await Task.Delay(500, GsxController.Token);
            }

            GroundEquipmentPlaced = Aircraft.EquipmentPower && ((Aircraft.HasChocks && Aircraft.EquipmentChocks) || !Aircraft.HasChocks);

            if (!ServiceDeboard.IsCalled &&
                (Profile.CallDeboardOnArrival || (!Profile.CallDeboardOnArrival && SmartButtonRequest)))
            {
                Logger.Information("Automation: Call Deboard on Arrival");
                await ServiceDeboard.Call();
            }
            else if (!ServiceDeboard.IsCalled && !IsGateConnected && Profile.CallJetwayStairsOnArrival)
            {
                if (ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled && !Aircraft.HasAirStairForward)
                {
                    Logger.Information("Automation: Call Jetway on Arrival");
                    await ServiceJetway.Call();
                }

                if (!ServiceStairs.IsConnected && !ServiceStairs.IsCalled
                    && ((!ServiceJetway.IsAvailable && !Aircraft.HasAirStairForward && !Aircraft.HasAirStairAft) || (ServiceJetway.IsAvailable && !Aircraft.HasAirStairAft)))
                {
                    Logger.Information("Automation: Call Stairs on Arrival");
                    await ServiceStairs.Call();
                }
            }
        }
    }
}
