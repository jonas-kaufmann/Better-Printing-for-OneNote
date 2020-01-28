using System;
using System.Windows.Data;

namespace Better_Printing_for_OneNote.Views.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool a)
                return !a;
            else if (targetType == typeof(bool?))
            {
                bool? v = (bool?)value;
                if (!v.HasValue)
                    return null;
                return !v.Value;
            }
            else
                throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool a)
                return !a;
            else if (targetType == typeof(bool?))
            {
                bool? v = (bool?)value;
                if (!v.HasValue)
                    return null;
                return !v.Value;
            }
            else
                throw new InvalidOperationException("The target must be a boolean");
        }
    }
}