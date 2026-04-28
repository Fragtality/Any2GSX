using Any2GSX.AppConfig;
using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Controls;

namespace Any2GSX.UI.Views.Automation
{
    public partial class ModelVariables(AppService appService) : ModelBase<SettingProfile>(appService?.SettingProfile, appService)
    {
        public virtual SimStore SimStore => AppService.Instance.SimStore;

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
            return !string.IsNullOrWhiteSpace(VariableName) && !string.IsNullOrWhiteSpace(VariableUnit);
        }

        public static void SetTextBlock(ISimResourceSubscription subscription, TextBlock textBlock)
        {
            if (textBlock != null && subscription is SimVarSubscription variable)
                textBlock.Text = GetVariable(variable);
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
                VariableSubscription = SimStore.AddVariable(VariableName, VariableUnit);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void StopMonitor()
        {
            try
            {
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
        }
    }
}
