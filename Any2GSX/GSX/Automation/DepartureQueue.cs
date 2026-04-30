using Any2GSX.Aircraft;
using Any2GSX.AppConfig;
using Any2GSX.GSX.Services;
using Any2GSX.PluginInterface;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
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
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController?.GsxServices;
        protected virtual Queue<DepartureService> ServiceQueue { get; } = [];
        public virtual DepartureService Next => ServiceQueue?.TryPeek(out DepartureService service) == true ? service : null;
        public virtual ServiceConfig NextConfig => Next?.Config ?? null;
        public virtual GsxService NextService => Next?.Service ?? null;
        public virtual GsxServiceType NextType => NextService?.Type ?? GsxServiceType.Unknown;
        public virtual bool HasNext => Next != null;
        protected virtual bool IsFirst => !ServicesCalled.Any(s => !s.Service.IsSkipped);
        protected virtual Queue<DepartureService> ServicesCalled { get; } = [];
        public virtual DepartureService LastCalled => ServicesCalled?.LastOrDefault((s) => !s.Service.IsSkipped) ?? null;
        protected virtual DateTime LastCallTime { get; set; } = DateTime.MinValue;
        public virtual bool HasLastService => LastCalled != null;
        public virtual bool ServicesCompleted => !HasNext && AllCalledCompleted();
        public virtual int CountTotal => ServiceQueue.Count(s => s.Config.ServiceActivation != GsxServiceActivation.Skip) + ServicesCalled.Count(s => s.Config.ServiceActivation != GsxServiceActivation.Skip);
        public virtual int CountRunning => ServicesCalled.Count(s => s.Service.IsRunning);
        public virtual int CountCompleted => ServicesCalled.Count(s => s.Service.IsCompleted || s.Service.IsSkipped);

        public virtual DepartureService GetQueuedService(GsxServiceType serviceType)
        {
            return ServiceQueue.FirstOrDefault(s => s.Service.Type == serviceType);
        }

        public virtual bool IsQueued(GsxServiceType type)
        {
            return ServiceQueue.Any(s => s.Service.Type == type && s.Config.ServiceActivation > GsxServiceActivation.Skip);
        }

        public virtual bool AllCalledCompleted()
        {
            return ServicesCalled.All(s => s.Service.IsCompleted || s.Service.IsSkipped || s.Service.State == GsxServiceState.Callable || s.Service.State == GsxServiceState.Bypassed);
        }

        public virtual bool HasCalledRunning()
        {
            return ServicesCalled.Any(s => s.Service.IsRunning);
        }

        public virtual bool HasCalledRunning(GsxServiceType serviceType)
        {
            return ServicesCalled.Any(s => s.Service.Type == serviceType && s.Service.IsRunning);
        }

        public virtual bool HasCalled(Func<DepartureService, bool> func)
        {
            return ServicesCalled.Any(func);
        }

        public virtual void Reset()
        {
            BuildQueue();
            ResetActivations();
        }

        public virtual void ResetFlight()
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
            ServiceQueue.Clear();
            ServicesCalled.Clear();
            LastCallTime = DateTime.MinValue;

            if (Profile?.DepartureServices != null)
            {
                foreach (var config in Profile.DepartureServices.Values)
                    ServiceQueue.Enqueue(new(GsxController.GsxServices[config.ServiceType], config));
            }
        }

        public virtual async Task CheckQueueSkips()
        {
            GsxServiceType lastType = GsxServiceType.Unknown;
            while (ServiceQueue.TryPeek(out _) && lastType != NextType)
            {
                lastType = NextType;
                await ApplyServiceSkips();
            }
        }

        protected virtual void ResetActivations()
        {
            if (Profile?.DepartureServices != null)
            {
                foreach (var activation in Profile.DepartureServices.Values)
                    activation.TurnCount = 0;
            }
        }

        public virtual void FinishServices()
        {
            while (ServiceQueue.TryPeek(out _))
                MoveQueue(true);

            foreach (var called in ServicesCalled)
            {
                if (!called.Service.IsSkipped)
                    called.Service.IsSkipped = true;
            }
        }

        protected virtual void MoveQueue(bool asSkipped = false)
        {
            if (HasNext)
            {
                var service = ServiceQueue.Dequeue();
                if (asSkipped)
                    service.Service.IsSkipped = true;
                LastCallTime = DateTime.Now;
                ServicesCalled.Enqueue(service);
            }
        }

        public virtual Task CancelRunningServices(string reason)
        {
            return CancelCalledServices(s => s.Service.IsRunning, reason);
        }

        protected virtual async Task CancelCalledServices(Func<DepartureService, bool> cancelCheck, string reason)
        {
            foreach (var service in ServicesCalled)
            {
                if (cancelCheck(service))
                {
                    Logger.Information($"Automation: Cancel Service {service.Service.Type} due to {reason}");
                    await service.Service.Cancel(GsxCancelService.Complete);
                }
            }
        }

        public virtual Task ApplyRunTime()
        {
            return CancelCalledServices(s => s.Service.IsRunning && s.Config.HasMaxRunTime && s.Service.ActivationTime != DateTime.MaxValue && DateTime.Now - s.Service.ActivationTime >= s.Config.MaxRunTime, "Run Time Constraint");
        }

        public virtual async Task ApplyServiceSkips()
        {
            if (!HasNext)
                return;

            bool move = false;
            bool asSkipped = false;
            string reason = "";

            if (NextConfig.ServiceActivation == GsxServiceActivation.Skip)
            {
                asSkipped = true;
                reason = "Set to Skip/Ignore";
            }
            else if (NextService.IsCalled || NextService.IsRunning)
            {
                move = true;
                if (AutomationController.HasFuelSync && NextType == GsxServiceType.Refuel
                    && !AircraftController.RefuelTimer.IsEnabled && NextService.IsActive && GsxController.ServiceRefuel.IsHoseConnected)
                {
                    Logger.Debug($"Starting Refuel Sync for already running GSX Service");
                    await AircraftController.OnRefuelHoseChanged(true);
                }
            }
            else if (NextService.State == GsxServiceState.NotAvailable || NextService.State == GsxServiceState.Bypassed)
            {
                asSkipped = true;
                reason = $"Service State '{NextService.TextState}'";
            }
            else if (NextService.IsCompleted || NextService.IsCompleting)
            {
                move = true;
                reason = "Service Completed";

            }
            else if (!NextConfig.CallOnCargo && AutomationController.IsCargo)
            {
                asSkipped = true;
                reason = "not enabled for Cargo Airplane";
            }
            else if (NextConfig.HasDurationConstraint && Flightplan.Duration < NextConfig.MinimumFlightDuration)
            {
                asSkipped = true;
                reason = "Min. Flight Time";
            }
            else if (NextConfig.TurnCount > 0 && NextConfig.ServiceConstraint == GsxServiceConstraint.FirstLeg)
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.TurnCount == 0 && NextConfig.ServiceConstraint == GsxServiceConstraint.TurnAround)
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.ServiceConstraint == GsxServiceConstraint.CompanyHub && !Profile.IsCompanyHub(Flightplan.Origin))
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.ServiceConstraint == GsxServiceConstraint.NonCompanyHub && Profile.IsCompanyHub(Flightplan.Origin))
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.ServiceConstraint == GsxServiceConstraint.TurnOnHub && (NextConfig.TurnCount == 0 || !Profile.IsCompanyHub(Flightplan.Origin)))
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.ServiceConstraint == GsxServiceConstraint.TurnOnNonHub && (NextConfig.TurnCount == 0 || Profile.IsCompanyHub(Flightplan.Origin)))
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (NextConfig.ServiceConstraint == GsxServiceConstraint.PreferredOp && GsxController.Menu.WasOperatorHandlingSelected && !GsxController.Menu.WasOperatorPreferred)
            {
                asSkipped = true;
                reason = $"Constraint '{NextConfig.ServiceConstraintName}'";
            }
            else if (Profile.SkipFuelOnTankering && NextType == GsxServiceType.Refuel && Flightplan.IsLoaded && AutomationController.FuelOnBoard >= Flightplan.FuelRampKg - Config.FuelCompareVariance)
            {
                asSkipped = true;
                reason = "FOB is greater than planned";
            }

            if (move || asSkipped)
            {
                if (!string.IsNullOrWhiteSpace(reason))
                    Logger.Information($"Automation: Departure Service {NextType} {(asSkipped ? "skipped" : "bypassed")} because: {reason}");
                MoveQueue(asSkipped);
            }
        }

        public virtual bool IsNextCallable()
        {
            if (!HasNext)
                return false;

            var activation = NextConfig.ServiceActivation;

            return ((IsFirst && activation != GsxServiceActivation.Skip) || activation == GsxServiceActivation.AfterCalled
                || (activation == GsxServiceActivation.AfterRequested && LastCalled?.Service?.State >= GsxServiceState.Requested)
                || (activation == GsxServiceActivation.AfterActive && LastCalled?.Service?.State >= GsxServiceState.Active)
                || (activation == GsxServiceActivation.AfterPrevCompleted && LastCalled?.Service?.IsCompleted == true)
                || (activation == GsxServiceActivation.AfterAllCompleted && AllCalledCompleted()))
                && (!NextConfig.HasTobtConstraint || NextConfig.MaxTimeBeforeDeparture >= Flightplan.ScheduledOutTime - GsxController.GetTime())
                && (!NextConfig.HasCallDelay || LastCallTime + NextConfig.CallDelay <= DateTime.Now);
        }

        public virtual async Task<bool> CallNext(string message = "")
        {
            Logger.Information($"Automation: Call Departure Service {NextType}{(!string.IsNullOrWhiteSpace(message) ? " " : "")}{message}");
            await NextService.Call();

            if (NextService.IsCalled)
            {
                MoveQueue();
                return true;
            }
            else
                return false;
        }
    }

    public class DepartureService(GsxService service, ServiceConfig config)
    {
        public virtual GsxService Service { get; } = service;
        public virtual ServiceConfig Config { get; } = config;
    }
}
