using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Menu;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation.Phases
{
    public abstract class AutomationPhase(AutomationState phase)
    {
        public virtual AutomationState Phase { get; } = phase;
        public virtual bool NeedsFlightplan => false;
        public virtual bool NeedsDepartureQueue => false;
        public virtual bool NeedsEquipPlaced => false;
        public virtual bool NeedsEquipRemoved => false;

        protected virtual AppService AppService => AppService.Instance;
        protected virtual Config Config => AppService.Config;
        protected virtual SettingProfile Profile => AppService.SettingProfile;
        protected virtual CancellationToken RequestToken => AppService.RequestToken;
        protected virtual GsxController GsxController => AppService.GsxController;
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
        protected virtual GsxMenu GsxMenu => GsxController.Menu;
        protected virtual GsxGate GsxGate => GsxController.Gate;
        protected virtual GsxAutomationController AutomationController => GsxController.AutomationController;
        protected virtual AircraftController AircraftController => GsxController.AircraftController;
        public virtual AircraftBase Aircraft => AircraftController?.Aircraft;
        protected virtual Flightplan Flightplan => AppService.Flightplan;
        protected virtual AircraftState AircraftState => AutomationController.AircraftState;
        protected virtual GroundEquipManager EquipManager => AutomationController.EquipManager;
        protected virtual DepartureServiceQueue DepartureQueue => AutomationController.DepartureQueue;
        protected virtual StateStore Store => GsxController.StateStore;
        protected virtual NotificationManager NotificationManager => AppService.NotificationManager;
        protected virtual NotificationTracker Tracker => AppService.NotificationTracker;
        protected virtual bool IsOnGround => GsxController.IsOnGround;
        protected virtual bool IsWalkaround => GsxController.IsWalkaround || !GsxController.IsSessionReady;
        protected virtual bool AutomationStateValid => AutomationController.AutomationStateValid;
        protected virtual bool DepartureServicesRunning => ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning || ServiceCleaning.IsRunning || ServiceLavatory.IsRunning || ServiceWater.IsRunning;

        public abstract Task InitStore();

        public abstract Task<AutomationState> Evaluate();

        public abstract Task Run();

        public virtual async Task SetPhase()
        {
            if (NeedsFlightplan && !Flightplan.IsLoaded)
            {
                await Flightplan.Import();
                await Task.Delay(Config.StateMachineInterval / 2, RequestToken);
                await AircraftState.Refresh();
            }

            if (Flightplan.IsLoaded && Tracker.IsActive(AppNotification.OfpCheck))
                Tracker.Clear(AppNotification.OfpCheck);

            if (NeedsDepartureQueue && Store[StateVar.ServiceQueueInit] == false)
            {
                DepartureQueue.BuildQueue();
                Store[StateVar.ServiceQueueInit] = true;
                await Task.Delay(Config.StateMachineInterval / 2, RequestToken);
                await AircraftState.Refresh();
            }

            if (NeedsEquipPlaced)
            {
                await EquipManager.PlaceGroundEquipment(Phase.ToString());
                await Task.Delay(Config.StateMachineInterval / 2, RequestToken);
                await AircraftState.Refresh();
            }

            if (NeedsEquipRemoved)
            {
                await EquipManager.RemoveGroundEquip(Phase.ToString());
                await Task.Delay(Config.StateMachineInterval / 2, RequestToken);
                await AircraftState.Refresh();
            }
        }

        public static void LogReason(AutomationState newState, string reason, string debug = null)
        {
            Logger.Information($"Switching to Phase {newState} due to {reason}");
            if (!string.IsNullOrWhiteSpace(debug))
                Logger.Debug($"Phase {newState}: {debug}");
        }

        public static AutomationPhase Create(AutomationState state)
        {
            return state switch
            {
                AutomationState.SessionStart => new PhaseSessionStart(),
                AutomationState.Preparation => new PhasePreparation(),
                AutomationState.Departure => new PhaseDeparture(),
                AutomationState.Pushback => new PhasePushback(),
                AutomationState.TaxiOut => new PhaseTaxiOut(),
                AutomationState.Flight => new PhaseFlight(),
                AutomationState.TaxiIn => new PhaseTaxiIn(),
                AutomationState.Arrival => new PhaseArrival(),
                AutomationState.TurnAround => new PhaseTurnaround(),
                _ => new PhaseUnknown()
            };
        }
    }
}
