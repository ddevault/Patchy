using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Patchy
{
    public class TorrentLabel
    {
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

        public string Color { get; set; }
        public string Name { get; set; }
    }
}
