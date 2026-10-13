using Any2GSX.GSX.Automation;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelServiceConfig(ServiceConfig config, ModelAutomation model) : ModelBase<ServiceConfig>(config, AppService.Instance)
    {
        public virtual GsxServiceType ServiceType { get => Source.ServiceType; set => SetModelValue<GsxServiceType>(value); }
        public virtual CommandWrapper<int?> CommandButtonUp { get; } = model.CommandButtonUp;
        public virtual CommandWrapper<int?> CommandButtonDown { get; } = model.CommandButtonDown;
        public virtual GsxServiceActivation ServiceActivation
        {
            get => Source.ServiceActivation;
            set
            {
                SetModelValue<GsxServiceActivation>(value);
                NotifyPropertyChanged(nameof(HasActivation));
                NotifyPropertyChanged(nameof(HasDepartureTime));
                NotifyPropertyChanged(nameof(HasDependency));
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
                NotifyPropertyChanged(nameof(ActivationText));
                NotifyPropertyChanged(nameof(ServiceActivationName));
                NotifyPropertyChanged(nameof(HasCancellation));
                NotifyPropertyChanged(nameof(HasMaxRunTime));
                NotifyPropertyChanged(nameof(MaxRunTime));
                NotifyPropertyChanged(nameof(HasCancelDependency));
                NotifyPropertyChanged(nameof(ServiceCancelDependency));
                NotifyPropertyChanged(nameof(ServiceCancelDependencyScope));
                NotifyPropertyChanged(nameof(ServiceCancellationName));
            }
        }
        public virtual string MaxTimeBeforeDeparture
        {
            get => Source.MaxTimeBeforeDeparture.TotalMinutes.ToString();
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, new RealInvariantFormat(value), out double number))
                {
                    SetModelValue<TimeSpan>(TimeSpan.FromMinutes(number));
                    NotifyPropertyChanged(nameof(ActivationText));
                }
                else
                    NotifyPropertyChanged(nameof(MaxTimeBeforeDeparture));
            }
        }
        public virtual string ServiceActivationName => Source.ServiceActivationName;
        public virtual bool HasActivation => ServiceActivation != GsxServiceActivation.Skip && ServiceActivation != GsxServiceActivation.Manual;
        public virtual bool HasDepartureTime => ServiceActivation == GsxServiceActivation.DepartureTime;
        public virtual bool HasDependency => ServiceActivation == GsxServiceActivation.ServiceDependent;
        public virtual GsxServiceDependency ServiceDependency
        {
            get => Source.ServiceDependency;
            set
            {
                SetModelValue<GsxServiceDependency>(value);
                NotifyPropertyChanged(nameof(ActivationText));
            }
        }
        public virtual GsxServiceDependencyScope ServiceDependencyScope
        {
            get => Source.ServiceDependencyScope;
            set
            {
                SetModelValue<GsxServiceDependencyScope>(value);
                NotifyPropertyChanged(nameof(ActivationText));
            }
        }
        public virtual GsxServiceActivation ServiceCancellation
        {
            get => Source.ServiceCancellation;
            set
            {
                SetModelValue<GsxServiceActivation>(value);
                NotifyPropertyChanged(nameof(HasCancellation));
                NotifyPropertyChanged(nameof(HasMaxRunTime));
                NotifyPropertyChanged(nameof(MaxRunTime));
                NotifyPropertyChanged(nameof(HasCancelDependency));
                NotifyPropertyChanged(nameof(ServiceCancelDependency));
                NotifyPropertyChanged(nameof(ServiceCancelDependencyScope));
                NotifyPropertyChanged(nameof(ServiceCancellationName));
            }
        }
        public virtual string MaxRunTime
        {
            get => Source.MaxRunTime.TotalMinutes.ToString();
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, new RealInvariantFormat(value), out double number))
                {
                    SetModelValue<TimeSpan>(TimeSpan.FromMinutes(number));
                }
                else
                    NotifyPropertyChanged(nameof(MaxRunTime));
            }
        }
        public virtual string ServiceCancellationName => Source.ServiceCancellationName;
        public virtual bool HasCancellation => ServiceCancellation >= GsxServiceActivation.DepartureTime;
        public virtual bool HasMaxRunTime => ServiceCancellation == GsxServiceActivation.DepartureTime && HasActivation;
        public virtual bool HasCancelDependency => ServiceCancellation == GsxServiceActivation.ServiceDependent;
        public virtual GsxServiceDependency ServiceCancelDependency
        {
            get => Source.ServiceCancelDependency;
            set
            {
                SetModelValue<GsxServiceDependency>(value);
            }
        }
        public virtual GsxServiceDependencyScope ServiceCancelDependencyScope
        {
            get => Source.ServiceCancelDependencyScope;
            set
            {
                SetModelValue<GsxServiceDependencyScope>(value);
            }
        }

        public virtual string ActivationText => BuildActivationText();

        public virtual string BuildActivationText()
        {
            string text = "";

            if (ServiceActivation == GsxServiceActivation.DepartureTime)
                text = $"below {MaxTimeBeforeDeparture} Minutes";
            else if (ServiceActivation == GsxServiceActivation.ServiceDependent)
                text = $"{Source.ServiceDependencyScopeName} at least {Source.ServiceDependencyName}";

            return text;
        }

        public virtual string ConstraintText => BuildConstraintText();

        public virtual string BuildConstraintText()
        {
            List<string> constraints = [];
            string text = "";

            if (Source.HasDurationConstraint)
                constraints.Add($"Min. {MinimumFlightDuration} Flight");

            if (OnlyOnPax)
                constraints.Add("Pax");

            if (OnlyDeparture)
                constraints.Add("Departure");
            else if (OnlyTurn)
                constraints.Add("Turnaround");

            if (OnlyHub)
                constraints.Add("Hub");
            else if (OnlyNonHub)
                constraints.Add("Non-Hub");

            if (OnlyPreferredOp)
                constraints.Add("Pref. Op");

            if (Source.HasMaxRunTime)
                constraints.Add($"Max Run {MaxRunTime}m");

            if (constraints.Count > 0 && ServiceActivation > GsxServiceActivation.Manual)
                text = $"( {string.Join(" | ", constraints)} )";
            return text;
        }

        public virtual bool HasAnyConstraint => !string.IsNullOrWhiteSpace(ConstraintText);

        public virtual bool OnlyDeparture
        {
            get => Source.OnlyDeparture;
            set
            {
                if (value && Source.OnlyTurn)
                {
                    OnlyTurn = false;
                    NotifyPropertyChanged(nameof(Source.OnlyTurn));
                }
                SetModelValue<bool>(value);
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual bool OnlyTurn
        {
            get => Source.OnlyTurn;
            set
            {
                if (value && Source.OnlyDeparture)
                {
                    OnlyDeparture = false;
                    NotifyPropertyChanged(nameof(Source.OnlyDeparture));
                }
                SetModelValue<bool>(value);
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual bool OnlyHub
        {
            get => Source.OnlyHub;
            set
            {
                if (value && Source.OnlyNonHub)
                {
                    OnlyNonHub = false;
                    NotifyPropertyChanged(nameof(Source.OnlyNonHub));
                }
                SetModelValue<bool>(value);
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual bool OnlyNonHub
        {
            get => Source.OnlyNonHub;
            set
            {
                if (value && Source.OnlyHub)
                {
                    OnlyHub = false;
                    NotifyPropertyChanged(nameof(Source.OnlyHub));
                }
                SetModelValue<bool>(value);
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual bool OnlyPreferredOp
        {
            get => Source.OnlyPreferredOp;
            set
            {
                SetModelValue<bool>(value);
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual bool OnlyOnPax
        {
            get => !Source.CallOnCargo;
            set
            {
                SetModelValue<bool>(!value, null, null, nameof(Source.CallOnCargo));
                NotifyPropertyChanged(nameof(ConstraintText));
                NotifyPropertyChanged(nameof(HasAnyConstraint));
            }
        }
        public virtual string MinimumFlightDuration
        {
            get => Source.MinimumFlightDuration.ToString(@"hh\:mm");
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && value.Length == 5 && value != "00:00" && TimeSpan.TryParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture, out var span))
                {
                    SetModelValue<TimeSpan>(span);
                    NotifyPropertyChanged(nameof(ConstraintText));
                    NotifyPropertyChanged(nameof(HasAnyConstraint));
                }
                else if (string.IsNullOrWhiteSpace(value) || value.Length != 5)
                    NotifyPropertyChanged(nameof(MinimumFlightDuration));
            }
        }

        public virtual string CallDelay
        {
            get => Source.CallDelay.TotalSeconds.ToString();
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, new RealInvariantFormat(value), out double number))
                    SetModelValue<TimeSpan>(TimeSpan.FromSeconds(number));
                else
                    NotifyPropertyChanged(nameof(CallDelay));
            }
        }

        protected override void InitializeModel()
        {

        }
    }
}
