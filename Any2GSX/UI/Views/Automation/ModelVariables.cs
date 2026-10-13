using Any2GSX.AppConfig;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.InputEvents;
using CFIT.SimConnectLib.SimEvents;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelVariables(AppService appService) : ModelBase<SettingProfile>(appService?.SettingProfile, appService)
    {
        public virtual SimStore SimStore => AppService.Instance.SimStore;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MonitorVariableCommand))]
        public partial int VariableType { get; set; } = 0;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MonitorVariableCommand))]
        public partial string VariableName { get; set; } = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(MonitorVariableCommand))]
        public partial string VariableUnit { get; set; } = "";

        [ObservableProperty]
        public partial string VariableValue { get; set; } = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMonitorActive))]
        [NotifyPropertyChangedFor(nameof(IsEditAllowed))]
        public partial ISimResourceSubscription VariableSubscription { get; protected set; }
        public virtual bool IsMonitorActive => VariableSubscription != null;
        public virtual bool IsEditAllowed => !IsMonitorActive;

        public virtual Dictionary<int, string> MonitorTypes { get; } = new()
        {
            { 0, "SimVar" },
            { 1, "SimEvent" },
            { 2, "InputEvent" },
        };

        protected override void InitializeModel()
        {

        }

        [RelayCommand(CanExecute = nameof(CanMonitor))]
        public virtual void MonitorVariable()
        {
            if (IsMonitorActive)
                StopMonitor();
            else
                StartMonitor();
        }

        protected virtual bool CanMonitor()
        {
            return !string.IsNullOrWhiteSpace(VariableName) && ((VariableType == 0 && !string.IsNullOrWhiteSpace(VariableUnit)) || VariableType > 0);
        }

        public static string GetSubscription(ISimResourceSubscription subscription)
        {
            string value = "nil";

            if (subscription is SimVarSubscription variable)
                value = GetVariable(variable);
            else if (subscription != null)
                value = subscription.GetNumber().ToString();

            return value;
        }

        public static string GetVariable(SimVarSubscription variable)
        {
            try
            {
                if (variable.Resource.IsString)
                    return variable?.GetString() ?? "";
                else
                    return (variable?.GetNumber() ?? 0).ToString();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return ex.GetType().Name;
            }
        }

        protected virtual void StartMonitor()
        {
            try
            {
                VariableSubscription = null;
                VariableValue = "";

                if (VariableType == 2)
                    VariableSubscription = SimStore.AddInput(VariableName);
                else if (VariableType == 1)
                    VariableSubscription = SimStore.AddEvent(VariableName);
                else
                    VariableSubscription = SimStore.AddVariable(VariableName, VariableUnit);

                if (VariableSubscription != null)
                    VariableSubscription.OnReceived += VariableSubscriptionOnReceived;
                else
                    Logger.Error($"Sim Resource '{VariableName}' could not be subscribed");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void SetVariableValue(ISimResourceSubscription sub)
        {
            try
            {
                if (sub is SimVarSubscription subVar && subVar.Resource.IsString)
                    VariableValue = subVar.GetString();
                else
                    VariableValue = Conversion.ToString(sub.GetNumber());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual Task VariableSubscriptionOnReceived(ISimResourceSubscription sub, object value)
        {
            ModelHelper.RunOnDispatcher(() => SetVariableValue(sub));
            return Task.CompletedTask;
        }

        protected virtual void StopMonitor()
        {
            try
            {
                VariableSubscription?.OnReceived -= VariableSubscriptionOnReceived;
                SimStore.Remove(VariableName);
                VariableSubscription = null;
                VariableValue = "";
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                VariableSubscription = null;
                VariableValue = "";
            }
        }

        public virtual void Refresh()
        {
            if (VariableSubscription is SimVarSubscription variable)
                VariableValue = GetVariable(variable);
            else if (VariableSubscription is SimEventSubscription simEvent)
                VariableValue = simEvent.GetNumber().ToString();
            else if (VariableSubscription is InputEventSubscription inputEvent)
                VariableValue = inputEvent.GetString();
        }
    }
}
