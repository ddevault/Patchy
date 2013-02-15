using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MonoTorrent.Common;

namespace Patchy.Converters
{
    public class PriorityToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var priority = (Priority)value;
            switch (priority)
            {
                case Priority.DoNotDownload:
                    return 0;
                case Priority.Lowest:
                    return 1;
                case Priority.Low:
                    return 2;
                case Priority.Normal:
                    return 3;
                case Priority.High:
                    return 4;
                case Priority.Highest:
                    return 5;
                default:
                    return 6;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int priority = System.Convert.ToInt32(value.ToString());
            switch (priority)
            {
                case 0:
                    return Priority.DoNotDownload;
                case 1:
                    return Priority.Lowest;
                case 2:
                    return Priority.Low;
                case 3:
                    return Priority.Normal;
                case 4:
                    return Priority.High;
                case 5:
                    return Priority.Highest;
                default:
                    return Priority.Immediate;
            }
        }
    }
}
