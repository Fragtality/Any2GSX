using System;

namespace Any2GSX.Tools
{
    public static class AudioTools
    {
        public static double NormalizedRatio(double value, double minimum, double maximum)
        {
            if (minimum < 0.0)
            {
                maximum += Math.Abs(minimum);
                value += Math.Abs(minimum);
            }
            else if (minimum > 0.0)
            {
                maximum -= minimum;
                value -= minimum;
            }

            return Ratio(value, maximum);
        }

        public static double Ratio(double value, double maximum)
        {
            double ratio = value / maximum;
            if (ratio < 0.0)
                ratio = 0.0;
            else if (ratio > 1.0)
                ratio = 1.0;

            return ratio;
        }
    }
}
