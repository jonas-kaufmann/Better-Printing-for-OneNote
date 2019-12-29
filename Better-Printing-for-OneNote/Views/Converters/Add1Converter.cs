using System;
using System.Globalization;
using System.Windows.Data;

namespace Better_Printing_for_OneNote.Views.Converters
{
    public class Add1Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int)
                return (int)value + 1;
            else if (value is long)
                return (long)value + 1;
            else throw new Exception($"{nameof(Add1Converter)} can only convert int and long.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
                if (int.TryParse(text, out int intVal))
                    return intVal - 1;
                else if (long.TryParse(text, out long longVal))
                    return longVal - 1;
                else throw new Exception("Cannot convert back value to int or long");
            else
                return 0;
        }
    }
}
