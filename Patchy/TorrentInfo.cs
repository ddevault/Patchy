using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Common;

namespace Patchy
{
    public class TorrentInfo
    {
        public TorrentInfo()
        {
            ElapsedTime = TimeSpan.Zero;
            CompletionTime = DateTime.MinValue;
        }

        public TorrentLabel Label { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public DateTime CompletionTime { get; set; }
        public long TotalDownloaded { get; set; }
        public long TotalUploaded { get; set; }
        public string Path { get; set; }
        public Priority[] FilePriority { get; set; }
    }
}
