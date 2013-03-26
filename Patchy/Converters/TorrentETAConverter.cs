using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Patchy.Converters
{
    public class TorrentETAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan time = (TimeSpan)value;
            string result = string.Empty;

            if (time == TimeSpan.MaxValue || time == TimeSpan.MinValue)
                result = "n/a";
            else if (Math.Abs(time.TotalSeconds) < 60)
                result = Math.Abs((int)time.TotalSeconds) + " secs";
            else if (Math.Abs(time.TotalSeconds) < (60 * 5))
                result = string.Format("{0}:{1:00}", Math.Abs(time.Minutes), Math.Abs(time.Seconds));
            else if (Math.Abs(time.TotalMinutes) < 60)
                result = Math.Abs(time.Minutes) + " mins";
            else if (Math.Abs(time.TotalHours) < 24)
                result = string.Format("{0} hours", Math.Abs(time.Hours));
            else
                result = string.Format("{0} days", Math.Abs((int)time.TotalDays));
            if (time.Ticks < 0)
                return "-" + result;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
