using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Patchy.Converters
{
    public class TorrentETADateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var span = (TimeSpan)value;
            if (span == TimeSpan.MinValue || span == TimeSpan.MaxValue)
                return "n/a";
            return (DateTime.Now + span).ToShortTimeString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
