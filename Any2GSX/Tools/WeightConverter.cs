using Any2GSX.PluginInterface.Interfaces;

namespace Any2GSX.Tools
{
    public class WeightConverter() : IWeightConverter
    {
        public virtual double WeightConversion => AppService.Instance.Config.WeightConversion;

        public virtual double ToKg(double pound)
        {
            return pound / WeightConversion;
        }

        public virtual double ToLb(double kilo)
        {
            return kilo * WeightConversion;
        }
    }
}
