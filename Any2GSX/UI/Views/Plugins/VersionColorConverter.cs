using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Any2GSX.UI.Views.Plugins
{
    public class BoolColorConverter : IValueConverter
    {
        public virtual Color ColorTrue { get; set; } = Colors.Orange;
        public virtual Color ColorFalse { get; set; } = SystemColors.ControlTextColor;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = ColorFalse;
            if (value is bool boolValue && boolValue)
                color = ColorTrue;

            if (targetType == typeof(Brush))
                return new SolidColorBrush(color);
            else
                return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color colorValue)
                return colorValue == ColorTrue;
            else if (value is SolidColorBrush colorBrush)
                return colorBrush.Color == ColorTrue;
            else
                return false;
        }
    }
}
