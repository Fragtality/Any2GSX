using Any2GSX.AppConfig;
using Any2GSX.PluginInterface.Interfaces;
using CFIT.AppLogger;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ModelSettings : ModelBase<Config>
    {
        public virtual ModelSavedFuelCollection ModelSavedFuel { get; }
        public virtual SolidColorBrush BrushSimbrief => string.IsNullOrWhiteSpace(SimbriefUser) ? Brushes.Red : SystemColors.ActiveBorderBrush;
        public virtual Dictionary<DisplayUnit, string> DisplayUnitDefaultItems { get; } = new()
        {
            { DisplayUnit.KG, "kg" },
            { DisplayUnit.LB, "lb" },
        };
        public virtual Dictionary<DisplayUnitSource, string> DisplayUnitSourceItems { get; } = new()
        {
            { DisplayUnitSource.App, "App" },
            { DisplayUnitSource.Simbrief, "Simbrief" },
            { DisplayUnitSource.Aircraft, "Aircraft" },
        };

        public ModelSettings(AppService appService) : base(appService.Config, appService)
        {
            ModelSavedFuel = new();
            ModelSavedFuel.CollectionChanged += OnSavedFuelCollectionChanged;
        }

        protected override void InitializeModel()
        {
            Config.PropertyChanged += OnConfigPropertyChanged;
        }

        protected virtual void OnSavedFuelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!InhibitConfigSave)
                Config.SaveConfiguration();
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                OnUnitChanged();
            if (e?.PropertyName == nameof(Config.FuelFobSaved))
                OnFuelCollectionChanged();
        }

        protected virtual void OnUnitChanged()
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            NotifyPropertyChanged(nameof(FuelCompareVariance));
            ModelSavedFuel.NotifyCollectionChanged();
            InhibitConfigSave = false;
        }

        protected virtual void OnFuelCollectionChanged()
        {
            InhibitConfigSave = true;
            ModelSavedFuel.NotifyCollectionChanged();
            InhibitConfigSave = false;
        }

        //OFP & Weights
        public virtual string SimbriefUser { get => Source.SimbriefUser; set { SetModelValue<string>(value); NotifyPropertyChanged(nameof(BrushSimbrief)); } }
        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;
        public virtual DisplayUnit DisplayUnitDefault { get => Source.DisplayUnitDefault; set { SetModelValue<DisplayUnit>(value); Config.EvaluateDisplayUnit(); } }
        public virtual DisplayUnitSource DisplayUnitSource { get => Source.DisplayUnitSource; set { SetModelValue<DisplayUnitSource>(value); Config.EvaluateDisplayUnit(); } }

        //GSX Settings
        public virtual bool RestartGsxOnTaxiIn { get => Source.RestartGsxOnTaxiIn; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxStartupFail { get => Source.RestartGsxStartupFail; set => SetModelValue<bool>(value); }
        public virtual int GsxMenuStartupMaxFail { get => Source.GsxMenuStartupMaxFail; set => SetModelValue<int>(value); }
        public virtual int SpeedTresholdTaxiIn { get => Source.SpeedTresholdTaxiIn; set => SetModelValue<int>(value); }

        public virtual int DelayOpenTaxiInMenu { get => Source.DelayOpenTaxiInMenu; set => SetModelValue<int>(value); }
        public virtual int PanelRefuelOpenDelayUnderground { get => Source.PanelRefuelOpenDelayUnderground; set => SetModelValue<int>(value); }
        public virtual int PanelRefuelCloseDelayUnderground { get => Source.PanelRefuelCloseDelayUnderground; set => SetModelValue<int>(value); }
        public virtual int PanelRefuelOpenDelayTanker { get => Source.PanelRefuelOpenDelayTanker; set => SetModelValue<int>(value); }
        public virtual int PanelRefuelCloseDelayTanker { get => Source.PanelRefuelCloseDelayTanker; set => SetModelValue<int>(value); }
        public virtual int PanelLavatoryOpenDelay { get => Source.PanelLavatoryOpenDelay; set => SetModelValue<int>(value); }
        public virtual int PanelWaterOpenDelay { get => Source.PanelWaterOpenDelay; set => SetModelValue<int>(value); }
        public virtual int OperatorSelectTimeout { get => Source.OperatorSelectTimeout / 1000; set => SetModelValue<int>(value * 1000); }

        //APP
        public virtual double FuelResetPercent { get => Source.FuelResetPercent * 100.0; set => SetModelValue<double>(value / 100.0); }
        public virtual double FuelCompareVariance { get => Config.ConvertKgToDisplayUnit(Source.FuelCompareVariance); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }

        public virtual int AudioDeviceCheckInterval { get => Source.AudioDeviceCheckInterval; set => SetModelValue<int>(value); }
        public virtual int AudioProcessCheckInterval { get => Source.AudioProcessCheckInterval; set => SetModelValue<int>(value); }
        public virtual int PortBase { get => Source.PortBase; set => SetModelValue<int>(value); }
        public virtual int PortRange { get => Source.PortRange; set => SetModelValue<int>(value); }
        public virtual string DeckUrlBase { get => Source.DeckUrlBase; set => SetModelValue<string>(value); }
        public virtual bool OpenAppWindowOnStart { get => Source.OpenAppWindowOnStart; set => SetModelValue<bool>(value); }
        public virtual bool AppWindowRestorePosition { get => Source.AppWindowRestorePosition; set => SetModelValue<bool>(value); }
        public virtual bool RefreshMenuForEfb { get => Source.RefreshMenuForEfb; set => SetModelValue<bool>(value); }
        public virtual bool RefreshMenuForDeck { get => Source.RefreshMenuForDeck; set => SetModelValue<bool>(value); }
        public virtual int DeckClearedMenuRefresh { get => Source.DeckClearedMenuRefresh / 1000; set => SetModelValue<int>(value * 1000); }
        public virtual bool DebugArrival { get => Source.DebugArrival; set => SetModelValue<bool>(value); }
        public virtual bool VerboseLogging { get => Source.LogLevel == LogLevel.Verbose; set => SetModelValue<LogLevel>(value ? LogLevel.Verbose : LogLevel.Debug, null, null, nameof(Source.LogLevel)); }
    }
}
