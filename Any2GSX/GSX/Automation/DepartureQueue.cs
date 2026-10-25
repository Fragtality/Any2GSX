using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Services;
using Any2GSX.Notifications;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using CFIT.AppTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Any2GSX.GSX.Automation
{
    public class DepartureServiceQueue
    {
        protected virtual Config Config => AppService.Instance?.Config;
        protected virtual SettingProfile Profile => AppService.Instance?.SettingProfile;
        protected virtual GsxController GsxController => AppService.Instance?.GsxController;
        protected virtual GsxAutomationController AutomationController => GsxController?.AutomationController;
        protected virtual AircraftController AircraftController => AppService.Instance?.AircraftController;
        protected virtual AircraftBase Aircraft => AircraftController?.Aircraft;
        protected virtual Flightplan Flightplan => AppService.Instance?.Flightplan;
        public static string TextExternal => "EXT";
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController?.GsxServices;
        protected virtual ConcurrentDictionary<int, DepartureService> InternalServiceQueue { get; } = [];
        public virtual bool ExternalServiceControl { get; protected set; } = false;
        public virtual string ExternalServiceHandler { get; protected set; } = TextExternal;
        protected virtual ConcurrentDictionary<int, DepartureService> ExternalServiceQueue { get; } = [];
        protected virtual ConcurrentDictionary<int, DepartureService> ServicesQueued => ExternalServiceControl ? ExternalServiceQueue : InternalServiceQueue;
        public virtual bool HasServiceQueued => !ServicesQueued.IsEmpty;
        protected virtual int? NextIndex { get; set; } = null;
        protected virtual int MaxIndex { get; set; } = 0;
        public virtual DepartureService Next => NextIndex != null && ServicesQueued.TryGetValue((int)NextIndex, out var service) ? service : null;
        public virtual DepartureService NextQueued => ServicesQueued.IsEmpty ? null : ServicesQueued.First().Value;
        public virtual int? NextQueuedIndex => ServicesQueued.IsEmpty ? null : ServicesQueued.First().Key;
        public virtual ServiceConfig NextConfig => Next?.Config ?? null;
        public virtual GsxService NextService => Next?.Service ?? null;
        public virtual GsxServiceType NextType => NextService?.Type ?? GsxServiceType.Unknown;
        public virtual bool HasNextQueued => NextQueued != null;
        public virtual bool HasNext => Next != null;
        protected virtual ConcurrentDictionary<int, DepartureService> ServicesCalled { get; } = [];
        protected virtual ConcurrentDictionary<int, DepartureService> ServicesSkipped { get; } = [];
        public virtual DepartureService LastCalled => ServicesCalled.IsEmpty ? null : ServicesCalled?.Last().Value ?? null;
        protected virtual DateTime LastCallTime { get; set; } = DateTime.MinValue;
        public virtual bool HasLastService => LastCalled != null;
        public virtual bool ServicesCompleted => !ExternalServiceControl && !ServicesCalled.IsEmpty && HasAllCalled(s => s.Service.IsCompleted) && ServicesQueued.IsEmpty;
        public virtual int CountTotal => ServicesQueued.Count + ServicesCalled.Count;
        public virtual int CountRunning => ServicesCalled.Count(s => s.Value.Service.IsRunning);
        public virtual int CountSkipped => ServicesSkipped.Count;
        public virtual int CountCompleted => ServicesCalled.Count(s => s.Value.Service.IsCompleted);
        public virtual string ErrorMessage { get; protected set; } = "";
        public virtual string LastErrorMessage { get; protected set; } = "";

        public event Func<Task> ExternalControlChanged;
        public event Func<List<GsxServiceType>, Task> ServiceQueueChanged;

        public virtual bool HasQueued(Func<DepartureService, bool> func)
        {
            return ServicesQueued.Any(kv => func(kv.Value));
        }

        public virtual bool HasAllQueued(Func<DepartureService, bool> func)
        {
            return ServicesQueued.All(kv => func(kv.Value));
        }

        public virtual bool HasCalled(Func<DepartureService, bool> func)
        {
            return ServicesCalled.Any(kv => func(kv.Value));
        }

        public virtual IDictionary<int, DepartureService> SelectCalled(Func<DepartureService, bool> func)
        {
            return ServicesCalled.Where(kv => func(kv.Value)).ToDictionary();
        }

        public virtual bool HasLastCalled(Func<DepartureService, bool> func)
        {
            return LastCalled != null && func(LastCalled);
        }

        public virtual bool HasAllCalled(Func<DepartureService, bool> func)
        {
            return ServicesCalled.All(kv => func(kv.Value));
        }

        public virtual void Reset()
        {
            ExternalServiceQueue.Clear();
            BuildQueue();
            ResetActivations();
        }

        public virtual void CountActivations()
        {
            if (Profile?.DepartureServices != null)
            {
                foreach (var activation in Profile.DepartureServices.Values)
                    activation.TurnCount++;
            }
            BuildQueue();
        }

        public virtual void BuildQueue()
        {
            InternalServiceQueue.Clear();
            ServicesCalled.Clear();
            ServicesSkipped.Clear();
            MaxIndex = 0;
            ErrorMessage = "";
            LastErrorMessage = "";
            LastCallTime = DateTime.MinValue;

            if (Profile?.DepartureServices != null && !GsxController.GsxServices.IsEmpty)
            {
                foreach (var config in Profile.DepartureServices.Values)
                {
                    if (!config.IsSkipped)
                        InternalServiceQueue.Add(MaxIndex++, new(GsxController.GsxServices[config.ServiceType], config));
                }
            }

            NotifyQueueChange();
        }

        public virtual async Task<bool> EvaluateNextService()
        {
            ErrorMessage = "";
            foreach (var service in ServicesQueued)
            {
                if (service.Value.Config.ServiceActivation != GsxServiceActivation.Skip)
                    await ApplyServiceConstraints(service.Key, service.Value);
            }

            int next = -1;
            foreach (var service in ServicesQueued)
            {
                if (CheckActivation(service.Key, service.Value))
                {
                    next = service.Key;
                    break;
                }
            }

            if (ServicesQueued.TryGetValue(next, out DepartureService nextService) && nextService.Config.HasCallDelay && LastCallTime + nextService.Config.CallDelay > DateTime.Now)
                next = -1;

            if (!string.IsNullOrWhiteSpace(ErrorMessage) && LastErrorMessage != ErrorMessage)
                Logger.Debug($"Queue Evaluation: {ErrorMessage}");
            LastErrorMessage = ErrorMessage;

            if (next >= 0)
            {
                NextIndex = next;
                return true;
            }
            else
            {
                NextIndex = null;
                return false;
            }
        }

        protected virtual bool CheckActivation(int index, DepartureService service)
        {
            bool isCallable = false;
            switch (service.Config.ServiceActivation)
            {
                case GsxServiceActivation.Independent:
                    isCallable = true;
                    break;
                case GsxServiceActivation.DepartureTime:
                    isCallable = service.Config.MaxTimeBeforeDeparture >= Flightplan.ScheduledOutTime - GsxController.GetTime();
                    break;
                case GsxServiceActivation.ServiceDependent:
                    isCallable = CheckServiceDependency(index, service);
                    break;
                default:
                    break;
            }

            isCallable = CheckConflicts(isCallable, service);

            return isCallable;
        }

        protected virtual bool CheckConflicts(bool isCallable, DepartureService service)
        {
            if ((service.Service.Type == GsxServiceType.Cleaning || service.Service.Type == GsxServiceType.Catering || service.Service.Type == GsxServiceType.Boarding) && (GsxController.ServiceDeboard.IsRunning || GsxController.ServiceBoard.IsRunning))
            {
                if (isCallable)
                    ErrorMessage = $"Service {service.Service.Type} not allowed while Board or Deboard is running";
                isCallable = false;
            }

            if (service.Service.HasStairConflict && (HasCalled(s => s.Service.IsRequested && s.Service.RequiresStair) || (GsxController.ServiceDeboard.IsRunning && !GsxController.HasGateJetway)))
            {
                if (isCallable)
                    ErrorMessage = $"Service {service.Service.Type} has Stair Conflict and running Services require Stairs";
                isCallable = false;
            }

            if (service.Service.RequiresStair && HasCalled(s => s.Service.IsRequested && s.Service.HasStairConflict) && (!GsxController.ServiceStairs.IsConnected || GsxController.ServiceStairs.IsOperating || GsxController.StateStore[StateVar.WasStairRemoved] == true || GsxController.Tracker.IsActive(AppNotification.UpdatesBlocked)))
            {
                if (isCallable)
                    ErrorMessage = $"Service {service.Service.Type} requires Stair but running Services have Stair Conflict";
                isCallable = false;
            }

            return isCallable;
        }

        protected virtual bool CheckServiceDependency(int index, DepartureService service)
        {
            var dependency = service.Config.ServiceDependency;
            return index == 0
                    || (dependency == GsxServiceDependency.Requested && CheckServiceDependencyState(index, GsxServiceState.Requested, service))
                    || (dependency == GsxServiceDependency.Active && CheckServiceDependencyState(index, GsxServiceState.Active, service))
                    || (dependency == GsxServiceDependency.Completed && CheckServiceDependencyState(index, GsxServiceState.Completed, service));
        }

        protected virtual bool CheckServiceDependencyState(int index, GsxServiceState minState, DepartureService service)
        {
            return service.Config.ServiceDependencyScope switch
            {
                GsxServiceDependencyScope.LastCalled => HasLastCalled(s => s.Service.State >= minState || s.Service.IsCompleted),
                GsxServiceDependencyScope.PreviousQueued => ServicesSkipped.ContainsKey(index - 1) || ServicesCalled.Any(kv => kv.Key == index - 1 && (kv.Value.Service.State >= minState || kv.Value.Service.IsCompleted)),
                GsxServiceDependencyScope.AllCalled => !ServicesCalled.IsEmpty && HasAllCalled(s => s.Service.State >= minState || s.Service.IsCompleted),
                GsxServiceDependencyScope.AllQueued => !ServicesCalled.IsEmpty && ServicesCalled.Where(kv => kv.Key < index).All(kv => kv.Value.Service.State >= minState || kv.Value.Service.IsCompleted),
                _ => false
            };
        }

        protected virtual bool CheckServiceCancelDependency(int index, DepartureService service)
        {
            var dependency = service.Config.ServiceCancelDependency;
            return (dependency == GsxServiceDependency.Requested && CheckServiceCancelDependencyState(index, GsxServiceState.Requested, service))
                    || (dependency == GsxServiceDependency.Active && CheckServiceCancelDependencyState(index, GsxServiceState.Active, service))
                    || (dependency == GsxServiceDependency.Completed && CheckServiceCancelDependencyState(index, GsxServiceState.Completed, service));
        }

        protected virtual bool CheckServiceCancelDependencyState(int index, GsxServiceState minState, DepartureService service)
        {
            return service.Config.ServiceCancelDependencyScope switch
            {
                GsxServiceDependencyScope.LastCalled => HasLastCalled(s => s.Service.State >= minState && s.Service.Type != service.Service.Type),
                GsxServiceDependencyScope.PreviousQueued => ServicesSkipped.ContainsKey(index - 1) || ServicesCalled.Any(kv => kv.Key == index - 1 && (kv.Value.Service.State >= minState || kv.Value.Service.IsCompleted)),
                GsxServiceDependencyScope.AllCalled => !ServicesCalled.IsEmpty && HasAllCalled(s => (s.Service.State >= minState && s.Service.Type != service.Service.Type) || s.Service.Type == service.Service.Type),
                GsxServiceDependencyScope.AllQueued => !ServicesCalled.IsEmpty && ServicesCalled.Where(kv => kv.Key < index).All(kv => kv.Value.Service.State >= minState || kv.Value.Service.IsCompleted || kv.Value.Service.Type == service.Service.Type),
                _ => false
            };
        }

        protected virtual bool CheckCancellation(int index, DepartureService service)
        {
            bool isCancelable = false;
            switch (service.Config.ServiceCancellation)
            {
                case GsxServiceActivation.DepartureTime:
                    isCancelable = service.Service.IsRunning && !service.Service.IsCompleted && service.Config.HasMaxRunTime && service.Service.ActivationTime != DateTime.MaxValue && DateTime.Now - service.Service.ActivationTime >= service.Config.MaxRunTime;
                    break;
                case GsxServiceActivation.ServiceDependent:
                    isCancelable = service.Service.IsActive && CheckServiceCancelDependency(index, service) && (service.Config.MinRunTime == TimeSpan.Zero || (service.Service.ActivationTime != DateTime.MaxValue && DateTime.Now - service.Service.ActivationTime >= service.Config.MinRunTime));
                    break;
                default:
                    break;
            }

            return isCancelable;
        }

        public virtual async Task ApplyCancellations()
        {
            foreach (var service in ServicesCalled)
            {
                if (CheckCancellation(service.Key, service.Value) && !service.Value.Service.WasCanceled)
                {
                    Logger.Information($"Automation: Cancel Service {service.Value.Service.Type} due to {(service.Value.Config.HasMaxRunTime ? "Max Run Time" : "Service Dependency")}");
                    await service.Value.Service.Cancel();
                }
            }
        }

        protected virtual async Task ApplyServiceConstraints(int index, DepartureService queueItem)
        {
            ServiceConfig config = queueItem.Config;
            GsxService service = queueItem.Service;

            bool move = false;
            bool asSkipped = false;
            string reason = "";

            if (service.IsCalled || service.IsRunning)
            {
                move = true;
                reason = $"Service State '{service.TextState}'";
            }
            else if (service.IsBypassed)
            {
                asSkipped = true;
                reason = $"Service State '{service.TextState}'";
            }
            else if (service.IsCompleted || service.IsCompleting)
            {
                move = true;
                reason = "Service Completed";

            }
            else if (!config.CallOnCargo && AutomationController.AircraftState.IsCargo)
            {
                asSkipped = true;
                reason = "Constraint 'Cargo'";
            }
            else if (config.HasDurationConstraint && Flightplan.Duration < config.MinimumFlightDuration)
            {
                asSkipped = true;
                reason = "Constraint 'Min. Flight Time'";
            }
            else if (config.TurnCount > 0 && config.OnlyDeparture)
            {
                asSkipped = true;
                reason = $"Constraint 'Only Departure'";
            }
            else if (config.TurnCount == 0 && config.OnlyTurn)
            {
                asSkipped = true;
                reason = $"Constraint 'Only Turn'";
            }
            else if (config.OnlyHub && !Profile.IsCompanyHub(Flightplan.Origin))
            {
                asSkipped = true;
                reason = $"Constraint 'Only Hub'";
            }
            else if (config.OnlyNonHub && Profile.IsCompanyHub(Flightplan.Origin))
            {
                asSkipped = true;
                reason = $"Constraint 'Only Non-Hub'";
            }
            else if (config.OnlyPreferredOp && GsxController.Menu.OperatorSelectionState[GsxMenuCommandOperatorType.Handling] && GsxController.StateStore[StateVar.WasOperatorPreferred] == false)
            {
                asSkipped = true;
                reason = $"Constraint 'Preferred Operator'";
            }
            else if (Profile.SkipFuelOnTankering && service.Type == GsxServiceType.Refuel && Flightplan.IsLoaded && AutomationController.AircraftState.FuelOnBoard >= Flightplan.FuelRampKg - Config.FuelCompareVariance)
            {
                asSkipped = true;
                reason = "FOB is greater than planned";
            }
            else if (service.RequiresStair && !GsxController.HasGateStair)
            {
                asSkipped = true;
                reason = "Gate has no Stairs";
            }

            if (move || asSkipped)
            {
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    if (asSkipped)
                        Logger.Information($"Automation: Departure Service {service.Type} skipped because: {reason}");
                    else
                        Logger.Information($"Automation: Departure Service {service.Type} moved to Called Services: {reason}");
                }
                MoveQueue(index, asSkipped);
            }
        }

        public virtual void ResetActivations()
        {
            if (Profile?.DepartureServices != null)
            {
                foreach (var activation in Profile.DepartureServices.Values)
                    activation.TurnCount = 0;
            }
        }

        public virtual void FinishServices()
        {
            while (!ServicesQueued.IsEmpty)
            {
                var service = ServicesQueued.First();
                MoveQueue(service.Key, true);
            }
        }

        protected virtual void MoveQueue(int index, bool asSkipped = false)
        {
            if (ServicesQueued.TryGetValue(index, out DepartureService service))
            {
                ServicesQueued.Remove(index);

                if (asSkipped)
                    ServicesSkipped.Add(index, service);
                else
                {
                    LastCallTime = DateTime.Now;
                    ServicesCalled.Add(index, service);
                }

                NotifyQueueChange();
            }
        }

        public virtual Task CancelRunningServices(string reason)
        {
            return CancelCalledServices(s => s.Service.IsRunning, reason);
        }

        public virtual Task<bool> CancelService(GsxServiceType type, string reason)
        {
            return CancelCalledServices(s => s.Service.Type == type, reason);
        }

        protected virtual async Task<bool> CancelCalledServices(Func<DepartureService, bool> cancelCheck, string reason)
        {
            bool hasFailed = false;
            foreach (var service in ServicesCalled.Values)
            {
                if (cancelCheck(service))
                {
                    Logger.Information($"Automation: Cancel Service {service.Service.Type} due to {reason}");
                    if (await service.Service.Cancel(GsxCancelService.Complete) == false)
                        hasFailed = true;
                }
            }

            return !hasFailed;
        }

        public virtual async Task<bool> CallNext(string message = "")
        {
            if (!HasNext)
                return false;

            Logger.Information($"Automation: Call Departure Service {NextType}{(!string.IsNullOrWhiteSpace(message) ? " " : "")}{message}");
            await NextService.Call();

            if (NextService.IsCalled || NextService.IsCompleted)
            {
                MoveQueue((int)NextIndex);
                return true;
            }
            else
                return false;
        }

        public virtual async Task<bool> CallNextQueued()
        {
            if (!HasNextQueued)
                return false;

            await NextQueued.Service.Call();

            if (NextQueued.Service.IsCalled || NextQueued.Service.IsCompleted)
            {
                MoveQueue((int)NextQueuedIndex);
                return true;
            }
            else
                return false;
        }

        protected virtual void SetExternalState(bool state)
        {
            bool notify = ExternalServiceControl != state;
            if (notify && state)
            {
                ExternalServiceQueue.Clear();
                MaxIndex = 0;
            }
            ExternalServiceControl = state;

            if (notify)
            {
                Logger.Information($"External Service Control is set to: {(state ? "On" : "Off")}");
                _ = TaskTools.RunPool(() => ExternalControlChanged?.Invoke(), GsxController.RequestToken);
                NotifyQueueChange();
            }
        }

        public virtual void SetExternalControl(bool state, string handler = null)
        {
            SetExternalState(state);

            if (!string.IsNullOrWhiteSpace(handler))
                ExternalServiceHandler = handler;
            else
                ExternalServiceHandler = TextExternal;

            if (!ExternalServiceControl)
            {
                ExternalServiceQueue.Clear();
                BuildQueue();
            }
        }

        public virtual bool QueueExternalService(ServiceConfig serviceConfig)
        {
            if (serviceConfig != null && !serviceConfig.IsSkipped && serviceConfig.ServiceType >= GsxServiceType.Refuel && serviceConfig.ServiceType != GsxServiceType.Jetway && serviceConfig.ServiceType != GsxServiceType.Stairs && serviceConfig.ServiceType != GsxServiceType.Pushback
                && !ExternalServiceQueue.Any(svc => svc.Value.Service.Type == serviceConfig.ServiceType) && GsxController.GsxServices.TryGetValue(serviceConfig.ServiceType, out GsxService? gsxService))
            {
                ExternalServiceQueue.Add(MaxIndex++, new(gsxService, serviceConfig));
                NotifyQueueChange();
                return true;
            }
            else
                return false;
        }

        protected virtual void NotifyQueueChange()
        {
            _ = TaskTools.RunPool(() => ServiceQueueChanged?.Invoke(GetQueuedServicesTypes()), GsxController.RequestToken);
        }

        public virtual List<GsxServiceType> GetQueuedServicesTypes()
        {
            return ServicesQueued?.Values?.Select(s => s.Service.Type)?.ToList() ?? [];
        }

        public virtual List<string> GetQueuedServicesTypeNames()
        {
            return ServicesQueued?.Values?.Select(s => s.Service.Type.ToString())?.ToList() ?? [];
        }

        public virtual List<string> GetCalledServicesTypeNames()
        {
            return ServicesCalled?.Values?.Select(s => s.Service.Type.ToString())?.ToList() ?? [];
        }

        public virtual List<string> GetSkippedServicesTypeNames()
        {
            return ServicesSkipped?.Values?.Select(s => s.Service.Type.ToString())?.ToList() ?? [];
        }
    }

    public class DepartureService(GsxService service, ServiceConfig config)
    {
        public virtual GsxService Service { get; } = service;
        public virtual ServiceConfig Config { get; } = config;
    }
}
