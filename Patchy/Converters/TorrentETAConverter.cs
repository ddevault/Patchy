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

            if (time == TimeSpan.MaxValue || time == TimeSpan.MinValue)
                return "∞";
            if (time.TotalSeconds < 60)
                return (int)time.TotalSeconds + " secs";
            if (time.TotalSeconds < (60 * 5))
                return string.Format("{0}:{1:00}", time.Minutes, time.Seconds);
            if (time.TotalMinutes < 60)
                return time.Minutes + " mins";
            if (time.TotalHours < 24)
                return string.Format("{0}:{1:00}", time.Hours, time.Minutes);
            return string.Format("{0} days", (int)time.TotalDays);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
