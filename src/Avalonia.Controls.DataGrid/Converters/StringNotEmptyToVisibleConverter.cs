using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace Avalonia.Controls
{
    internal class StringNotEmptyToVisibleConverter : IValueConverter
    {
        public static readonly StringNotEmptyToVisibleConverter Default = new StringNotEmptyToVisibleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return !string.IsNullOrEmpty(s);
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
