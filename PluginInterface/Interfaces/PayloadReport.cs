namespace Any2GSX.PluginInterface.Interfaces
{
    public class PayloadReport(long id)
    {
        public virtual long Id { get; } = id;
        public virtual int CountPax { get; set; }
        public virtual int CountBags { get; set; }
        public virtual double WeightCargoKg { get; set; }
    }
}
