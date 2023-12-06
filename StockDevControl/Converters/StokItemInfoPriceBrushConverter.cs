using StockDevControl.StockModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StockDevControl.Converters
{
    public class StokItemInfoPriceBrushConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double price)
            {
                var item_info = values[1] as StockItemInfo;
                if (item_info != null)
                {
                    if (price > item_info.전일가)
                        return Brushes.Red;// Brushes.PaleVioletRed;
                    else if (price < item_info.전일가)
                        return Brushes.Blue;// Brushes.LightBlue;
                }
            }
            return Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
