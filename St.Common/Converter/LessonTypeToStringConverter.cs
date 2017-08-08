using System;
using System.Globalization;
using System.Windows.Data;

namespace St.Common
{
    public class LessonTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                string lessonTypeStr = string.Empty;
                LessonType lessonType = (LessonType) Enum.Parse(typeof(LessonType), value.ToString());

                switch (lessonType)
                {
                    case LessonType.Discussion:
                        lessonTypeStr = GlobalResources.Discussion;
                        break;
                    case LessonType.Interactive:
                        lessonTypeStr = GlobalResources.Interactive;
                        break;
                    case LessonType.InteractiveWithoutLive:
                        lessonTypeStr = GlobalResources.InteractiveWithoutLive;
                        break;
                    case LessonType.Live:
                        lessonTypeStr = GlobalResources.Live;
                        break;
                }

                return lessonTypeStr;
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