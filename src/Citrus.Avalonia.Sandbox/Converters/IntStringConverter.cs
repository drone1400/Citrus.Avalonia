using System;
using System.Globalization;
using Avalonia.Data.Converters;
namespace Citrus.Avalonia.Sandbox.Converters {
    public class IntStringConverter : IValueConverter {

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is int intValue) {
                return intValue.ToString();
            }
            throw new ArgumentException("value must be integer");
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is string str) {
                try {
                    return int.Parse(str);
                } catch (Exception) {
                    return null;
                }
            }
            throw new ArgumentException("value must be string");
        }
    }
}
