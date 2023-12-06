using System.Globalization;
using System.Windows.Data;

namespace StockDevControl.Converters
{
    internal class PasswordConverter : IValueConverter
    {
        public static char PasswordChar = '◆';
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                string password = string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    password += PasswordChar;
                }
                return password;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                if (text.Length > 0 && text[0] == PasswordChar)
                    return Binding.DoNothing;
                return value;
            }
            return Binding.DoNothing;//DependencyProperty.UnsetValue
        }
    }
}
