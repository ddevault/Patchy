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

namespace Patchy
{
    public class ClientManager
    {
        public void Initialize()
        {
            Torrents = new ObservableCollection<PeriodicTorrent>();
            Torrents.CollectionChanged += Torrents_CollectionChanged;

            // TODO: Customize most of these settings
            var settings = new EngineSettings(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), 22239);
            settings.PreferEncryption = true;
            settings.AllowedEncryption = EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;
            Client = new ClientEngine(settings);
            Client.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, 22239));
            var listener = new DhtListener(new IPEndPoint(IPAddress.Any, 22239));
            var dht = new DhtEngine(listener);
            Client.RegisterDht(dht);
            listener.Start();
            if (File.Exists(SettingsManager.DhtCachePath))
                dht.Start(File.ReadAllBytes(SettingsManager.DhtCachePath));
            else
                dht.Start();
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
            Client.DhtEngine.Stop();
            File.WriteAllBytes(SettingsManager.DhtCachePath, Client.DhtEngine.SaveNodes());
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
