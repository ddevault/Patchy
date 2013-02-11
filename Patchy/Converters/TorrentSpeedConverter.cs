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
            int speed = (int)value;
            if (speed < 1048576)
                return (speed / 1024.0).ToString("0.00") + " kb/s";
            return (speed / 1048576.0).ToString("0.00") + " mb/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value; // TODO
        }
    }
}
