using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Patchy
{
    public class WatchedDirectory
    {
        public override string ToString()
        {
            return System.IO.Path.GetFileName(Path);
        }

        public string Path { get; set; }
        public TorrentLabel Label { get; set; }
    }
}
