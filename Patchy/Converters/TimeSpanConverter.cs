using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Patchy.Converters
{
    public class TimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var span = (TimeSpan)value;
            StringBuilder result = new StringBuilder();
            if (span.TotalDays >= 1)
                result.AppendFormat("{0} days, ", (int)span.TotalDays);
            result.AppendFormat("{0}:{1:00}:{2:00}", span.Hours, span.Minutes, span.Seconds);
            return result.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
