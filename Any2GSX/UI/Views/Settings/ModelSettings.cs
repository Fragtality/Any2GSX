using Any2GSX.AppConfig;
using Any2GSX.PluginInterface.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ModelSettings : ModelBase<Config>
    {
        protected virtual DispatcherTimer UnitUpdateTimer { get; }
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
            UnitUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UnitUpdateTimer.Tick += UnitUpdateTimer_Tick;

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
            if ((e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                && !UnitUpdateTimer.IsEnabled)
                UnitUpdateTimer.Start();
        }

        protected virtual void UnitUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            NotifyPropertyChanged(nameof(FuelCompareVariance));
            ModelSavedFuel.NotifyCollectionChanged();
            InhibitConfigSave = false;
            UnitUpdateTimer.Stop();
        }

        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;
        public virtual DisplayUnit DisplayUnitDefault { get => Source.DisplayUnitDefault; set { SetModelValue<DisplayUnit>(value); Config.EvaluateDisplayUnit(); } }
        public virtual DisplayUnitSource DisplayUnitSource { get => Source.DisplayUnitSource; set { SetModelValue<DisplayUnitSource>(value); Config.EvaluateDisplayUnit(); } }
        public virtual string SimbriefUser { get => Source.SimbriefUser; set { SetModelValue<string>(value); NotifyPropertyChanged(nameof(BrushSimbrief)); } }
        public virtual double FuelResetPercent { get => Source.FuelResetPercent * 100.0; set => SetModelValue<double>(value / 100.0); }
        public virtual double FuelCompareVariance { get => Config.ConvertKgToDisplayUnit(Source.FuelCompareVariance); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual bool ResetGsxStateVarsFlight { get => Source.ResetGsxStateVarsFlight; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxOnTaxiIn { get => Source.RestartGsxOnTaxiIn; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxStartupFail { get => Source.RestartGsxStartupFail; set => SetModelValue<bool>(value); }
        public virtual int GsxMenuStartupMaxFail { get => Source.GsxMenuStartupMaxFail; set => SetModelValue<int>(value); }
        public virtual int PortBase { get => Source.PortBase; set => SetModelValue<int>(value); }
        public virtual int PortRange { get => Source.PortRange; set => SetModelValue<int>(value); }
        public virtual string DeckUrlBase { get => Source.DeckUrlBase; set => SetModelValue<string>(value); }
        public virtual bool OpenAppWindowOnStart { get => Source.OpenAppWindowOnStart; set => SetModelValue<bool>(value); }
        public virtual bool RefreshMenuForEfb { get => Source.RefreshMenuForEfb; set => SetModelValue<bool>(value); }
    }
}
