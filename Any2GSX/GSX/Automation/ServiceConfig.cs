using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Any2GSX.GSX.Automation
{
    public class ServiceConfig
    {
        [JsonIgnore]
        public static Dictionary<GsxServiceActivation, string> TextServiceActivations { get; } = new()
        {
            { GsxServiceActivation.Skip, "Never (Skip / Ignore)" },
            { GsxServiceActivation.Manual, "Manual: by User / Addon" },
            { GsxServiceActivation.Independent, "Auto: Independent" },
            { GsxServiceActivation.DepartureTime, "Auto: Time to Departure" },
            { GsxServiceActivation.ServiceDependent, "Auto: Dependent on Services" },
        };

        [JsonIgnore]
        public static Dictionary<GsxServiceDependency, string> TextServiceDependencies { get; } = new()
        {
            { GsxServiceDependency.Requested, "Requested" },
            { GsxServiceDependency.Active, "Active" },
            { GsxServiceDependency.Completed, "Completed" },
        };

        [JsonIgnore]
        public static Dictionary<GsxServiceDependencyScope, string> TextServiceDependencyScope { get; } = new()
        {
            { GsxServiceDependencyScope.LastCalled, "Last called" },
            { GsxServiceDependencyScope.AllCalled, "All called" },
            { GsxServiceDependencyScope.PreviousQueued, "Previous queued" },
            { GsxServiceDependencyScope.AllQueued, "All queued" },
        };

        [JsonIgnore]
        public static Dictionary<GsxServiceActivation, string> TextServiceCancellations { get; } = new()
        {
            { GsxServiceActivation.Skip, "Never" },
            { GsxServiceActivation.DepartureTime, "Max Run Time" },
            { GsxServiceActivation.ServiceDependent, "Dependent on Services" },
        };

        public virtual GsxServiceType ServiceType { get; set; } = GsxServiceType.Unknown;
        public virtual GsxServiceActivation ServiceActivation { get; set; } = GsxServiceActivation.ServiceDependent;
        [JsonIgnore]
        public virtual string ServiceActivationName => TextServiceActivations[ServiceActivation];
        [JsonIgnore]
        public virtual bool IsSkipped => ServiceActivation == GsxServiceActivation.Skip;
        public virtual GsxServiceDependency ServiceDependency { get; set; } = GsxServiceDependency.Requested;
        [JsonIgnore]
        public virtual string ServiceDependencyName => TextServiceDependencies[ServiceDependency];
        public virtual GsxServiceDependencyScope ServiceDependencyScope { get; set; } = GsxServiceDependencyScope.LastCalled;
        [JsonIgnore]
        public virtual string ServiceDependencyScopeName => TextServiceDependencyScope[ServiceDependencyScope];
        public virtual GsxServiceActivation ServiceCancellation { get; set; } = GsxServiceActivation.Skip;
        [JsonIgnore]
        public virtual string ServiceCancellationName => TextServiceCancellations[ServiceCancellation];
        public virtual GsxServiceDependency ServiceCancelDependency { get; set; } = GsxServiceDependency.Completed;
        [JsonIgnore]
        public virtual string ServiceCancelDependencyName => TextServiceDependencies[ServiceDependency];
        public virtual GsxServiceDependencyScope ServiceCancelDependencyScope { get; set; } = GsxServiceDependencyScope.AllCalled;
        [JsonIgnore]
        public virtual string ServiceDCancelependencyScopeName => TextServiceDependencyScope[ServiceCancelDependencyScope];
        public virtual TimeSpan MaxTimeBeforeDeparture { get; set; } = TimeSpan.Zero;

        public virtual bool OnlyDeparture { get; set; } = false;
        public virtual bool OnlyTurn { get; set; } = false;
        public virtual bool OnlyHub { get; set; } = false;
        public virtual bool OnlyNonHub { get; set; } = false;
        public virtual bool OnlyPreferredOp { get; set; } = false;
        public virtual bool CallOnCargo { get; set; } = false;
        public virtual TimeSpan MinimumFlightDuration { get; set; } = TimeSpan.Zero;
        [JsonIgnore]
        public virtual bool HasDurationConstraint => MinimumFlightDuration > TimeSpan.Zero;
        public virtual TimeSpan MaxRunTime { get; set; } = TimeSpan.Zero;
        [JsonIgnore]
        public virtual bool HasMaxRunTime => MaxRunTime > TimeSpan.Zero;
        public virtual TimeSpan CallDelay { get; set; } = TimeSpan.Zero;
        [JsonIgnore]
        public virtual bool HasCallDelay => CallDelay > TimeSpan.Zero;

        [JsonIgnore]
        public virtual int TurnCount { get; set; } = 0;

        public ServiceConfig() { }

        public ServiceConfig(GsxServiceType type, GsxServiceActivation activation, TimeSpan departureTime, GsxServiceDependency serviceDependency, GsxServiceDependencyScope dependencyScope, bool onlyDepart, bool onlyTurn, bool onlyHub, bool onlyNonHub, bool callOnCargo, TimeSpan duration, TimeSpan maxRun, TimeSpan callDelay)
        {
            ServiceType = type;
            ServiceActivation = activation;
            MaxTimeBeforeDeparture = departureTime;
            ServiceDependency = serviceDependency;
            ServiceDependencyScope = dependencyScope;
            OnlyDeparture = onlyDepart;
            OnlyTurn = onlyTurn;
            OnlyHub = onlyHub;
            OnlyNonHub = onlyNonHub;
            CallOnCargo = callOnCargo;
            MinimumFlightDuration = duration;
            MaxRunTime = maxRun;
            CallDelay = callDelay;
        }
    }
}
