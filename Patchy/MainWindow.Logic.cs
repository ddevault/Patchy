using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace Patchy
{
    public partial class MainWindow
    {
        private ClientManager Client { get; set; }
        private Timer Timer { get; set; }
        private SettingsManager SettingsManager { get; set; }

        private void Initialize()
        {
            Client.Initialize();
            SettingsManager = new SettingsManager();
            SettingsManager.Initialize();
            // Load prior session
            if (File.Exists(SettingsManager.FastResumePath))
            {
                // Load on another thread because it takes some time
                new Thread(() =>
                    {
                        var resume = BEncodedValue.Decode<BEncodedDictionary>(
                            File.ReadAllBytes(SettingsManager.FastResumePath));
                        var torrents = Directory.GetFiles(SettingsManager.TorrentCachePath, "*.torrent");
                        foreach (var torrent in torrents)
                        {
                            var path = File.ReadAllText(Path.Combine(
                                SettingsManager.TorrentCachePath, Path.GetFileNameWithoutExtension(torrent))
                                                        + ".info");
                            var wrapper = new TorrentWrapper(Torrent.Load(torrent), path, new TorrentSettings());
                            Dispatcher.Invoke(new Action(() =>
                            {
                                PeriodicTorrent periodicTorrent;
                                if (resume.ContainsKey(wrapper.Torrent.InfoHash.ToHex()))
                                {
                                    periodicTorrent = Client.LoadFastResume(
                                        new FastResume((BEncodedDictionary)resume[wrapper.Torrent.InfoHash.ToHex()]), wrapper);
                                }
                                else
                                {
                                    periodicTorrent = Client.AddTorrent(wrapper);
                                }
                                periodicTorrent.CompletedOnAdd = wrapper.Complete;
                                periodicTorrent.CacheFilePath = torrent;
                            }));
                        }
                    }).Start();
            }
            Timer = new Timer(o => Dispatcher.Invoke(new Action(PeriodicUpdate)),
                null, 1000, 1000);
        }

        public void AddTorrent(MagnetLink link, string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(link.Name));
            var cache = Path.Combine(
                    SettingsManager.TorrentCachePath,
                    ClientManager.CleanFileName(name) + ".torrent");
            var wrapper = new TorrentWrapper(link, path, new TorrentSettings(), cache);
            var periodic = Client.AddTorrent(wrapper);
            File.WriteAllText(Path.Combine(
                    SettingsManager.TorrentCachePath,
                    ClientManager.CleanFileName(name) + ".info"),
                    path);
            periodic.CacheFilePath = cache;
        }

        public void AddTorrent(Torrent torrent, string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var wrapper = new TorrentWrapper(torrent, path, new TorrentSettings());
            var periodic = Client.AddTorrent(wrapper);
            // Save torrent to cache
            var cache = Path.Combine(SettingsManager.TorrentCachePath, Path.GetFileName(torrent.TorrentPath));
            if (File.Exists(cache))
                File.Delete(cache);
            File.Copy(torrent.TorrentPath, cache);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(cache),
                Path.GetFileNameWithoutExtension(cache)) + ".info", path);
            periodic.CacheFilePath = cache;
        }

        private void PeriodicUpdate()
        {
            CheckMagnetLinks();
            foreach (var torrent in Client.Torrents)
            {
                torrent.Update();
                if (torrent.Torrent.Complete && !torrent.CompletedOnAdd && !torrent.NotifiedComplete)
                {
                    NotifyIcon.ShowBalloonTip(5000, "Download Complete",
                        torrent.Name, System.Windows.Forms.ToolTipIcon.Info);
                    torrent.NotifiedComplete = true;
                    BalloonTorrent = torrent;
                }
            }
            UpdateNotifyIcon();
        }

        private void CheckMagnetLinks()
        {
            var visibility = Visibility.Collapsed;
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (IgnoredClipboardValue != text)
                {
                    if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                    {
                        var uri = new Uri(text);
                        if (uri.Scheme == "magnet")
                        {
                            try
                            {
                                var link = new MagnetLink(text);
                                if (!Client.Torrents.Any(t => t.Torrent.InfoHash == link.InfoHash))
                                {
                                    quickAddName.Text = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(link.Name));
                                    visibility = Visibility.Visible;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            quickAddGrid.Visibility = visibility;
        }
    }
}
