using System.Globalization;
using System.Windows.Data;

namespace StockDevControl.Converters
{
    public class RadioBoolToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int integer = (int)value;
            int.TryParse(parameter as string, out int param_int);
            if (integer == param_int)
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
