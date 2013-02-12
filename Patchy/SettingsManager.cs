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
            DefaultDownloadLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }

        public static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".patchy");
            }
        }

        public static string FastResumePath
        {
            get
            {
                return Path.Combine(SettingsPath, "fastresume");
            }
        }

        public static string TorrentCachePath
        {
            get { return Path.Combine(SettingsPath, "torrentcache"); }
        }

        public void Initialize()
        {
            if (!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);
            if (!Directory.Exists(TorrentCachePath))
                Directory.CreateDirectory(TorrentCachePath);
        }

        public string DefaultDownloadLocation { get; set; }
    }
}
