using System;
using System.Threading.Tasks;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IFlightplan
    {
        public long Id { get; }
        public long LastId { get; }
        public bool IsLoaded { get; }
        public bool LastOnlineCheck { get; }
        public string SimbriefUser { get; }
        public DisplayUnit Unit { get; }
        public double WeightPerPaxKg { get; }
        public double WeightPerBagKg { get; }
        public string Number { get; }
        public string AircraftType { get; }
        public string AircraftReg { get; }
        public string Origin { get; }
        public string Destination { get; }
        public TimeSpan Duration { get; }
        public DateTime ScheduledOutTime { get; }
        public double FuelRampKg { get; }
        public int CountPaxPlanned { get; }
        public int CountPax { get; }
        public int MaxPax { get; }
        public int DiffPax { get; }
        public int DiffBags { get; }
        public int CountBagsPlanned { get; }
        public int CountBags { get; }
        public double WeightPaxKg { get; }
        public double WeightBagKg { get; }
        public double WeightFreightKg { get; }
        public double WeightCargoKg { get; }
        public double WeightCargoPlannedKg { get; }
        public double WeightPayloadKg { get; }
        public double AircraftEmptyOewKg { get; }
        public double ZeroFuelRampKg { get; }
        public double WeightTotalRampKg { get; }
        public double DiffPayloadKg { get; }

        public event Func<IFlightplan, Task> OnImport;
        public event Func<Task> OnUnload;

        public Task<bool> ImportOfp();
        public double RoundUp100(double value);
        public void UpdatePlannedFuelKg(double fuelRampKg);
        public void UpdatePassengerMax(int count);
        public void UpdatePassengerCount(int count, int diff = 0);
        public void UpdateBagCount(int count, int diff = 0);
        public void UpdateFreightKg(double freightKg);
        public void UpdateScheduledOut(DateTime outTime);
    }
}
