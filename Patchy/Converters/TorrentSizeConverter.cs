using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Patchy.Converters
{
    public class TorrentSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long size = (long)value;
            if (size == -1)
                return "n/a";
            if (size < 1048576)
                return (size / 1024.0).ToString("0.00") + " KB";
            if (size < 1073741824)
                return (size / 1048576.0).ToString("0.00") + " MB";
            return (size / 1073741824.0).ToString("0.00") + " GB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value; // TODO
        }
    }
}
