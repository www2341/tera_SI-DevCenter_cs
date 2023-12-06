using StockDevControl.StockModels;
using System.Globalization;
using System.Windows.Data;

namespace StockDevControl.Converters
{
    internal class OrderTypeCompareConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrderType val && parameter is OrderType comp)
            {
                return val == comp;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolval && boolval)
                return parameter;
            return Binding.DoNothing;
        }
    }
}
