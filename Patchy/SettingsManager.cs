using System;
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
            SetToDefaults();
        }

        public static string SettingsPath
        {
#if !PORTABLE
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".patchy"); }
#else
            get { return "data"; }
#endif
        }

        public static string SettingsFile
        {
            get { return Path.Combine(SettingsPath, "settings"); }
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

        public static void Initialize()
        {
            if (!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);
            if (!Directory.Exists(TorrentCachePath))
                Directory.CreateDirectory(TorrentCachePath);
        }

        public void SetToDefaults()
        {
            // General
            SaveSession = true;
            AutoUpdate = true;
            PromptForSaveOnShellLinks = true;
            DeleteTorrentsAfterAdd = false;
            SeedOnlyWhenIdle = false;

            // Downloads
            DefaultDownloadLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
            PostCompletionDestination = string.Empty;
            WatchedDirectories = new WatchedDirectory[0];

            // Connection
            IncomingPort = 22239;
            UseRandomPort = false;
            MapWithUPnP = false;
            MaxUploadSpeed = 0;
            MaxDownloadSpeed = 0;
            MaxConnections = 500;
            MaxConnectionsPerTorrent = 100;
            UploadSlotsPerTorrent = 4;

            // Proxy
            ProxyAddress = null;
            EnableProxyAuthentication = false;
            ProxyUsername = null;
            ProxyPassword = null;

            // BitTorrent
            EnableDHT = true;
            EncryptionSettings = EncryptionTypes.RC4Header | EncryptionTypes.RC4Full;
            HoursToSeed = 0;
            TargetSeedRatio = 0;
            
            // Interface
            ShowTrayIcon = true;
            CloseToSystemTray = true;
            MinimizeToSystemTray = false;
            ShowNotificationOnCompletion = true;
            ConfirmExitWhenActive = true;
            ConfirmTorrentRemoval = true;
            WarnOnDangerousFiles = true;
            WarnWhenRunningAsAdministrator = true;
            StartTorrentsImmediately = true;
            DoubleClickSeeding = DoubleClickAction.OpenFolder;
            DoubleClickDownloading = DoubleClickAction.OpenFolder;

            // Completion
            TorrentCompletionCommand = string.Empty;

            // RSS
            RssFeeds = new RssFeed[0];
            MinutesBetweenRssUpdates = 5;

            // Other
            RecentDownloadLocations = new string[0];
            Labels = new TorrentLabel[0];
            TotalBytesDownloaded = TotalBytesUploaded = 0;
            WindowWidth = WindowHeight = -1;
            Maximized = false;
        }

        public void ForcePropertyUpdate()
        {
            // Calls OnPropertyChanged for all properties
            var properties = GetType().GetProperties();
            foreach (var property in properties)
                OnPropertyChanged(property.Name);
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

        private bool _PromptForSaveOnShellLinks;
        public bool PromptForSaveOnShellLinks
        {
            get { return _PromptForSaveOnShellLinks; }
            set
            {
                _PromptForSaveOnShellLinks = value;
                OnPropertyChanged("PromptForSaveOnShellLinks");
            }
        }

        private bool _DeleteTorrentsAfterAdd;
        public bool DeleteTorrentsAfterAdd
        {
            get { return _DeleteTorrentsAfterAdd; }
            set
            {
                _DeleteTorrentsAfterAdd = value;
                OnPropertyChanged("DeleteTorrentsAfterAdd");
            }
        }

        private bool _SeedOnlyWhenIdle;
        public bool SeedOnlyWhenIdle
        {
            get { return _SeedOnlyWhenIdle; }
            set
            {
                _SeedOnlyWhenIdle = value;
                OnPropertyChanged("SeedOnlyWhenIdle");
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

        private WatchedDirectory[] _WatchedDirectories;
        public WatchedDirectory[] WatchedDirectories
        {
            get { return _WatchedDirectories; }
            set
            {
                _WatchedDirectories = value;
                OnPropertyChanged("WatchedDirectories");
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

        private int _MaxUploadSpeed;
        public int MaxUploadSpeed
        {
            get { return _MaxUploadSpeed; }
            set
            {
                _MaxUploadSpeed = value;
                OnPropertyChanged("MaxUploadSpeed");
            }
        }

        private int _MaxDownloadSpeed;
        public int MaxDownloadSpeed
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

        #region Proxy

        private string _ProxyAddress;
        public string ProxyAddress
        {
            get { return _ProxyAddress; }
            set
            {
                _ProxyAddress = value;
                OnPropertyChanged("ProxyAddress");
            }
        }

        private string _ProxyUsername;
        public string ProxyUsername
        {
            get { return _ProxyUsername; }
            set
            {
                _ProxyUsername = value;
                OnPropertyChanged("ProxyUsername");
            }
        }

        private string _ProxyPassword;
        public string ProxyPassword
        {
            get { return _ProxyPassword; }
            set
            {
                _ProxyPassword = value;
                OnPropertyChanged("ProxyPassword");
            }
        }

        private bool _EnableProxyAuthentication;
        public bool EnableProxyAuthentication
        {
            get { return _EnableProxyAuthentication; }
            set
            {
                _EnableProxyAuthentication = value;
                OnPropertyChanged("EnableProxyAuthentication");
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

        private bool _WarnOnDangerousFiles;
        public bool WarnOnDangerousFiles
        {
            get { return _WarnOnDangerousFiles; }
            set
            {
                _WarnOnDangerousFiles = value;
                OnPropertyChanged("WarnOnDangerousFiles");
            }
        }

        private bool _WarnWhenRunningAsAdministrator;
        public bool WarnWhenRunningAsAdministrator
        {
            get { return _WarnWhenRunningAsAdministrator; }
            set
            {
                _WarnWhenRunningAsAdministrator = value;
                OnPropertyChanged("WarnWhenRunningAsAdministrator");
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

        // Settings that are edited outside of the preferences window
        #region Non-preferences

        public RssFeed[] RssFeeds { get; set; }
        public string[] RecentDownloadLocations { get; set; }

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

        public TorrentLabel[] Labels { get; set; }

        public long TotalBytesUploaded { get; set; }
        public long TotalBytesDownloaded { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public bool Maximized { get; set; }

        #endregion

        protected internal virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
