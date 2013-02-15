using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Patchy
{
    public class SettingsManager : INotifyPropertyChanged
    {
        public SettingsManager()
        {
            // Default settings
            DefaultDownloadLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            RssFeeds = new ObservableCollection<RssFeed>();
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

        public ObservableCollection<RssFeed> RssFeeds { get; set; }
        private string _DefaultDownloadLocation;
        public string DefaultDownloadLocation 
        {
            get { return _DefaultDownloadLocation; }
            set
            {
                _DefaultDownloadLocation = value;
                OnPropertyChanged("DefaultDownloadLocation");
            }
        }

        private int _MinutesBetweenRssUpdates;
        public int MinutesBetweenRssUpdates
        {
            get { return _MinutesBetweenRssUpdates; }
            set
            {
                _MinutesBetweenRssUpdates = value;
                OnPropertyChanged("MinutesBetweenRssUpdates");
            }
        }

        protected internal virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
