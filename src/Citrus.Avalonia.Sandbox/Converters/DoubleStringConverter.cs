using System;
using System.Globalization;
using Avalonia.Data.Converters;
namespace Citrus.Avalonia.Sandbox.Converters {
    public class DoubleStringConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is double doubleValue) {
                return doubleValue.ToString("0.00",CultureInfo.InvariantCulture);
            }
            throw new ArgumentException("value must be double");
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is string str) {
                try {
                    return double.Parse(str);
                } catch (Exception) {
                    return null;
                }
            }
            throw new ArgumentException("value must be string");
        }
    }
}
