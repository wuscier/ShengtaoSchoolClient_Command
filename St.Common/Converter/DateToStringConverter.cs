using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dt = System.Convert.ToDateTime(value);

            return dt.ToString("yyyy-MM-dd HH:mm");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
