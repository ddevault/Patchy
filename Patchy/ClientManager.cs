using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.ComponentModel;

namespace Patchy
{
    public class ClientManager
    {
        private SettingsManager SettingsManager { get; set; }

        public void Initialize(SettingsManager settingsManager)
        {
            SettingsManager = settingsManager;
            Torrents = new ObservableCollection<PeriodicTorrent>();
            Torrents.CollectionChanged += Torrents_CollectionChanged;

            var port = SettingsManager.IncomingPort;
            if (SettingsManager.UseRandomPort)
                port = new Random().Next(1, 65536);
            var settings = new EngineSettings(SettingsManager.DefaultDownloadLocation, port);

            settings.PreferEncryption = SettingsManager.EncryptionSettings != EncryptionTypes.PlainText; // Always prefer encryption unless it's disabled
            settings.AllowedEncryption = SettingsManager.EncryptionSettings;
            Client = new ClientEngine(settings);
            Client.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
            if (SettingsManager.EnableDHT)
            {
                var listener = new DhtListener(new IPEndPoint(IPAddress.Any, port));
                var dht = new DhtEngine(listener);
                Client.RegisterDht(dht);
                listener.Start();
                if (File.Exists(SettingsManager.DhtCachePath))
                    dht.Start(File.ReadAllBytes(SettingsManager.DhtCachePath));
                else
                    dht.Start();
            }
            SettingsManager.PropertyChanged += SettingsManager_PropertyChanged;
        }

        void SettingsManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IncomingPort":
                    Client.Listener.ChangeEndpoint(new IPEndPoint(IPAddress.Any, SettingsManager.IncomingPort));
                    break;
                case "MapWithUPnP":
                    // TODO: UPnP
                    break;
                case "MaxUploadSpeed":
                    Client.Settings.GlobalMaxUploadSpeed = SettingsManager.MaxUploadSpeed * 1024;
                    break;
                case "MaxDownloadSpeed":
                    Client.Settings.GlobalMaxDownloadSpeed = SettingsManager.MaxDownloadSpeed * 1024;
                    break;
                case "MaxConnections":
                    Client.Settings.GlobalMaxConnections = SettingsManager.MaxConnections;
                    break;
                case "EnableDHT":
                    if (SettingsManager.EnableDHT)
                    {
                        var port = SettingsManager.IncomingPort;
                        if (SettingsManager.UseRandomPort)
                            port = new Random().Next(1, 65536);
                        var listener = new DhtListener(new IPEndPoint(IPAddress.Any, port));
                        var dht = new DhtEngine(listener);
                        Client.RegisterDht(dht);
                        listener.Start();
                        if (File.Exists(SettingsManager.DhtCachePath))
                            dht.Start(File.ReadAllBytes(SettingsManager.DhtCachePath));
                        else
                            dht.Start();
                    }
                    else
                        Client.DhtEngine.Stop();
                    break;
                case "EncryptionSettings":
                    Client.Settings.AllowedEncryption = SettingsManager.EncryptionSettings;
                    break;
            }
        }

        void Torrents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    for (int i = 0; i < Torrents.Count; i++)
                        Torrents[i].Index = i + 1;
                }));
        }

        public PeriodicTorrent AddTorrent(TorrentWrapper torrent)
        {
            var periodicTorrent = new PeriodicTorrent(torrent);
            Task.Factory.StartNew(() =>
                {
                    Client.Register(torrent);
                    if (SettingsManager.StartTorrentsImmediately)
                        torrent.Start();
                });
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Torrents.Add(periodicTorrent)));
            return periodicTorrent;
        }

        public PeriodicTorrent LoadFastResume(FastResume resume, TorrentWrapper torrent)
        {
            var periodicTorrent = new PeriodicTorrent(torrent);
            Task.Factory.StartNew(() =>
                {
                    torrent.LoadFastResume(resume);
                    Client.Register(torrent);
                    if (SettingsManager.StartTorrentsImmediately)
                        torrent.Start();
                });
            Application.Current.Dispatcher.BeginInvoke(new Action(() => Torrents.Add(periodicTorrent)));
            return periodicTorrent;
        }

        public void RemoveTorrent(PeriodicTorrent torrent)
        {
            torrent.Torrent.TorrentStateChanged += (s, e) =>
                {
                    if (e.NewState == TorrentState.Stopped)
                    {
                        torrent.Torrent.Stop();
                        try
                        {
                            Client.Unregister(torrent.Torrent);
                        }
                        catch { } // TODO: See if we need to do more
                        // Delete cache
                        if (File.Exists(torrent.CacheFilePath))
                            File.Delete(torrent.CacheFilePath);
                        if (File.Exists(Path.Combine(SettingsManager.TorrentCachePath,
                            Path.GetFileNameWithoutExtension(torrent.CacheFilePath) + ".info")))
                        {
                            File.Delete(Path.Combine(SettingsManager.TorrentCachePath,
                                Path.GetFileNameWithoutExtension(torrent.CacheFilePath) + ".info"));
                        }
                        torrent.Torrent.Dispose();
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => Torrents.Remove(torrent)));
                    }
                };
            Task.Factory.StartNew(() => torrent.Torrent.Stop());
        }

        public void RemoveTorrentAndFiles(PeriodicTorrent torrent)
        {
            torrent.Torrent.TorrentStateChanged += (s, e) =>
            {
                if (e.NewState == TorrentState.Stopped)
                {
                    Client.Unregister(torrent.Torrent);
                    // Delete cache
                    if (File.Exists(torrent.CacheFilePath))
                        File.Delete(torrent.CacheFilePath);
                    if (File.Exists(Path.Combine(SettingsManager.TorrentCachePath,
                        Path.GetFileNameWithoutExtension(torrent.CacheFilePath) + ".info")))
                    {
                        File.Delete(Path.Combine(SettingsManager.TorrentCachePath,
                            Path.GetFileNameWithoutExtension(torrent.CacheFilePath) + ".info"));
                    }
                    // Delete files
                    Directory.Delete(torrent.Torrent.SavePath, true);

                    torrent.Torrent.Dispose();
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => Torrents.Remove(torrent)));
                }
            };
            Task.Factory.StartNew(() => torrent.Torrent.Stop());
        }

        public void Shutdown()
        {
            if (SettingsManager.EnableDHT)
            {
                Client.DhtEngine.Stop();
                File.WriteAllBytes(SettingsManager.DhtCachePath, Client.DhtEngine.SaveNodes());
            }
            Client.Dispose();
        }

        public PeriodicTorrent GetTorrent(Torrent torrent)
        {
            return Torrents.FirstOrDefault(t => t.Torrent.Torrent == torrent);
        }

        public ObservableCollection<PeriodicTorrent> Torrents { get; set; }

        private static ClientEngine Client { get; set; }

        public static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), String.Empty));
        }
    }

    public class TorrentWrapper : TorrentManager
    {
        public string Name { get; private set; }
        public long Size { get; private set; }
        public bool IsMagnet { get; set; }

        public TorrentWrapper(Torrent torrent, string savePath, TorrentSettings settings)
            : base(torrent, savePath, settings)
        {
            Name = torrent.Name;
            Size = torrent.Size;
            IsMagnet = false;
        }

        public TorrentWrapper(MagnetLink magnetLink, string savePath, TorrentSettings settings, string torrentSave)
            : base(magnetLink, savePath, settings, torrentSave)
        {
            Name = magnetLink.Name;
            Name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(Name));
            Size = -1;
            IsMagnet = true;
        }
    }
}
