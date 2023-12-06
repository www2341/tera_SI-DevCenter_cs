using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockDevControl.Converters
{
    public class ValueCompareToBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double compValue)
            {
                if (compValue > 0)
                    return Brushes.Red;
                else if (compValue < 0)
                    return Brushes.Blue;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

