using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Setting
{
    public class ConfigChangedItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ConfigItemKey key = ConfigItemKey.Unknown;
            if (values[0] != null)
            {
                Enum.TryParse<ConfigItemKey>(values[0].ToString(), out key);
            }

            string value = values[1] == null ? null : values[1] as string;
            ConfigChangedItem item = new ConfigChangedItem()
            {
                key = key,
                value = value
            };

            return item;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
