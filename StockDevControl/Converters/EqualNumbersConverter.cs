using System.Globalization;
using System.Windows.Data;

namespace StockDevControl.Converters
{
    public class EqualNumbersConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? retVals = parameter as string;
            if (retVals != null && value is int comp)
            {
                var comp_vals = retVals.Split(',');
                for (int i = 0; i < comp_vals.Length; i++)
                {
                    if (int.TryParse(comp_vals[i], out int convValus))
                        if (comp == convValus)
                            return true;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
