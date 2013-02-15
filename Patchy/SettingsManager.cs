﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MonoTorrent.Client.Encryption;

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

        #region General

        private bool _SaveSession;
        public bool SaveSession
        {
            get { return _SaveSession; }
            set
            {
                _SaveSession = value;
                OnPropertyChanged("SaveSession");
            }
        }

        private bool _AutoUpdate;
        public bool AutoUpdate
        {
            get { return _AutoUpdate; }
            set
            {
                _AutoUpdate = value;
                OnPropertyChanged("AutoUpdate");
            }
        }

        #endregion

        #region Downloads

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

        private string _PostCompletionDestination;
        public string PostCompletionDestination
        {
            get { return _PostCompletionDestination; }
            set
            {
                _PostCompletionDestination = value;
                OnPropertyChanged("PostCompletionDestination");
            }
        }

        private string _AutomaticAddDirectory;
        public string AutomaticAddDirectory
        {
            get { return _AutomaticAddDirectory; }
            set
            {
                _AutomaticAddDirectory = value;
                OnPropertyChanged("AutomaticAddDirectory");
            }
        }

        #endregion

        #region Connections

        private int _IncomingPort;
        public int IncomingPort
        {
            get { return _IncomingPort; }
            set
            {
                _IncomingPort = value;
                OnPropertyChanged("IncomingPort");
            }
        }

        private bool _UseRandomPort;
        public bool UseRandomPort
        {
            get { return _UseRandomPort; }
            set
            {
                _UseRandomPort = value;
                OnPropertyChanged("UseRandomPort");
            }
        }

        private bool _MapWithUPnP;
        public bool MapWithUPnP
        {
            get { return _MapWithUPnP; }
            set
            {
                _MapWithUPnP = value;
                OnPropertyChanged("MapWithUPnP");
            }
        }

        private double _MaxUploadSpeed;
        public double MaxUploadSpeed
        {
            get { return _MaxUploadSpeed; }
            set
            {
                _MaxUploadSpeed = value;
                OnPropertyChanged("MaxUploadSpeed");
            }
        }

        private double _MaxDownloadSpeed;
        public double MaxDownloadSpeed
        {
            get { return _MaxDownloadSpeed; }
            set
            {
                _MaxDownloadSpeed = value;
                OnPropertyChanged("MaxDownloadSpeed");
            }
        }

        private int _MaxConnections;
        public int MaxConnections
        {
            get { return _MaxConnections; }
            set
            {
                _MaxConnections = value;
                OnPropertyChanged("MaxConnections");
            }
        }

        private int _MaxConnectionsPerTorrent;
        public int MaxConnectionsPerTorrent
        {
            get { return _MaxConnectionsPerTorrent; }
            set
            {
                _MaxConnectionsPerTorrent = value;
                OnPropertyChanged("MaxConnectionsPerTorrent");
            }
        }

        private int _UploadSlotsPerTorrent;
        public int UploadSlotsPerTorrent
        {
            get { return _UploadSlotsPerTorrent; }
            set
            {
                _UploadSlotsPerTorrent = value;
                OnPropertyChanged("UploadSlotsPerTorrent");
            }
        }

        #endregion

        #region BitTorrent

        private bool _EnableDHT;
        public bool EnableDHT
        {
            get { return _EnableDHT; }
            set
            {
                _EnableDHT = value;
                OnPropertyChanged("EnableDHT");
            }
        }

        private EncryptionTypes _EncryptionSettings;
        public EncryptionTypes EncryptionSettings
        {
            get { return _EncryptionSettings; }
            set
            {
                _EncryptionSettings = value;
                OnPropertyChanged("EncryptionSettings");
            }
        }

        private int _HoursToSeed;
        public int HoursToSeed
        {
            get { return _HoursToSeed; }
            set
            {
                _HoursToSeed = value;
                OnPropertyChanged("HoursToSeed");
            }
        }

        private double _TargetSeedRatio;
        public double TargetSeedRatio
        {
            get { return _TargetSeedRatio; }
            set
            {
                _TargetSeedRatio = value;
                OnPropertyChanged("TargetSeedRatio");
            }
        }

        #endregion

        #region Interface

        private bool _ShowTrayIcon;
        public bool ShowTrayIcon
        {
            get { return _ShowTrayIcon; }
            set
            {
                _ShowTrayIcon = value;
                OnPropertyChanged("ShowTrayIcon");
            }
        }

        private bool _CloseToSystemTray;
        public bool CloseToSystemTray
        {
            get { return _CloseToSystemTray; }
            set
            {
                _CloseToSystemTray = value;
                OnPropertyChanged("CloseToSystemTray");
            }
        }

        private bool _MinimizeToSystemTray;
        public bool MinimizeToSystemTray
        {
            get { return _MinimizeToSystemTray; }
            set
            {
                _MinimizeToSystemTray = value;
                OnPropertyChanged("MinimizeToSystemTray");
            }
        }

        private bool _ShowNotificationOnCompletion;
        public bool ShowNotificationOnCompletion
        {
            get { return _ShowNotificationOnCompletion; }
            set
            {
                _ShowNotificationOnCompletion = value;
                OnPropertyChanged("ShowNotificationOnCompletion");
            }
        }

        private bool _ConfirmExitWhenActive;
        public bool ConfirmExitWhenActive
        {
            get { return _ConfirmExitWhenActive; }
            set
            {
                _ConfirmExitWhenActive = value;
                OnPropertyChanged("ConfirmExitWhenActive");
            }
        }

        private bool _ConfirmTorrentRemoval;
        public bool ConfirmTorrentRemoval
        {
            get { return _ConfirmTorrentRemoval; }
            set
            {
                _ConfirmTorrentRemoval = value;
                OnPropertyChanged("ConfirmTorrentRemoval");
            }
        }

        private bool _StartTorrentsImmediately;
        public bool StartTorrentsImmediately
        {
            get { return _StartTorrentsImmediately; }
            set
            {
                _StartTorrentsImmediately = value;
                OnPropertyChanged("StartTorrentsImmediately");
            }
        }

        private DoubleClickAction _DoubleClickSeeding;
        public DoubleClickAction DoubleClickSeeding
        {
            get { return _DoubleClickSeeding; }
            set
            {
                _DoubleClickSeeding = value;
                OnPropertyChanged("DoubleClickSeeding");
            }
        }

        private DoubleClickAction _DoubleClickDownloading;
        public DoubleClickAction DoubleClickDownloading
        {
            get { return _DoubleClickDownloading; }
            set
            {
                _DoubleClickDownloading = value;
                OnPropertyChanged("DoubleClickDownloading");
            }
        }

        #endregion

        #region Completion

        private string _TorrentCompletionCommand;
        public string TorrentCompletionCommand
        {
            get { return _TorrentCompletionCommand; }
            set
            {
                _TorrentCompletionCommand = value;
                OnPropertyChanged("TorrentCompletionCommand");
            }
        }

        #endregion

        protected internal virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
