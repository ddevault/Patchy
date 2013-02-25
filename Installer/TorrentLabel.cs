using System;
using System.Collections.Generic;
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

        public string Color { get; set; }
        public string Foreground { get; set; }
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Name.CompareTo((obj as TorrentLabel).Name);
        }
    }
}
