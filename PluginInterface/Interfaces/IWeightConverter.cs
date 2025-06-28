namespace Any2GSX.PluginInterface.Interfaces
{
    public interface IWeightConverter
    {
        public double WeightConversion { get; }
        public double ToKg(double pound);
        public double ToLb(double kilo);
    }
}
