using StockDevControl.StockModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockDevControl.Converters
{
    public class OrderTypeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrderType val)
            {
                return val switch
                {
                    OrderType.매수 => Brushes.Red,
                    OrderType.매도 => Brushes.Blue,
                    _ => Brushes.Green
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
