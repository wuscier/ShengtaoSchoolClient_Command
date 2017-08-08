using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isLive = (bool) value;
                return isLive ? "是" : "否";
            }
            catch (Exception)
            {
                return "否";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}