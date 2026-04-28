using CFIT.AppTools;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Any2GSX.UI
{
    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanConverterSeconds() : TimeSpanConverter(0, int.MaxValue, false)
    {

    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanConverterMinutes() : TimeSpanConverter(0, int.MaxValue, true)
    {

    }

    [ValueConversion(typeof(TimeSpan), typeof(string))]
    public class TimeSpanConverter(int minValue = 0, int maxValue = int.MaxValue, bool minutes = true) : IValueConverter
    {
        public virtual int MinValue { get; } = minValue;
        public virtual int MaxValue { get; } = maxValue;
        public virtual bool UseMinutes { get; } = minutes;

        public virtual int GetTotal(TimeSpan span)
        {
            return (int)(UseMinutes ? span.TotalMinutes : span.TotalSeconds);
        }

        public virtual TimeSpan GetTimespan(int span)
        {
            if (span < MinValue || span > MaxValue)
                return TimeSpan.Zero;
            else
                return UseMinutes ? TimeSpan.FromMinutes(span) : TimeSpan.FromSeconds(span);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan span)
                return $"{GetTotal(span)}";
            else
                return $"0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str) && int.TryParse(str, new RealInvariantFormat(str), out int span))
                return GetTimespan(span);
            else
                return TimeSpan.Zero;
        }
    }
}
