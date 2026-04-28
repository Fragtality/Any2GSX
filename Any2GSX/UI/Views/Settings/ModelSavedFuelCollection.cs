using Any2GSX.AppConfig;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Any2GSX.UI.Views.Settings
{
    public partial class ModelSavedFuelCollection : ViewModelCollection<KeyValuePair<string, double>, ModelSavedFuelItem>
    {
        public override ICollection<KeyValuePair<string, double>> Source => AppService.Instance?.Config?.FuelFobSaved ?? [];

        public ModelSavedFuelCollection() : base(AppService.Instance?.Config?.FuelFobSaved ?? [],
                    (kv) => new ModelSavedFuelItem(kv),
                    (kv) => !string.IsNullOrWhiteSpace(kv.Key))
        {
            AddAllowed = false;
            UpdatesAllowed = true;
        }

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();
            CreateMemberBinding<double, string>("Value", new TextUnitConverter());
        }

        public override bool UpdateSource(KeyValuePair<string, double> oldItem, KeyValuePair<string, double> newItem)
        {
            if (oldItem.Key == newItem.Key && oldItem.Key != null && Source is IDictionary<string, double> dict && dict.ContainsKey(newItem.Key))
            {
                dict[oldItem.Key] = newItem.Value;
                return true;
            }
            else
                return false;
        }

        protected override bool RemoveSource(KeyValuePair<string, double> item)
        {
            if (item.Key != null && Source is IDictionary<string, double> dict && dict.ContainsKey(item.Key))
            {
                dict.Remove(item.Key);
                return true;
            }
            else
                return false;
        }

        public override bool Contains(KeyValuePair<string, double> item)
        {
            return item.Key != null && Source is IDictionary<string, double> dict && dict.ContainsKey(item.Key);
        }
    }

    public class ModelSavedFuelItem(KeyValuePair<string, double> valuePair)
    {
        public virtual string Id { get; } = valuePair.Key;
        public virtual double Value { get; set; } = valuePair.Value;

        public override string ToString()
        {
            return $"{Math.Round(AppService.Instance.Config.ConvertKgToDisplayUnit(Value), 2)} {AppService.Instance.Config.DisplayUnitCurrentString} @ '{Id}'";
        }
    }

    public class TextUnitConverter : IValueConverter
    {
        protected virtual Config Config => AppService.Instance.Config;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string text)
                {
                    if (Conversion.IsNumber(text, out double doubleValue))
                        return Config.ConvertFromDisplayUnitKg(doubleValue);
                    else if (Conversion.IsNumberF(text, out float floatValue))
                        return Config.ConvertFromDisplayUnitKg(floatValue);
                    else
                        return value;
                }
                else if (value is double doubleValue && doubleValue > 0)
                    return Conversion.ToString(Config.ConvertKgToDisplayUnit(doubleValue));
                else if (value is float floatValue && floatValue > 0)
                    return Conversion.ToString(Config.ConvertKgToDisplayUnit(floatValue));
                else
                    return value;
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
