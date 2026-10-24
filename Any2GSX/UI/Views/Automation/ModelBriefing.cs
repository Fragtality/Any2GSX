using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelBriefing(AppService appService) : ModelBase<SettingProfile>(appService?.SettingProfile, appService)
    {
        public virtual string NotAssigned => "NOT ASSIGNED";
        public static string NotAvailable => "NOT AVAIL";
        public override SettingProfile Source => AppService?.SettingProfile;
        protected virtual Flightplan Flightplan => AppService?.Flightplan;
        protected virtual GsxGate GsxGate => GsxController.Gate;
        protected virtual AircraftBase Aircraft => AircraftController?.Aircraft;

        public virtual DispatcherTimerAsync WeightRefreshTimer { get; protected set; } = new();
        protected virtual bool IsShowAllowed => IsSessionInitialized && IsFlightplanLoaded;
        public virtual bool IsSessionInitialized { get; set; } = false;
        [ObservableProperty]
        public virtual partial bool IsFlightplanLoaded { get; set; }
        public virtual bool IsAtGate => !string.IsNullOrWhiteSpace(ActiveGate) && ActiveGate?.Equals(NotAvailable, StringComparison.InvariantCultureIgnoreCase) == false;
        [ObservableProperty]
        public virtual partial string ActiveGate { get; set; } = NotAvailable;
        [ObservableProperty]
        public virtual partial string QueuedServices { get; set; } = NotAvailable;
        public virtual ObservableCollection<string> PushbackDirections { get; } = ["NOT ASSIGNED"];
        public virtual bool IsPushSelectable => AppService.IsSessionInitialized && AutomationController?.ExternalServiceControl == false
                                                && AutomationController?.State < AutomationState.TaxiOut && GsxController?.ServicePushBack?.PushStatus < 5
                                                && GsxController.ApiData?.HandlerData?.gate?.IsPushbackEnabled == true && GsxController.ApiData?.HandlerData?.gate?.HasDirections == true;
        public virtual ObservableCollection<string> GateLabels { get; } = ["No Label", "Gate", "Stand", "Ramp", "Parking", "Cargo", "Dock"];
        public virtual List<string> ArrivalGates { get; } = [];
        public virtual ObservableCollection<string> GatesFiltered { get; } = [];
        public virtual bool IsGateSelectable => AppService.IsSessionInitialized && AutomationController?.ExternalServiceControl == false;
        protected static Dictionary<GsxServiceType, string> ServiceDisplay { get; } = new()
        {
            { GsxServiceType.Lavatory, "LAV" },
            { GsxServiceType.Water, "WATER" },
            { GsxServiceType.Refuel, "FUEL" },
            { GsxServiceType.Catering, "CATER" },
            { GsxServiceType.Cleaning, "CLEAN" },
            { GsxServiceType.Boarding, "BOARD" },
        };

        protected override void InitializeModel()
        {
            GsxController.ApiClient.AirportNameChanged += (icao) => RunOnDispatcher(() => OnAirportNameChanged(icao));
            GsxController.ApiClient.GateNameChanged += (name) => RunOnDispatcher(() => OnGateNameChanged(name));
            AutomationController.OnStateChange += (state) => RunOnDispatcher(() => OnAutomationStateChange(state));
            AutomationController.DepartureQueue.ServiceQueueChanged += (list) => RunOnDispatcher(() => OnServiceQueueChanged(list));
            AutomationController.DepartureQueue.ExternalControlChanged += () => RunOnDispatcher(() => OnExternalControlChanged());
            GsxGate.OnPushDirectionChanged += (_) => RunOnDispatcher(() => OnPropertyChanged(nameof(SelectedPushDirection)));
            GsxGate.OnArrivalGateChanged += (_) => RunOnDispatcher(() => OnPropertyChanged(nameof(AssignedArrivalGate)));
            GsxController.ServicePushBack.OnPushStatus += (_) => RunOnDispatcher(() => OnPropertyChanged(nameof(IsPushSelectable)));
            Flightplan.OnImport += (_) => RunOnDispatcher(() => NotifyFlightPlan(true));
            Flightplan.OnChanged += () => RunOnDispatcher(() => NotifyFlightPlan(IsFlightplanLoaded));
            Flightplan.OnUnload += () => RunOnDispatcher(() => NotifyFlightPlan(false));
            Config.PropertyChanged += OnConfigPropertyChanged;
            AppService.MessageService.Subscribe<MsgAppSessionStarted>(() => RunOnDispatcher(() => OnSessionChange(true)));
            AppService.MessageService.Subscribe<MsgAppSessionStopped>(() => RunOnDispatcher(() => OnSessionChange(false)));

            WeightRefreshTimer.Interval = TimeSpan.FromSeconds(1);
            WeightRefreshTimer.Tick += OnRefreshTick;
        }

        protected virtual async Task OnRefreshTick()
        {
            try
            {
                if (Aircraft != null)
                {
                    LiveTotal = FormatWeightAircraft(await Aircraft.GetWeightTotalKg());
                    LiveZeroFuel = FormatWeightAircraft(await Aircraft.GetWeightZeroFuelKg());
                    LiveFuel = FormatWeightAircraft(await Aircraft.GetFuelOnBoardKg());
                    LiveEmpty = FormatWeightAircraft(Aircraft.WeightEmptyKg);
                }
                else
                {
                    LiveTotal = FormatWeightAircraft(0);
                    LiveZeroFuel = FormatWeightAircraft(0);
                    LiveFuel = FormatWeightAircraft(0);
                    LiveEmpty = FormatWeightAircraft(0);
                }
                _ = RunOnDispatcher(NotifyLiveWeights);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void NotifyLiveWeights()
        {
            OnPropertyChanged(nameof(LiveTotal));
            OnPropertyChanged(nameof(LiveZeroFuel));
            OnPropertyChanged(nameof(LiveFuel));
            OnPropertyChanged(nameof(LiveEmpty));
        }

        protected virtual void ResetPushDirections()
        {
            PushbackDirections.Clear();
            PushbackDirections.Add(NotAssigned);
        }

        protected virtual void OnSessionChange(bool state)
        {
            OnPropertyChanged(nameof(State));
            IsSessionInitialized = state;

            if (!IsSessionInitialized)
            {
                NotifyFlightPlan(false);
                ArrivalGates.Clear();
                GatesFiltered.Clear();
                ResetPushDirections();
                QueuedServices = NotAvailable;
                OnGateNameChanged("");
            }
            else
                OnPropertyChanged(nameof(SelectedPushDirection));

            OnPropertyChanged(nameof(AssignedArrivalGate));
            OnPropertyChanged(nameof(IsPushSelectable));
            OnPropertyChanged(nameof(IsGateSelectable));
            OnPropertyChanged(nameof(QueueHandler));
        }

        protected virtual Task OnAutomationStateChange(AutomationState state)
        {
            OnPropertyChanged(nameof(IsPushSelectable));
            OnPropertyChanged(nameof(IsGateSelectable));
            OnPropertyChanged(nameof(State));
            OnPropertyChanged(nameof(SchedOut));
            OnPropertyChanged(nameof(TimeToDepart));
            OnServiceQueueChanged(AutomationController.DepartureQueue.GetQueuedServicesTypes());

            if (state <= AutomationState.SessionStart)
                NotifyFlightPlan(false);
            if (state == AutomationState.TaxiOut)
                SelectedPushDirection = "";
            if (state == AutomationState.Arrival)
                AssignedArrivalGate = null;

            return Task.CompletedTask;
        }

        protected virtual Task OnGateNameChanged(string name)
        {
            name = GsxController.ApiData?.HandlerData?.gate?.uiGateName?.Trim();
            ActiveGate = !string.IsNullOrWhiteSpace(name) ? name : NotAvailable;
            ResetPushDirections();

            if (IsAtGate)
            {
                foreach (var direction in GsxController.ApiData?.HandlerData?.gate?.PushbackDirections ?? [])
                    PushbackDirections.Add(direction);
            }
            OnPropertyChanged(nameof(SelectedPushDirection));
            OnPropertyChanged(nameof(IsPushSelectable));
            OnPropertyChanged(nameof(State));

            return Task.CompletedTask;
        }

        protected virtual Task OnServiceQueueChanged(List<GsxServiceType> list)
        {
            if (!AppService.IsSessionInitialized || (AutomationController.State >= AutomationState.Pushback && AutomationController.State <= AutomationState.TaxiIn) || AutomationController.State <= AutomationState.Preparation)
                QueuedServices = NotAvailable;
            else if (list?.Count > 0)
            {
                List<string> names = [];
                foreach (var svc in list)
                {
                    if (ServiceDisplay.TryGetValue(svc, out var name))
                        names.Add(name);
                    else
                        names.Add(svc.ToString());
                }

                QueuedServices = string.Join(", ", names);
            }
            else
                QueuedServices = "";

            return Task.CompletedTask;
        }

        protected virtual Task OnExternalControlChanged()
        {
            OnPropertyChanged(nameof(IsPushSelectable));
            OnPropertyChanged(nameof(IsGateSelectable));
            OnPropertyChanged(nameof(QueueHandler));

            return Task.CompletedTask;
        }

        public virtual string QueueHandler
        {
            get
            {
                if (AutomationController?.ExternalServiceControl == true)
                    return AutomationController.DepartureQueue.ExternalServiceHandler;
                else
                    return "A2G";
            }
        }

        public virtual string State
        {
            get
            {
                if (!AppService.IsSessionInitialized || AutomationController.State < AutomationState.Preparation)
                    return "NOT AVAIL";
                else if (AutomationController.State == AutomationState.Preparation || AutomationController.State == AutomationState.TurnAround)
                    return "NO INIT";
                else if (AutomationController.State == AutomationState.Departure || AutomationController.State == AutomationState.Arrival)
                    return "GND HANDLING";
                else if (AutomationController.State == AutomationState.TaxiOut || AutomationController.State == AutomationState.TaxiIn)
                    return "TAXI";
                else if (AutomationController.State == AutomationState.Pushback)
                    return "PUSH";
                else if (AutomationController.State == AutomationState.Flight)
                    return "FLIGHT";
                else
                    return AutomationController.State.ToString().ToUpper();
            }
        }

        public virtual string SelectedPushDirection
        {
            get
            {
                string dir = GsxGate?.AutoSelectionPushback;
                if (string.IsNullOrWhiteSpace(dir) || !AppService.IsSessionInitialized)
                    return NotAssigned;
                else if (PushbackDirections.Contains(dir))
                    return dir;
                else
                {
                    PushbackDirections.Add(dir);
                    return dir;
                }
            }
            set
            {
                if (value?.Equals(NotAssigned, StringComparison.InvariantCultureIgnoreCase) == false)
                    GsxGate.SetPushDirection(value);
                else
                    GsxGate.SetPushDirection("");

                OnPropertyChanged();
            }
        }

        protected virtual Task OnAirportNameChanged(string name)
        {
            List<string> gates = GsxController.ApiData?.HandlerData?.airport?.GetParkings()?.Keys?.ToList() ?? [];

            ArrivalGates.Clear();
            GatesFiltered.Clear();
            foreach (var gate in gates)
            {
                ArrivalGates.Add(gate);
                GatesFiltered.Add(gate);
            }

            return Task.CompletedTask;
        }

        public virtual void SetGateFilter(string input)
        {
            var gates = ArrivalGates.Where(g => g.Contains(input ?? "", StringComparison.InvariantCultureIgnoreCase)).ToList();
            for (int i = GatesFiltered.Count - 1; i >= 0; i--)
            {
                var item = GatesFiltered[i];
                if (!gates.Contains(item))
                    GatesFiltered.Remove(item);
            }
            foreach (var item in gates)
            {
                if (!GatesFiltered.Contains(item))
                    GatesFiltered.Add(item);
            }
        }

        public virtual string AssignedArrivalGate
        {
            get
            {
                string gate = GsxGate?.AutoSelectionArrivalGate?.ToString() ?? NotAssigned;
                if (string.IsNullOrWhiteSpace(gate) || !AppService.IsSessionInitialized)
                    gate = NotAssigned;
                return gate;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Equals(NotAssigned, StringComparison.InvariantCultureIgnoreCase))
                    GsxGate.SetArrivalGate(null);
                else
                    GsxGate.SetArrivalGate(JsonValue.Create(value));

                OnPropertyChanged();
            }
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                NotifyFlightPlan(IsFlightplanLoaded);
        }

        protected virtual Task NotifyFlightPlan(bool state)
        {
            IsFlightplanLoaded = state;
            OnPropertyChanged(nameof(OfpId));
            OnPropertyChanged(nameof(FlightNumber));
            OnPropertyChanged(nameof(Date));
            OnPropertyChanged(nameof(Leg));
            OnPropertyChanged(nameof(CallSign));
            OnPropertyChanged(nameof(AircraftReg));
            OnPropertyChanged(nameof(AircraftType));
            OnPropertyChanged(nameof(SchedOut));
            OnPropertyChanged(nameof(TimeToDepart));
            OnPropertyChanged(nameof(PaxBag));
            OnPropertyChanged(nameof(PaxBagDiff));
            OnPropertyChanged(nameof(Payload));
            OnPropertyChanged(nameof(Fuel));
            OnPropertyChanged(nameof(Empty));
            OnPropertyChanged(nameof(ZeroFuel));
            OnPropertyChanged(nameof(WeightTotal));
            OnPropertyChanged(nameof(WeightPax));
            OnPropertyChanged(nameof(WeightBag));
            OnPropertyChanged(nameof(Freight));
            OnPropertyChanged(nameof(Cargo));
            OnPropertyChanged(nameof(PayloadDiff));

            return Task.CompletedTask;
        }

        protected virtual string FormatWeight(string property)
        {
            if (IsShowAllowed)
                return $"{Config.ConvertKgToDisplayUnit(Flightplan.GetPropertyValue<double>(property)).ToString("N0", CultureInfo.InstalledUICulture)} {Config.DisplayUnitCurrentString.ToUpper()}";
            else
                return $"0 {Config.DisplayUnitCurrentString.ToUpper()}";
        }

        protected virtual string FormatWeightAircraft(double weight)
        {
            return $"{Config.ConvertKgToDisplayUnit(weight).ToString("N0", CultureInfo.InstalledUICulture)} {Config.DisplayUnitCurrentString.ToUpper()}";
        }

        public virtual string OfpId => IsShowAllowed ? $"OFP #{Flightplan.Id}/{Flightplan.Edition}" : "";
        public virtual string Date => IsShowAllowed ? Flightplan?.ScheduledOutTime.ToString("ddMMMyy", CultureInfo.InvariantCulture)?.ToUpper() : "";
        public virtual string FlightNumber => IsShowAllowed ? Flightplan?.FlightNumber : "";
        public virtual string Leg => IsShowAllowed ? $"{Flightplan?.Origin}-{Flightplan?.Destination}" : "";
        public virtual string CallSign => IsShowAllowed ? Flightplan?.CallSign : "";
        public virtual string AircraftReg => IsShowAllowed ? Flightplan?.AircraftReg : "";
        public virtual string AircraftType => IsShowAllowed ? Flightplan?.AircraftType : "";
        public virtual string SchedOut => IsShowAllowed && AutomationController.State < AutomationState.TaxiOut ? Flightplan?.ScheduledOutTime.ToString("HH:mmZ", CultureInfo.InvariantCulture)?.ToUpper() : "";
        public virtual string TimeToDepart => IsShowAllowed && AutomationController.State < AutomationState.TaxiOut ? (Flightplan.ScheduledOutTime - GsxController.GetTime()).ToString(@"hh\:mm", CultureInfo.InvariantCulture) : "";
        public virtual string PaxBag => IsShowAllowed ? $"{Flightplan.CountPaxPlanned} / {Flightplan.CountBagsPlanned}" : "0 / 0";
        public virtual string PaxBagDiff => IsShowAllowed ? $"{Flightplan.DiffPax} / {Flightplan.DiffBags}" : "0 / 0";
        public virtual string Payload => FormatWeight(nameof(Flightplan.WeightPayloadKg));
        public virtual string Fuel => FormatWeight(nameof(Flightplan.FuelRampKg));
        public virtual string Empty => FormatWeight(nameof(Flightplan.AircraftEmptyOewKg));
        public virtual string ZeroFuel => FormatWeight(nameof(Flightplan.ZeroFuelRampKg));
        public virtual string WeightTotal => FormatWeight(nameof(Flightplan.WeightTotalRampKg));
        public virtual string WeightPax => FormatWeight(nameof(Flightplan.WeightPaxKg));
        public virtual string WeightBag => FormatWeight(nameof(Flightplan.WeightBagKg));
        public virtual string Freight => FormatWeight(nameof(Flightplan.WeightFreightKg));
        public virtual string Cargo => FormatWeight(nameof(Flightplan.WeightCargoKg));
        public virtual string PayloadDiff => FormatWeight(nameof(Flightplan.DiffPayloadKg));

        public virtual string LiveEmpty { get; protected set; }
        public virtual string LiveZeroFuel { get; protected set; }
        public virtual string LiveTotal { get; protected set; }
        public virtual string LiveFuel { get; protected set; }
    }
}
