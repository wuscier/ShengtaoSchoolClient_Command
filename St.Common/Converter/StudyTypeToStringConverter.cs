using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class StudyTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                StudyType studyType = (StudyType) Enum.Parse(typeof(StudyType), value.ToString());
                return studyType == StudyType.SpeakFreely ? "自由发言" : "主讲";
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}