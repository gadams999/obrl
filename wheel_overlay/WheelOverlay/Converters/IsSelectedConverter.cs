using System;
using System.Globalization;
using System.Windows.Data;

namespace WheelOverlay.Converters
{
    public class IsSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is string currentItem && values[1] is string item)
            {
                return currentItem == item;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
