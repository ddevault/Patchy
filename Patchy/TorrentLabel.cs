using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Patchy
{
    public class TorrentLabel : IComparable
    {
        public TorrentLabel()
        {
        }

        public TorrentLabel(string name, string color)
        {
            Name = name;
            Color = color;
        }

        [JsonIgnore]
        public Brush Brush
        {
            get { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Color)); }
        }

        [JsonIgnore]
        public Brush ForegroundBrush
        {
            get { return new SolidColorBrush(GetContrastingColor((Color)ColorConverter.ConvertFromString(Color))); }
        }

        public static Color GetContrastingColor(Color color)
        {
            double hue, saturation, value;
            ColorToHSV(color, out hue, out saturation, out value);
            if (value <= 0.5)
                return Colors.White;
            return Colors.Black;
        }

        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = Math.Atan2(Math.Sqrt(3) * (color.G - color.B), 2 * color.R - color.G - color.B);
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return System.Windows.Media.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return System.Windows.Media.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return System.Windows.Media.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return System.Windows.Media.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return System.Windows.Media.Color.FromArgb(255, t, p, v);
            else
                return System.Windows.Media.Color.FromArgb(255, v, p, q);
        }

        public string Color { get; set; }
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Name.CompareTo((obj as TorrentLabel).Name);
        }
    }
}
