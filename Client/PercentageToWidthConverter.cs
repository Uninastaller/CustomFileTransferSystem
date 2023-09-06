using System;
using System.Globalization;
using System.Windows.Data;

namespace Client
{
    public class PercentageToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || values[0] == null || values[1] == null)
            {
                return 0.0; // return a default value
            }

            if (!(values[0] is float percentage) || !(values[1] is double totalWidth))
            {
                return 0.0; // return a default value
            }

            return (percentage / 100.0) * totalWidth;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
