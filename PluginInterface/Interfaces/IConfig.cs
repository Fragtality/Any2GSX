using System.Collections.Generic;
using System.ComponentModel;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IConfig
    {
        public bool OpenAppWindowOnStart { get; }
        public double WeightConversion { get; }
        public string BinaryGsx2020 { get; }
        public string BinaryGsx2024 { get; }
        public string SimbriefUrlBase { get; }
        public string SimbriefUrlPathName { get; }
        public string SimbriefUrlPathId { get; }
        public string SimbriefUser { get; }
        public int UiRefreshInterval { get; }
        public double FuelCompareVariance { get; }
        public int TimerGsxCheck { get; }
        public int TimerGsxProcessCheck { get; }
        public int TimerGsxStartupMenuCheck { get; }
        public bool RestartGsxOnTaxiIn { get; }
        public int GsxServiceStartDelay { get; }
        public int GroundTicks { get; }
        public int MenuCheckInterval { get; }
        public int MenuOpenTimeout { get; }
        public DisplayUnit DisplayUnitCurrent { get; }
        public string DisplayUnitCurrentString { get; }
        public DisplayUnit DisplayUnitDefault { get; }
        public DisplayUnitSource DisplayUnitSource { get; }
        public int OperatorWaitTimeout { get; }
        public int OperatorSelectTimeout { get; }
        public bool DebugArrival { get; }
        public int StateMachineInterval { get; }
        public int DelayServiceStateChange { get; }
        public int SpeedTresholdTaxiOut { get; }
        public int SpeedTresholdTaxiIn { get; }
        public Dictionary<string, double> FuelFobSaved { get; }
        public double FuelResetPercent { get; }


        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetDisplayUnit(DisplayUnit displayUnit);
        public double ConvertKgToDisplayUnit(double kg);
        public double ConvertLbToDisplayUnit(double lb);
        public double ConvertFromDisplayUnitKg(double value);

        public void SetFuelFob(string title, double fuelKg);
        public double GetFuelFob(string title, double fuelCapacityKg, double resetBaseKg, out bool saved);
    }
}
