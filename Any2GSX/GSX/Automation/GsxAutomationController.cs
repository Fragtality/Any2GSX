using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class GsxAutomationController() : IGsxAutomationController
    {
        public virtual GsxController GsxController => AppService.Instance.GsxController;
        public virtual GsxMenu Menu => GsxController.Menu;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual AircraftController AircraftController => AppService.Instance.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController.Aircraft;
        public virtual Flightplan Flightplan => AppService.Instance.Flightplan;
        public virtual Config Config => GsxController.Config;
        public virtual SettingProfile Profile => AppService.Instance.SettingProfile;
        public virtual NotificationManager NotificationManager => AppService.Instance.NotificationManager;
        public virtual NotificationTracker Tracker => AppService.Instance.NotificationTracker;
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool RunFlag { get; protected set; } = false;
        public virtual bool IsStarted { get; protected set; } = false;
        public virtual bool FirstRun { get; protected set; } = true;
        public virtual AutomationState State { get; protected set; } = AutomationState.SessionStart;
        public virtual bool IsOnGround => GsxController.IsOnGround;
        public virtual DepartureServiceQueue DepartureQueue { get; } = new();
        public virtual GsxServiceType NextType => DepartureQueue.NextType;
        public virtual bool DepartureServicesCompleted => DepartureQueue.ServicesCompleted;
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
        protected virtual GsxServiceCleaning ServiceCleaning => GsxController.ServiceCleaning;
        protected virtual GsxServiceLavatory ServiceLavatory => GsxController.ServiceLavatory;
        protected virtual GsxServiceWater ServiceWater => GsxController.ServiceWater;
        public virtual bool IsGateConnected => GsxController.IsGateConnected;
        public virtual bool HasDepartBypassed => GsxController.ServiceRefuel.State == GsxServiceState.Bypassed || GsxController.ServiceBoard.State == GsxServiceState.Bypassed;
        public virtual bool HasGateJetway => GsxController.HasGateJetway;
        public virtual bool HasGateStair => GsxController.HasGateStair;
        public virtual bool ServicesValid => GsxController.ServicesValid;

        public virtual DateTime TimeNextTurnCheck { get; set; } = DateTime.MinValue;
        public virtual bool ExecutedReposition { get; protected set; } = false;
        public virtual GroundEquipManager EquipManager { get; } = new();
        public virtual bool JetwayStairRemoved { get; protected set; } = false;
        public virtual bool IsFinalReceived { get; protected set; } = false;
        public virtual bool CockpitPrepNotified { get; protected set; } = false;
        public virtual bool CockpitChocksNotified { get; protected set; } = false;
        public virtual int FinalDelay { get; protected set; } = 0;
        public virtual PayloadReport PayloadArrival { get; set; } = new(0);
        public virtual long OfpArrivalId => PayloadArrival?.Id ?? 0;
        public virtual bool RunDepartureOnArrival { get; protected set; } = false;

        public virtual bool HasSmartButtonRequest { get; protected set; } = false;
        public virtual bool ReadyDepartureServices { get; protected set; } = false;
        public virtual bool IsBoardingCompleted { get; protected set; } = false;
        public virtual bool IsFuelOnStairSide { get; protected set; } = false;
        public virtual bool IsCargo { get; protected set; } = false;
        public virtual bool HasFuelSync { get; protected set; } = false;
        public virtual bool HasAirStairForward { get; protected set; } = false;
        public virtual bool HasAirStairAft { get; protected set; } = false;
        public virtual bool LightBeacon { get; protected set; } = false;
        public virtual bool LightNav { get; protected set; } = false;
        public virtual double Speed { get; protected set; } = 0;
        public virtual double FuelOnBoard { get; protected set; } = 0;
        public virtual double ZeroFuel { get; protected set; } = 0;
        public virtual double WeightTotal { get; protected set; } = 0;
        public virtual bool EnginesRunning { get; protected set; } = false;
        protected virtual bool LastBrake { get; set; } = false;
        public virtual bool BrakeChanged => LastBrake != EquipManager.BrakeSet;


        public event Func<AutomationState, Task> OnStateChange;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                GsxController.OnCouatlStarted += OnCouatlStarted;
            }
        }

        public virtual void FreeResources()
        {
            try
            {
                GsxController.OnCouatlStarted -= OnCouatlStarted;
            }
            catch { }

            IsInitialized = false;
        }

        public virtual async Task Reset()
        {
            IsStarted = false;
            RunFlag = false;
            FirstRun = true;
            State = AutomationState.SessionStart;

            foreach (var service in GsxServices)
                await service.Value.ResetState();

            TimeNextTurnCheck = DateTime.MinValue;
            ExecutedReposition = false;
            CockpitPrepNotified = false;
            CockpitChocksNotified = false;
            JetwayStairRemoved = false;
            IsFinalReceived = false;
            FinalDelay = 0;
            PayloadArrival = new(0);
            RunDepartureOnArrival = false;

            EquipManager.Reset();
            ReadyDepartureServices = false;
            HasSmartButtonRequest = false;
            IsBoardingCompleted = false;
            IsFuelOnStairSide = false;
            IsCargo = false;
            HasFuelSync = false;
            HasAirStairForward = false;
            HasAirStairAft = false;
            LightBeacon = false;
            LightNav = false;
            Speed = 0;
            FuelOnBoard = 0;
            ZeroFuel = 0;
            WeightTotal = 0;
            EnginesRunning = false;
            LastBrake = false;

            DepartureQueue.Reset();
        }

        protected virtual async Task ResetFlight()
        {
            Menu.ResetFlight();
            foreach (var service in GsxServices)
                await service.Value.ResetState(Config.ResetGsxStateVarsFlight);

            TimeNextTurnCheck = DateTime.MinValue;
            EquipManager.Reset();
            CockpitPrepNotified = false;
            CockpitChocksNotified = false;
            JetwayStairRemoved = false;
            IsFinalReceived = false;
            FinalDelay = 0;
            PayloadArrival = new(0);
            RunDepartureOnArrival = false;
            LastBrake = false;
        }

        public virtual async Task RefreshAircraft()
        {
            LastBrake = EquipManager.BrakeSet;
            await EquipManager.Refresh();
            ReadyDepartureServices = await Aircraft.GetReadyDepartureServices();
            HasSmartButtonRequest = await Aircraft.GetSmartButtonRequest() || GsxController?.SubDefaultSmartButton?.GetNumber() != 0;
            IsBoardingCompleted = await Aircraft.GetIsBoardingCompleted();
            IsFuelOnStairSide = await Aircraft.GetIsFuelOnStairSide();
            IsCargo = await Aircraft.GetIsCargo();
            HasFuelSync = await Aircraft.GetHasFuelSync();
            HasAirStairForward = await Aircraft.GetHasAirStairForward();
            HasAirStairAft = await Aircraft.GetHasAirStairAft();
            LightBeacon = await Aircraft.GetLightBeacon();
            LightNav = await Aircraft.GetLightNav();
            Speed = await Aircraft.GetSpeed();
            FuelOnBoard = await Aircraft.GetFuelOnBoardKg();
            ZeroFuel = await Aircraft.GetWeightZeroFuelKg();
            WeightTotal = await Aircraft.GetWeightTotalKg();
            EnginesRunning = await Aircraft.GetEngineRunning();
        }

        public virtual async Task Run()
        {
            try
            {
                RunFlag = true;

                while (RunFlag && GsxController.IsActive && !GsxController.Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                {
                    if (Aircraft?.IsConnected == true && GsxController.IsGsxRunning && GsxController.CanAutomationRun)
                    {
                        await RefreshAircraft();

                        if (!IsStarted)
                        {
                            if (Aircraft?.IsConnected == true && GsxController?.SkippedWalkAround == true && HasSmartButtonRequest)
                            {
                                Logger.Debug($"Reset Smart Button on Service Start");
                                await Aircraft.ResetSmartButton();
                            }

                            Logger.Information($"Automation Service started");
                            IsStarted = true;
                        }

                        await EvaluateState();

                        if (Profile.RunAutomationService && Aircraft.IsConnected && Menu.FirstReadyReceived)
                        {
                            EvaluateSmartCalls();
                            if (ServicesValid)
                                await RunServices();
                            if (HasSmartButtonRequest)
                                await RunSmartCalls();
                        }

                        if (Profile.RunAutomationService && HasSmartButtonRequest)
                        {
                            await Aircraft.ResetSmartButton();
                            if (GsxController?.SubDefaultSmartButton?.GetNumber() != 0)
                                await GsxController.SubDefaultSmartButton.WriteValue(0);
                        }
                    }

                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                {
                    Logger.LogException(ex);
                    RunFlag = false;
                }
            }

            IsStarted = false;

            Logger.Information($"Automation Service ended");
        }

        public virtual void Stop()
        {
            IsStarted = false;
            RunFlag = false;
        }

        protected virtual async Task EvaluateState()
        {
            //Session Start => Prep / Push / Taxi-Out / Flight
            if (State == AutomationState.SessionStart)
            {
                if (!IsOnGround || Config.DebugArrival)
                {
                    await Flightplan.Import();
                    Logger.Debug($"Starting in {AutomationState.Flight} - IsOnGround {IsOnGround} | DebugArrival {Config.DebugArrival}");
                    StateChange(AutomationState.Flight);
                }
                else if ((EquipManager.AvionicsPowered && (EnginesRunning || LightBeacon)) || ServicePushBack.PushStatus > 0)
                {
                    await Flightplan.Import();
                    if ((LightBeacon && !EnginesRunning) || ServicePushBack.PushStatus > 0)
                    {
                        Logger.Debug($"Starting in {AutomationState.Pushback} - Beacon {LightBeacon} | PushStatus {ServicePushBack.PushStatus > 0}");
                        await SetPushback();
                    }
                    else
                    {
                        Logger.Debug($"Starting in {AutomationState.TaxiOut} - EnginesRunning {EnginesRunning}");
                        await EquipManager.RemoveGroundEquip("TaxiOut");
                        StateChange(AutomationState.TaxiOut);
                    }

                }
                else if (ReadyDepartureServices
                        || ServiceRefuel.State > GsxServiceState.Requested
                        || ServiceCatering.State > GsxServiceState.Requested
                        || ServiceBoard.State > GsxServiceState.Requested)
                {
                    if (!Flightplan.IsLoaded)
                        await Flightplan.Import();

                    if (IsBoardingCompleted)
                    {
                        Logger.Debug($"Starting in {AutomationState.Pushback} - WeightTotalKg {await Aircraft.GetWeightTotalKg()} | WeightTotalRampKg {Flightplan.WeightTotalRampKg}");
                        DepartureQueue.FinishServices();
                        await SetPushback();
                    }
                    else if (Menu.IsGateMenu)
                    {
                        Logger.Debug($"Starting in {AutomationState.Departure} - ReadyForDepartureServices {ReadyDepartureServices} | FlightplanLoaded {Flightplan.IsLoaded}");
                        await EquipManager.PlaceGroundEquipment("Departure");
                        DepartureQueue.BuildQueue();
                        await DepartureQueue.CheckQueueSkips();
                        StateChange(AutomationState.Departure);
                    }
                    else if (Profile.RunAutomationService && Menu.MenuCommandsAllowed && !Menu.IsReady)
                    {
                        Logger.Information("Automation: Refresh Menu to start Departure Phase ...");
                        await RefreshCheckGateMenu();
                    }
                }
                else if (Aircraft.IsConnected && GsxController.SkippedWalkAround)
                {
                    if (FirstRun)
                    {
                        if (OnStateChange != null)
                        {
                            await OnStateChange?.Invoke(State);
                            await Task.Delay(Config.StateMachineInterval, RequestToken);
                        }
                        FirstRun = false;
                    }

                    if (Menu.IsGateMenu)
                    {
                        DepartureQueue.BuildQueue();
                        StateChange(AutomationState.Preparation);
                    }
                    else if (Profile.RunAutomationService && Menu.MenuCommandsAllowed && !Menu.IsReady)
                    {
                        Logger.Information("Automation: Refresh Menu to start Preparation Phase ...");
                        await RefreshCheckGateMenu();
                    }
                }
            }
            //intercept Flight
            else if (State < AutomationState.Flight && !IsOnGround)
            {
                StateChange(AutomationState.Flight);
                await ResetFlight();
            }
            //intercept TaxiOut
            else if (State < AutomationState.TaxiOut && EnginesRunning && ServicePushBack.State != GsxServiceState.Active && Speed > 1)
            {
                Logger.Debug($"Intercepting Taxi Out!");
                StateChange(AutomationState.TaxiOut);
            }
            //Preparation => Departure
            else if (State == AutomationState.Preparation)
            {
                if ((!Profile.RunAutomationService || ExecutedReposition) && ReadyDepartureServices && EquipManager.EquipmentPlaced())
                {
                    Logger.Information($"Aircraft is ready for Departure Services");
                    await Flightplan.Import();
                    if (Profile.RunAutomationService && Profile.RefreshGsxOnDeparture)
                        await RefreshGsxSimbrief();
                    await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                    StateChange(AutomationState.Departure);
                }
                else if (IsBoardingCompleted && Flightplan.IsLoaded)
                {
                    Logger.Information($"Aircraft already boarded - skipping Preparation");
                    Logger.Debug($"Switching to {AutomationState.Pushback} - WeightTotalKg {await Aircraft.GetWeightTotalKg()} | WeightTotalRampKg {Flightplan.WeightTotalRampKg}");
                    DepartureQueue.FinishServices();
                    StateChange(AutomationState.Pushback);
                }
                else if (ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning)
                {
                    Logger.Information($"Departure Services already running - skipping Preparation");
                    await Flightplan.Import();

                    if (!ServiceBoard.IsRunning)
                        await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                    StateChange(AutomationState.Departure);
                }
                else if ((EquipManager.AvionicsPowered && (EnginesRunning || LightBeacon)) || ServicePushBack.PushStatus > 0)
                {
                    Tracker.Clear(AppNotification.GateDepart);
                    await Flightplan.Import();
                    if ((LightBeacon && !EnginesRunning) || ServicePushBack.PushStatus > 0)
                    {
                        Logger.Information($"Pushback already running - skipping Preparation");
                        Logger.Debug($"Switching to {AutomationState.Pushback} - Beacon {LightBeacon} | PushStatus {ServicePushBack.PushStatus > 0}");
                        await SetPushback();
                    }
                    else
                    {
                        Logger.Information($"Engines already running - skipping Preparation");
                        Logger.Debug($"Starting in {AutomationState.TaxiOut} - EnginesRunning {EnginesRunning}");
                        await EquipManager.RemoveGroundEquip("TaxiOut");
                        StateChange(AutomationState.TaxiOut);
                    }

                }

                if (State == AutomationState.Preparation && !ReadyDepartureServices)
                    Tracker.Track(AppNotification.GateDepart);
                else if (State == AutomationState.Departure)
                {
                    Tracker.Clear(AppNotification.GateDepart);
                    DepartureQueue.BuildQueue();
                    await RefreshAircraft();
                }
            }
            //Departure => PushBack
            else if (State == AutomationState.Departure)
            {
                if (DepartureQueue.ServicesCompleted)
                {
                    await SetPushback();
                }
                else if (ServicePushBack.PushStatus > 0 && (ServicePushBack.IsActive || HasDepartBypassed))
                {
                    Logger.Information($"Pushback Service already running - skipping Departure");
                    DepartureQueue.FinishServices();
                    await SetPushback();
                }
            }
            //PushBack => TaxiOut
            else if (State == AutomationState.Pushback)
            {
                if (ServicePushBack.IsCompleted || (!GsxController.IsWalkaround && ServicePushBack.PushStatus == 0 && Speed >= Config.SpeedTresholdTaxiOut))
                    StateChange(AutomationState.TaxiOut);
            }
            //Flight => TaxiIn
            else if (State == AutomationState.Flight)
            {
                if (IsOnGround && Speed < Config.SpeedTresholdTaxiIn)
                {
                    if (Config.DebugArrival)
                        await Task.Delay(1500, RequestToken);
                    Logger.Debug("Entered Taxi-In phase Condition");
                    PayloadArrival = await Aircraft.GetPayload();
                    Logger.Debug($"OFP ID on Taxi-In: {PayloadArrival.Id}");
                    StateChange(AutomationState.TaxiIn);

                    if (Profile.RunAutomationService)
                    {
                        if (Config.RestartGsxOnTaxiIn)
                        {
                            await Task.Delay(500, RequestToken);
                            Logger.Information($"Automation: Restarting GSX on Taxi-In");
                            await AppService.Instance.RestartGsx();
                        }

                        if (Config.DelayOpenTaxiInMenu > 0 && Menu.MenuCommandsAllowed && !Tracker.HasCapture)
                        {
                            Logger.Information($"Automation: Open Menu in {Config.DelayOpenTaxiInMenu}s for Gate Selection ...");
                            NotificationManager.MenuOpenDelayed = DateTime.Now + TimeSpan.FromSeconds(Config.DelayOpenTaxiInMenu);
                        }
                    }
                }
            }
            //TaxiIn => Arrival
            else if (State == AutomationState.TaxiIn)
            {
                bool switchArrival = false;
                if (!EnginesRunning && EquipManager.BrakeSet && !LightBeacon)
                {
                    if (Profile.RunAutomationService && Menu.MenuCommandsAllowed && !Menu.IsReady)
                    {
                        Logger.Information($"Automation: Refresh Menu for Arrival Services ...");
                        await RefreshCheckGateMenu(() => Profile.EnableMenuForSelection && !Profile.CallDeboardOnArrival && !Profile.CallJetwayStairsOnArrival);
                    }
                    await ServiceDeboard.SetPaxTarget(PayloadArrival.CountPax);

                    switchArrival = true;
                }
                else if (ServiceDeboard.IsRunning)
                {
                    Logger.Information($"Deboard Service already running - skipping TaxiIn");
                    switchArrival = true;
                }

                if (switchArrival)
                {
                    EquipManager.SetRandomDelays();
                    DepartureQueue.ResetFlight();
                    await Flightplan.Unload();
                    StateChange(AutomationState.Arrival);

                    if (Profile.RunDepartureOnArrival)
                    {
                        Logger.Debug($"Setting inital Delay ({Profile.DelayTurnAroundSeconds}s)");
                        TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnAroundSeconds);
                    }
                }
            }
            //Arrival => Turnaround (or Departure)
            else if (State == AutomationState.Arrival)
            {
                if (Profile.RunDepartureOnArrival && !RunDepartureOnArrival && ServiceDeboard.IsRunning && ReadyDepartureServices)
                {
                    if (!Flightplan.IsLoaded && TimeNextTurnCheck <= DateTime.Now && await Flightplan.CheckNewOfp())
                        await Flightplan.Import();
                    if (!Flightplan.IsLoaded && TimeNextTurnCheck <= DateTime.Now)
                        TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnRecheckSeconds);

                    if (Flightplan.IsLoaded && PayloadArrival.Id != Flightplan.Id && !RunDepartureOnArrival)
                    {
                        Logger.Information("Automation: Run Departure Services during Arrival");
                        RunDepartureOnArrival = true;
                        DepartureQueue.BuildQueue();
                        Tracker.TrackTimeout(AppNotification.OfpImported, 30000);
                    }
                }

                if (ServiceDeboard.IsCompleted && !Tracker.IsActive(AppNotification.UpdatesBlocked))
                {
                    if (!Profile.RunDepartureOnArrival || !RunDepartureOnArrival)
                        await SetTurnaround();
                    else
                    {
                        if (!ServiceBoard.IsRunning)
                        {
                            if (Profile.RunAutomationService && Profile.RefreshGsxOnTurn)
                                await RefreshGsxSimbrief();

                            await ServiceBoard.SetPaxTarget(Flightplan.CountPax);
                        }
                        StateChange(AutomationState.Departure);
                    }
                }
            }
            //Turnaround => Departure
            else if (State == AutomationState.TurnAround)
            {
                if (ReadyDepartureServices && !Flightplan.IsLoaded && TimeNextTurnCheck <= DateTime.Now && await Flightplan.CheckNewOfp())
                    await Flightplan.Import();
                if (!Flightplan.IsLoaded && TimeNextTurnCheck <= DateTime.Now)
                {
                    TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnRecheckSeconds);
                    Tracker.TrackTimeout(AppNotification.OfpCheck, Profile.DelayTurnRecheckSeconds * 1000);
                }

                if (ReadyDepartureServices && GsxController.IsGsxRunning && Flightplan.IsLoaded)
                {
                    await SkipTurn(AutomationState.Departure);
                }
                else if (HasSmartButtonRequest)
                {
                    Logger.Information("Skip Turnaround Phase (SmarButton Request)");
                    await SkipTurn(AutomationState.Departure);
                }
                else if (ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning)
                {
                    Logger.Warning($"Departure Services already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.Departure);
                    await DepartureQueue.CheckQueueSkips();
                }
                else if (ServicePushBack.IsRunning)
                {
                    Logger.Warning($"Pushback Service already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.Pushback);
                    DepartureQueue.FinishServices();
                }

                if (State == AutomationState.TurnAround && !ReadyDepartureServices)
                    Tracker.Track(AppNotification.GateDepart);
                else if (State != AutomationState.TurnAround)
                    Tracker.Clear(AppNotification.GateDepart);
            }
        }

        public virtual async Task RefreshCheckGateMenu(Func<bool> enableCondition = null)
        {
            enableCondition ??= () => Profile.EnableMenuForSelection;
            await Menu.RunCommand(GsxMenuCommand.Open(), enableCondition());
            if (Menu.ReadyReceived && Menu.IsGateMenu && Profile.RunAutomationService)
                await Menu.RunCommand(GsxMenuCommand.Disable(), false);
        }

        protected virtual void EvaluateSmartCalls()
        {
            SmartButtonCall call = SmartButtonCall.None;

            if (State == AutomationState.SessionStart)
                call = Tracker.IsActive(AppNotification.GateMove) ? SmartButtonCall.WarpGate : SmartButtonCall.None;
            else if (State == AutomationState.Preparation)
            {
                if (Tracker.IsActive(AppNotification.GateMove))
                    call = SmartButtonCall.WarpGate;
                else
                    call = (ServiceJetway.IsConnectable && !HasAirStairForward) || (ServiceStairs.IsConnectable && CheckAirStairs() && !IsFuelOnStairSide) ? SmartButtonCall.Connect : SmartButtonCall.None;
            }
            else if (State == AutomationState.Departure)
            {
                call = DepartureQueue.HasNext ? SmartButtonCall.NextService : SmartButtonCall.None;
            }
            else if (State == AutomationState.Pushback)
            {
                if (GsxController.ServicePushBack.PushStatus == 8)
                    call = SmartButtonCall.PushConfirm;
                else if (GsxController.ServicePushBack.PushStatus > 4 && GsxController.ServicePushBack.PushStatus < 8)
                    call = SmartButtonCall.PushStop;
                else
                    call = GsxController.ServicePushBack.IsCompleted || GsxController.ServicePushBack.IsRunning ? SmartButtonCall.None : SmartButtonCall.PushCall;
            }
            else if (State == AutomationState.TaxiOut)
            {
                call = Tracker.HasCapture && GsxController.IsDeiceAvail && !ServiceDeice.IsCompleted && Speed < Config.SpeedTresholdTaxiOut && EquipManager.BrakeSet ? SmartButtonCall.Deice : SmartButtonCall.None;
                if (call == SmartButtonCall.Deice && Menu.MenuCommandsAllowed && BrakeChanged)
                {
                    Logger.Information($"Automation: Refresh Menu on Brake-Change for Deice Pad ({Tracker.LastCapturedGate}) ...");
                    _ = Menu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection);
                }
            }
            else if (State == AutomationState.TaxiIn)
            {
                call = Tracker.IsActive(AppNotification.GateMove) ? SmartButtonCall.ClearGate : SmartButtonCall.None;
            }
            else if (State == AutomationState.Arrival)
            {
                call = !Profile.CallDeboardOnArrival && !ServiceDeboard.IsRunning ? SmartButtonCall.Deboard : SmartButtonCall.None;
            }
            else if (State == AutomationState.TurnAround)
            {
                call = SmartButtonCall.SkipTurn;
            }

            Tracker.SmartButton = call;
        }

        protected virtual async Task RefreshGsxSimbrief()
        {
            if (Profile.RunAutomationService)
            {
                Tracker.Track(AppNotification.GsxRefresh);
                await GsxController.ReloadSimbrief();
                Tracker.Clear(AppNotification.GsxRefresh);
            }
        }

        protected virtual async Task SetPushback()
        {
            if (ServiceStairs.IsConnected && ((ServiceJetway.IsAvailable && Profile.RemoveStairsAfterDepature == 2) || (!ServiceJetway.IsAvailable && Profile.RemoveStairsAfterDepature == 1)))
            {
                Logger.Information($"Automation: Remove Stairs after Departure Services");
                await ServiceStairs.Remove();
            }

            if (State == AutomationState.Departure)
                _ = FinalLoadsheet();
            StateChange(AutomationState.Pushback);
        }

        protected virtual async Task SetTurnaround()
        {
            TimeNextTurnCheck = DateTime.Now + TimeSpan.FromSeconds(Profile.DelayTurnAroundSeconds);
            Tracker.TrackTimeout(AppNotification.OfpCheck, Profile.DelayTurnAroundSeconds * 1000);
            if (PayloadArrival.Id == Flightplan.Id)
                await Flightplan.Unload();
            StateChange(AutomationState.TurnAround);
            if (Profile.NotifyTurnReady)
                await Aircraft.NotifyCockpit(CockpitNotification.TurnReady);
            GsxController.Menu.BlockMenuUpdates(false);
        }

        protected virtual async Task SkipTurn(AutomationState state)
        {
            if (!Flightplan.IsLoaded)
                await Flightplan.Import();
            Tracker.Clear(AppNotification.OfpCheck);
            if (state == AutomationState.Departure && !ServiceBoard.IsRunning)
            {
                if (Profile.RunAutomationService && Profile.RefreshGsxOnTurn)
                    await RefreshGsxSimbrief();

                await ServiceBoard.SetPaxTarget(Flightplan.CountPax);
                DepartureQueue.BuildQueue();
            }
            Tracker.TrackTimeout(AppNotification.OfpImported, 30000);
            StateChange(state);
        }

        protected virtual void StateChange(AutomationState newState)
        {
            if (State == newState)
                return;

            Logger.Information($"State Change: {State} => {newState}");
            State = newState;
            _ = TaskTools.RunPool(() => OnStateChange?.Invoke(State), RequestToken);
        }

        public virtual Task OnCouatlStarted(IGsxController gsxController)
        {
            if (!IsStarted)
                return Task.CompletedTask;

            if (State == AutomationState.Departure && !DepartureQueue.HasNext)
            {
                if (ServiceBoard.WasActive && !ServiceBoard.WasCompleted)
                    ServiceBoard.ForceComplete();
                if (ServiceRefuel.WasActive && !ServiceRefuel.WasCompleted)
                    ServiceRefuel.ForceComplete();
                Logger.Information($"GSX Restart on last Departure Service detected - skip to Pushback");
                return SetPushback();
            }
            else if (State == AutomationState.Arrival && ServiceDeboard.WasActive && !ServiceDeboard.WasCompleted)
            {
                ServiceDeboard.ForceComplete();
                Logger.Information($"GSX Restart on Deboarding Service detected - skip to Turnaround");
                return SetTurnaround();
            }

            return Task.CompletedTask;
        }

        protected virtual Task RunServices()
        {
            if (State == AutomationState.Preparation)
            {
                return RunPreparation();
            }
            else if (State == AutomationState.Departure)
            {
                return RunDeparture();
            }
            else if (State == AutomationState.Pushback)
            {
                return RunPushback();
            }
            else if (State == AutomationState.Arrival)
            {
                return RunArrival();
            }
            else
                return Task.CompletedTask;
        }

        protected virtual Task RunSmartCalls()
        {
            if ((Tracker.IsActive(AppNotification.GateMove) || Tracker.SmartButton == SmartButtonCall.Deice) && Menu.MenuCommandsAllowed)
            {
                if (State <= AutomationState.Preparation && Tracker.SmartButton == SmartButtonCall.WarpGate)
                {
                    var sequence = new GsxMenuSequence();
                    sequence.Commands.Add(GsxMenuCommand.Open());
                    sequence.Commands.Add(GsxMenuCommand.Select((int)GsxChangePark.Warp, GsxConstants.MenuParkingChange, ["Warp"]));
                    sequence.ResetMenuCheck = () => false;

                    return Menu.RunSequence(sequence);
                }
                else if (State == AutomationState.TaxiOut && Tracker.SmartButton == SmartButtonCall.Deice)
                {
                    return SmartcallDeicePad();
                }
                else if (State == AutomationState.TaxiIn && Tracker.SmartButton == SmartButtonCall.ClearGate)
                {
                    var sequence = new GsxMenuSequence();
                    sequence.Commands.Add(GsxMenuCommand.Open());
                    sequence.Commands.Add(GsxMenuCommand.Select((int)Profile.ClearGateMenuOption, GsxConstants.MenuParkingChange));
                    sequence.EnableMenuCheck = () => Profile.EnableMenuForSelection && Profile.ClearGateMenuOption < GsxChangePark.ClearAI;
                    sequence.ResetMenuCheck = () => false;

                    return Menu.RunSequence(sequence);
                }
            }

            return Task.CompletedTask;
        }

        protected virtual async Task SmartcallDeicePad()
        {
            Menu.ExternalSequence = true;
            Logger.Debug($"Refresh Menu for Deice Pad ({Tracker.LastCapturedGate}) ...");
            await Menu.RunCommand(GsxMenuCommand.Open(), Profile.EnableMenuForSelection);
            await Menu.WaitInterval(2);

            bool stop = ServiceDeice.IsActive;
            if (Menu.IsDeicePad)
            {
                if (!stop)
                {
                    Logger.Information("SmartButton: Start Deice on Pad");
                    await Menu.RunCommand(GsxMenuCommand.Select(1, GsxConstants.MenuGate), Profile.EnableMenuForSelection);
                }
                else
                {
                    Logger.Information("SmartButton: Stop Deice on Pad");
                    var sequence = new GsxMenuSequence();
                    sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuGate));
                    sequence.Commands.Add(GsxMenuCommand.Wait(2));
                    sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuCancelService));
                    sequence.Commands.Add(GsxMenuCommand.Wait());
                    sequence.EnableMenuCheck = () => false;
                    sequence.EnableMenuAfterResetCheck = () => false;
                    await Menu.RunSequence(sequence);
                }
            }
            Menu.ExternalSequence = false;
        }

        protected virtual bool CheckAirStairs()
        {
            return (!ServiceJetway.IsAvailable && !HasAirStairForward && !HasAirStairAft) || (ServiceJetway.IsAvailable && !HasAirStairAft);
        }

        protected virtual async Task RunPreparation()
        {
            if (Tracker.IsActive(AppNotification.GateSelect) || Tracker.IsActive(AppNotification.GateMove))
                return;
            ExecutedReposition = ServiceReposition.IsCompleted || IsGateConnected || !Profile.CallReposition || ReadyDepartureServices || ServiceReposition.IsCalled || ServiceReposition.WasRepoReceived;

            //Reposition
            if (!ExecutedReposition)
            {
                Logger.Information("Automation: Reposition Aircraft on Gate");
                await ServiceReposition.Call();
            }
            else if (!Profile.CallReposition)
                await Task.Delay(1500, RequestToken);

            //Equipment & Jetway/Stairs
            if (ExecutedReposition && GsxController.SkippedWalkAround)
            {
                if (!EquipManager.EquipmentPlaced())
                {
                    Tracker.Track(AppNotification.GateEquip);
                    await EquipManager.PlaceGroundEquipment("Preparation");
                    await Task.Delay(100, RequestToken);
                    await EquipManager.Refresh();
                }

                if (Profile.CallJetwayStairsOnPrep || HasSmartButtonRequest)
                {
                    if (ServiceJetway.IsConnectable && !HasAirStairForward)
                    {
                        Logger.Information("Automation: Call Jetway on Preparation");
                        await ServiceJetway.Call();
                    }

                    if (ServiceStairs.IsConnectable && CheckAirStairs() && !IsFuelOnStairSide)
                    {
                        Logger.Information("Automation: Call Stairs on Preparation");
                        await ServiceStairs.Call();
                    }
                }
            }

            //Notification
            if (!CockpitPrepNotified && ExecutedReposition && EquipManager.EquipmentPlaced())
            {
                Tracker.Clear(AppNotification.GateEquip);
                if (Profile.NotifyPrepFinished)
                    await Aircraft.NotifyCockpit(CockpitNotification.PrepFinished);
                CockpitPrepNotified = true;
            }
        }

        protected virtual async Task RunDeparture()
        {
            await EquipManager.RemoveDepartureEquip();

            //Skip Departure on BoardingCompleted
            bool skip = false;
            if (!DepartureQueue.ServicesCompleted && IsBoardingCompleted && ServiceRefuel.State != GsxServiceState.Completed && ServiceBoard.State != GsxServiceState.Completed && State == AutomationState.Departure)
            {
                Logger.Information($"Automation: Skip Departure Services - Plane already boarded");
                DepartureQueue.FinishServices();
                skip = true;
            }

            //Apply Service Skips (Activation, Constraints, State)
            await DepartureQueue.CheckQueueSkips();

            //Departure Queue finished
            if (DepartureQueue.ServicesCompleted)
            {
                if (!skip)
                    Logger.Information($"Automation: All Departure Services completed");
                return;
            }

            //Skip when Commands active
            if (!Menu.MenuCommandsAllowed)
            {
                Logger.Verbose("Waiting for Menu Command to be allowed");
                return;
            }

            //Apply Run Time Constraint
            await DepartureQueue.ApplyRunTime();

            //Skip when certain Services are blocked
            if (!await CheckServiceBlocks())
                return;

            //Handle Stairs & Fuel (Port Side)
            if (IsFuelOnStairSide && Profile.AttemptConnectStairRefuel && ServiceStairs.IsAvailable)
            {
                if (!await ConnectStairsAndFuel())
                    return;
            }

            //Jetway / Stairs during Departure
            if (Profile.CallJetwayStairsDuringDeparture && State == AutomationState.Departure
                && DepartureQueue.NextType != GsxServiceType.Boarding && !DepartureQueue.HasCalledRunning(GsxServiceType.Boarding)
                && DepartureQueue.NextType != GsxServiceType.Cleaning && !DepartureQueue.HasCalledRunning(GsxServiceType.Cleaning))
            {
                if (ServiceJetway.IsConnectable && !HasAirStairForward)
                {
                    Logger.Information("Automation: Call Jetway during Departure");
                    await ServiceJetway.Call();
                }

                if (CheckAirStairs() && ServiceStairs.IsConnectable && (ServiceRefuel.IsCompleted || !IsFuelOnStairSide))
                {
                    Logger.Information($"Automation: Call Stairs {(ServiceRefuel.IsCompleted ? "after Refuel" : "during Departure")}");
                    await ServiceStairs.Call();
                }
            }

            //Run Service Queue
            if ((DepartureQueue.HasNext && HasSmartButtonRequest) || DepartureQueue.IsNextCallable())
            {
                if (NextType == GsxServiceType.Boarding)
                    await ServiceBoard.SetPaxTarget(Flightplan.CountPax);

                if (HasSmartButtonRequest && Profile.SmartButtonAbortService > GsxCancelService.Never)
                    await CanceLastService();

                await DepartureQueue.CallNext();
            }
        }

        protected virtual async Task CanceLastService()
        {
            if (DepartureQueue.HasLastService)
            {
                Logger.Information($"Automation: Cancel last Service {DepartureQueue.LastCalled.Service.Type} on SmartButton Call");
                await DepartureQueue.LastCalled.Service.Cancel();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }
            else if (State == AutomationState.Arrival && ServiceDeboard.IsActive)
            {
                Logger.Information($"Automation: Cancel last Service {ServiceDeboard.Type} on SmartButton Call");
                await ServiceDeboard.Cancel();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }
        }

        protected virtual async Task<bool> CheckServiceBlocks()
        {
            //Wait for Stairs to connect before Lavatory
            if (!ServiceStairs.IsActive && ServiceStairs.IsOperating && NextType == GsxServiceType.Lavatory)
            {
                Logger.Verbose($"Waiting for Stairs to finish before {NextType}");
                return false;
            }

            //Block Cleaning while Deboard running
            if (ServiceDeboard.IsRunning && NextType == GsxServiceType.Cleaning)
            {
                Logger.Verbose($"Waiting for Deboard to finish before {NextType}");
                return false;
            }

            return true;
        }

        protected virtual async Task CallNextWithStairs(GsxServiceType nextType, Func<Task> callNext = null)
        {
            Logger.Information($"Stair-Fix: Call Stairs before {nextType} ...");
            await ServiceStairs.Call();
            await Menu.WaitInterval();
            if (!ServiceStairs.IsCalled)
                return;

            Logger.Information($"Stair-Fix: Wait for Stairs extending before calling {nextType} ...");
            int wait = 0;
            while (ServiceStairs.IsOperating && !ServiceStairs.StairsExtending() && wait <= Profile.DelayCallRefuelAfterStair * 1000 && !RequestToken.IsCancellationRequested)
            {
                await Task.Delay(Config.StateMachineInterval, RequestToken);
                wait += Config.StateMachineInterval;
            }

            if (callNext != null)
                await callNext();
            else
                await DepartureQueue.CallNext();
            await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
        }

        protected virtual async Task<bool> ResetStairSequence()
        {
            Logger.Information("Stair-Fix: Reset Stair Service ...");
            if (ServiceStairs.IsConnected)
            {
                await ServiceStairs.Remove();
                await Task.Delay(Config.MenuOpenTimeout, RequestToken);
            }
            await ServiceStairs.Cancel(GsxCancelService.Abort);
            await Task.Delay(Config.MenuOpenTimeout, RequestToken);
            return !ServiceStairs.IsOperating || ServiceStairs.IsAnyStair((state) => state == GsxVehicleStairState.Idle);
        }

        protected virtual async Task<bool> ConnectStairsAndFuel()
        {
            //Block Refuel while Deboard running
            if (ServiceDeboard.IsRunning && NextType == GsxServiceType.Refuel)
            {
                Logger.Verbose($"Waiting for Deboard to finish before {NextType}");
                return false;
            }

            //Block Cleaning until Refuel active
            if (NextType == GsxServiceType.Cleaning && ServiceRefuel.State == GsxServiceState.Requested)
            {
                Logger.Verbose($"Waiting for Refuel to become active before {NextType}");
                return false;
            }

            //Fix Cleaning (-> Refuel) & Stairs
            if (NextType == GsxServiceType.Cleaning && DepartureQueue.IsNextCallable())
            {
                bool fuelQueued = DepartureQueue.IsQueued(GsxServiceType.Refuel) && !ServiceRefuel.IsRunning;
                if (ServiceStairs.IsConnectable && !fuelQueued)
                {
                    Logger.Information("Stair-Fix: Call Stairs before Cleaning ...");
                    await ServiceStairs.Call();
                    return false;
                }
                else if (ServiceStairs.IsOperating && !fuelQueued)
                {
                    Logger.Verbose($"Waiting for Stairs to finish before {NextType}");
                    return false;
                }
                else if (fuelQueued)
                {
                    var activation = DepartureQueue.GetQueuedService(GsxServiceType.Refuel).Config.ServiceActivation;
                    bool callable = activation == GsxServiceActivation.AfterActive || activation == GsxServiceActivation.AfterCalled || activation == GsxServiceActivation.AfterRequested;
                    if (callable)
                    {
                        Logger.Information("Stair-Fix: Call Refuel before Catering ...");
                        if (ServiceStairs.IsConnected || ServiceStairs.IsOperating)
                        {
                            if (await ResetStairSequence())
                                await CallNextWithStairs(GsxServiceType.Refuel, ServiceRefuel.Call);
                        }
                        else
                            await CallNextWithStairs(GsxServiceType.Refuel, ServiceRefuel.Call);

                        return false;
                    }
                }
            }
            //Fix Refuel & Stairs
            else if (NextType == GsxServiceType.Refuel && DepartureQueue.IsNextCallable())
            {
                if (ServiceStairs.IsConnectable)
                {
                    await CallNextWithStairs(NextType);
                    return false;
                }
                else if (ServiceStairs.IsConnected || ServiceStairs.IsOperating)
                {
                    if (await ResetStairSequence())
                        await CallNextWithStairs(NextType);
                    return false;
                }
            }

            return true;
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
                Tracker.TrackTimeout(AppNotification.SheetFinal, FinalDelay * 1000);
                while (FinalDelay > 0)
                {
                    await Task.Delay(1000, RequestToken);
                    FinalDelay--;
                }
                Logger.Debug("Generate Final LS ...");
                await Aircraft.GenerateLoadsheetFinal();
                Logger.Information($"Final Loadsheet received!");
                Tracker.Clear(AppNotification.SheetFinal);

                if (Profile.RunAutomationService && !JetwayStairRemoved && IsGateConnected && Profile.RemoveJetwayStairsOnFinal)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Final Loadsheet");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 6, RequestToken);
                }

                if (Profile.RunAutomationService && Profile.CloseDoorsOnFinal && await Aircraft.GetHasOpenDoors())
                {
                    Logger.Information($"Automation: Close Doors on Final Loadsheet");
                    await AircraftController.Aircraft.DoorsAllClose();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }

                if (Profile.NotifyFinalReceived)
                    _ = Aircraft.NotifyCockpit(CockpitNotification.FinalReceived);
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
            //Cancel Services
            if (DepartureQueue.HasCalledRunning() && Profile.CancelServicesOnPushPhase)
                await DepartureQueue.CancelRunningServices("Pushback Phase");

            //PCA Handling
            if (EquipManager.HasPca && EquipManager.ApuRunning && EquipManager.ApuBleed && EquipManager.EquipmentPca)
            {
                Logger.Information($"Automation: Disconnecting PCA on APU Bleed on");
                await Aircraft.SetEquipmentPca(false, ServicePushBack.PushStatus >= 3 || ServicePushBack.IsActive);
            }

            //Gradual Equipment
            if (Profile.GradualGroundEquipRemoval && !EquipManager.EquipmentRemoved())
            {
                await EquipManager.RemoveGroundEquip("Pushback Phase (Gradual)");
            }

            //Beacon <> Equip Handling
            if (!EquipManager.PowerConnected && EquipManager.BrakeSet && LightBeacon && !EnginesRunning)
            {
                if (Profile.ClearGroundEquipOnBeacon)
                    await EquipManager.RemoveGroundEquip("Beacon");

                if (Profile.CallPushbackOnBeacon && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested && !ServicePushBack.WasActive)
                {
                    Logger.Information($"Automation: Call Pushback (Beacon / Prepared for Push)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }
                else if (IsGateConnected && Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Beacon");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }
            }

            //Pushback Call handling
            if (ServicePushBack.TugAttachedOnBoarding && Profile.CallPushbackWhenTugAttached > 0 && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
            {
                if (Profile.CallPushbackWhenTugAttached == 1)
                {
                    Logger.Information($"Automation: Call Pushback after Departure Services (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                else if (Profile.CallPushbackWhenTugAttached == 2 && IsFinalReceived)
                {
                    Logger.Information($"Automation: Call Pushback after Final LS (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
            }

            //Expedited Equip removal & Door Closure
            if (ServicePushBack.IsRunning || ServiceDeice.IsRunning || EnginesRunning)
            {
                string reason = "Pushback Running";
                if (ServiceDeice.IsRunning)
                    reason = "De-Ice";
                else if (EnginesRunning)
                    reason = "Engines Running";

                if (await Aircraft.GetHasOpenDoors())
                {
                    Logger.Information($"Automation: Close Doors on {reason}");
                    await Aircraft.DoorsAllClose();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }

                if (!EquipManager.EquipmentRemoved())
                    await EquipManager.RemoveGroundEquip(reason, EnginesRunning);
            }

            if (Tracker.IsActive(AppNotification.PushReleaseBrake))
            {
                if (EquipManager.BrakeSet && ServicePushBack.IsTugConnected)
                {
                    NotificationManager.LastPushInfo = "Release Brake!";
                    Tracker.TrackMessage(AppNotification.PushPhase, NotificationManager.LastPushInfo);
                }
                else if (ServicePushBack.PushStatus >= 5)
                    Tracker.Clear(AppNotification.PushReleaseBrake);
            }

            //SmartButton Handling
            if (HasSmartButtonRequest)
            {
                Logger.Debug($"SmartButton on Push ({ServicePushBack.PushStatus})");
                if (ServicePushBack.PushStatus < 5)
                {
                    await EquipManager.RemoveGroundEquip("SmartButton");
                    Logger.Information($"Automation: Call Pushback on SmartButton");
                    await ServicePushBack.Call();
                    if (ServicePushBack.IsWaitingForDirection)
                    {
                        Menu.ExternalSequence = true;
                        await Menu.WaitInterval(5);

                        if (Menu.IsReady && Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && Menu.MatchMenuLine(2, GsxConstants.MenuPushbackChange))
                            await Menu.RunCommand(GsxMenuCommand.Select(3), Profile.EnableMenuForSelection);

                        await Menu.WaitInterval(2);
                        Menu.ExternalSequence = false;
                    }
                }
                else if (ServicePushBack.PushStatus >= 5 && ServicePushBack.PushStatus < 9)
                    await ServicePushBack.EndPushback();
            }
        }

        protected virtual async Task RunArrival()
        {
            //Equipment
            if (!EquipManager.EquipmentPlaced())
            {
                Tracker.Track(AppNotification.GateEquip);
                await EquipManager.PlaceGroundEquipment("Arrival");
                await Task.Delay(100, RequestToken);
                await EquipManager.Refresh();
            }

            //Chock Notification
            if (EquipManager.HasChocks && EquipManager.EquipmentChocks && !CockpitChocksNotified)
            {
                if (Profile.NotifyChocksPlaced)
                    await Aircraft.NotifyCockpit(CockpitNotification.ChocksPlaced);
                CockpitChocksNotified = true;
            }

            //GSX Menu Trigger
            if (!Menu.IsGateMenu && !NotificationManager.MenuOpenQueued && Menu.MenuCommandsAllowed && (ServiceJetway.State == GsxServiceState.Unknown || ServiceStairs.State == GsxServiceState.Unknown))
            {
                await RefreshCheckGateMenu(() => false);
                return;
            }

            //Deboard or Jetway/Stairs
            if (!ServiceDeboard.IsCalled && (Profile.CallDeboardOnArrival || (!Profile.CallDeboardOnArrival && HasSmartButtonRequest)))
            {
                if (HasSmartButtonRequest)
                    Logger.Information("Call Deboard on SmartButton");
                else
                    Logger.Information("Automation: Call Deboard on Arrival");
                await ServiceDeboard.Call();
            }
            else if (!ServiceDeboard.IsCalled && !IsGateConnected && Profile.CallJetwayStairsOnArrival)
            {
                if (ServiceJetway.IsConnectable && !HasAirStairForward)
                {
                    Logger.Information("Automation: Call Jetway on Arrival");
                    await ServiceJetway.Call();
                }

                if (!ServiceStairs.IsConnectable && CheckAirStairs())
                {
                    Logger.Information("Automation: Call Stairs on Arrival");
                    await ServiceStairs.Call();
                }
            }

            if (EquipManager.EquipmentPlaced())
                Tracker.Clear(AppNotification.GateEquip);

            if (RunDepartureOnArrival && NextType != GsxServiceType.Boarding)
            {
                await RunDeparture();
            }
        }
    }
}
