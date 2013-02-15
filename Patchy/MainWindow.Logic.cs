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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading.Tasks;

namespace Patchy
{
    public partial class MainWindow
    {
        private ClientManager Client { get; set; }
        private Timer Timer { get; set; }
        private SettingsManager SettingsManager { get; set; }

        [DllImport("user32.dll")]
        static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private void Initialize()
        {
            Client.Initialize();
            SettingsManager = new SettingsManager();
            SettingsManager.Initialize();
            // Load prior session
            if (File.Exists(SettingsManager.FastResumePath))
            {
                // Load on another thread because it takes some time
                Task.Factory.StartNew(() =>
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
                            periodicTorrent.CacheFilePath = torrent;
                        }
                    });
            }
            Timer = new Timer(o => Dispatcher.Invoke(new Action(PeriodicUpdate)),
                null, 1000, 1000);
        }

        public void AddTorrent(MagnetLink link, string path, bool suppressMessages = false)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(link.Name));
            var cache = Path.Combine(
                    SettingsManager.TorrentCachePath,
                    ClientManager.CleanFileName(name) + ".torrent");
            var wrapper = new TorrentWrapper(link, path, new TorrentSettings(), cache);
            if (Client.Torrents.Any(t => t.Torrent.InfoHash == wrapper.InfoHash))
            {
                if (!suppressMessages)
                    MessageBox.Show(name + " has already been added.", "Error");
                return;
            }
            var periodic = Client.AddTorrent(wrapper);
            File.WriteAllText(Path.Combine(
                    SettingsManager.TorrentCachePath,
                    ClientManager.CleanFileName(name) + ".info"),
                    path);
            periodic.CacheFilePath = cache;
        }

        public void AddTorrent(Torrent torrent, string path, bool suppressMessages = false)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            var wrapper = new TorrentWrapper(torrent, path, new TorrentSettings());
            if (Client.Torrents.Any(t => t.Torrent.InfoHash == wrapper.InfoHash))
            {
                if (!suppressMessages)
                    MessageBox.Show(torrent.Name + " has already been added.", "Error");
                return;
            }
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
                if (torrent.Torrent.Complete && !torrent.CompletedOnAdd && !torrent.NotifiedComplete && torrent.State == TorrentState.Seeding)
                {
                    NotifyIcon.ShowBalloonTip(5000, "Download Complete",
                        torrent.Name, System.Windows.Forms.ToolTipIcon.Info);
                    torrent.NotifiedComplete = true;
                    BalloonTorrent = torrent;
                    FlashWindow(new WindowInteropHelper(this).Handle, true);
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

        public void HandleArguments(string[] args)
        {
            if (args.Length == 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Visibility = Visibility.Visible;
                        Activate();
                    }));
                return;
            }
            if (args[0] == "--minimized")
            {
                Visibility = Visibility.Hidden;
                ShowInTaskbar = false;
                ShowActivated = false;
                WindowStyle = WindowStyle.None;
                Width = Height = 0;
                return;
            }
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    Visibility = Visibility.Visible;
                    Activate();
                    // TODO: Prompt for download location
                    try
                    {
                        var magnetLink = new MagnetLink(args[0]);
                        AddTorrent(magnetLink, Path.Combine(SettingsManager.DefaultDownloadLocation,
                            ClientManager.CleanFileName(HttpUtility.HtmlDecode(HttpUtility.UrlDecode(magnetLink.Name)))));
                        FlashWindow(new WindowInteropHelper(this).Handle, true);
                    }
                    catch
                    {
                        var torrent = Torrent.Load(args[0]);
                        AddTorrent(torrent, Path.Combine(SettingsManager.DefaultDownloadLocation,
                            ClientManager.CleanFileName(torrent.Name)));
                        FlashWindow(new WindowInteropHelper(this).Handle, true);
                    }
                }));
        }
    }
}
