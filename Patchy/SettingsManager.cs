using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Patchy
{
    public class SettingsManager
    {
        public SettingsManager()
        {
            // Default settings
            DefaultDownloadLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            RssFeeds = new List<RssFeed>();
            MinutesBetweenRssUpdates = 5;
        }

        public static string SettingsPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".patchy"); }
        }

        public static string FastResumePath
        {
            get { return Path.Combine(SettingsPath, "fastresume"); }
        }

        public static string TorrentCachePath
        {
            get { return Path.Combine(SettingsPath, "torrentcache"); }
        }

        public static string DhtCachePath
        {
            get { return Path.Combine(SettingsPath, "dhtcache"); }
        }

        public void Initialize()
        {
            if (!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);
            if (!Directory.Exists(TorrentCachePath))
                Directory.CreateDirectory(TorrentCachePath);
        }

        public void Save()
        {
            // TODO
        }

        public string DefaultDownloadLocation { get; set; }
        public List<RssFeed> RssFeeds { get; set; }
        public int MinutesBetweenRssUpdates { get; set; }
    }
}
