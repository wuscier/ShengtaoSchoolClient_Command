using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class RunEnvToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string runDevDescription = EnumHelper.GetDescription(typeof(InterfaceType), value);
                return runDevDescription;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                InterfaceType runEnv = (InterfaceType)Enum.Parse(typeof(InterfaceType), value.ToString());
                return runEnv;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}