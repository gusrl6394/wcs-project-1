using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wcs.Monitor
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool b = value is bool && (bool)value;
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is Visibility)
                return (Visibility)value != Visibility.Visible;
            return true;
        }
    }
}
