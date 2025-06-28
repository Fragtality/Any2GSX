using System;

namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IFlightplan
    {
        public int Id { get; }
        public int LastId { get; }
        public bool IsLoaded { get; }
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
        public double FuelRampKg { get; }
        public int CountPax { get; }
        public int DiffPax { get; }
        public int CountBags { get; }
        public double WeightPaxKg { get; }
        public double WeightBagKg { get; }
        public double WeightFreightKg { get; }
        public double WeightCargoKg { get; }
        public double WeightPayloadKg { get; }
        public double ZeroFuelRampKg { get; }
        public double WeightTotalRampKg { get; }

        public void UpdatePlannedFuelKg(double fuelRampKg);
        public void UpdatePassengerCount(int count);
        public void UpdateFreightKg(double freightKg);
    }
}
