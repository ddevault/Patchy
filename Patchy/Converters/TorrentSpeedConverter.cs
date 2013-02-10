using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Patchy.Converters
{
    public class TorrentSpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int speed = (int)value; // In bits per second, I think
            if (speed < 8388608)
                return (speed / 8192.0).ToString("0.00") + " kb/s";
            if (speed < 8589934592)
                return (speed / 8388608.0).ToString("0.00") + " mb/s";
            return (speed / 8589934592.0).ToString("0.00") + " gb/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value; // TODO
        }
    }
}
