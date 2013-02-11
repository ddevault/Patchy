using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon NotifyIcon { get; set; }
        private bool AllowClose = false;
        private ClientManager Client { get; set; }
        private Timer Timer { get; set; }
        private SettingsManager SettingsManager { get; set; }
        private string IgnoredClipboardValue { get; set; }
        private PeriodicTorrent BalloonTorrent { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            NotifyIcon = new System.Windows.Forms.NotifyIcon();
            NotifyIcon.Text = "Patchy";
            NotifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(
                new Uri("pack://application:,,,/Patchy;component/Images/patchy.ico")).Stream);
            NotifyIcon.Visible = true;
            NotifyIcon.DoubleClick += NotifyIcon_Click;
            NotifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;
            var menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("Add Torrent", (s, e) => ExecuteNew(null, null));
            menu.MenuItems.Add("Exit", (s, e) =>
            {
                NotifyIcon.Dispose();
                AllowClose = true;
                Close();
            });
            NotifyIcon.ContextMenu = menu;
            Client = new ClientManager();
            Client.Initialize();
            Initialize();
            Timer = new Timer(o => // Used for updating periodic objects and the notify icon
                {
                    Dispatcher.Invoke(() =>
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
                            if (Client.Torrents.Count == 0)
                                NotifyIcon.Text = "Patchy";
                            else if (Client.Torrents.Any(t => !t.Complete))
                            {
                                NotifyIcon.Text = string.Format(
                                    "Patchy - {0} torrent{3}, {1} downloading at {2}%",
                                    Client.Torrents.Count,
                                    Client.Torrents.Count(t => !t.Complete),
                                    (int)(Client.Torrents.Where(t => !t.Complete).Select(t => t.Progress)
                                        .Aggregate((t, n) => t + n) / Client.Torrents.Count(t => !t.Complete)),
                                    Client.Torrents.Count == 1 ? "" : "s");
                            }
                            else
                            {
                                NotifyIcon.Text = string.Format(
                                    "Patchy - Seeding {0} torrent{1}",
                                    Client.Torrents.Count,
                                    Client.Torrents.Count == 1 ? "" : "s");
                            }
                        });
                }, null, 1000, 1000);
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
                                quickAddName.Text = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(link.Name));
                                visibility = Visibility.Visible;
                            }
                            catch { }
                        }
                    }
                }
            }
            quickAddGrid.Visibility = visibility;
        }

        private void UpdateTorrentGrid()
        {
            torrentGrid.Items.Clear();
            foreach (var torrent in Client.Torrents)
                torrentGrid.Items.Add(torrent);
        }

        void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Hidden)
            {
                Visibility = Visibility.Visible;
                Focus();
            }
            else
                Visibility = Visibility.Hidden;
        }

        void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Process.Start("explorer", BalloonTorrent.Torrent.SavePath);
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (AllowClose)
                return;
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow(SettingsManager.DefaultDownloadLocation);
            if (window.ShowDialog().GetValueOrDefault(false))
            {
                TorrentWrapper torrent;
                // TODO: Allow for customized TorrentSettings
                if (window.IsMagnet)
                {
                    torrent = new TorrentWrapper(window.MagnetLink, window.DestinationPath,
                        new TorrentSettings(), Path.GetTempFileName());
                }
                else
                {
                    torrent = new TorrentWrapper(window.Torrent, window.DestinationPath,
                        new TorrentSettings());
                }
                Client.AddTorrent(torrent);
                UpdateTorrentGrid();
                torrentGrid.SelectedItem = torrent;
                
                if (Visibility == Visibility.Hidden)
                {
                    Visibility = Visibility.Visible;
                    Focus();
                }
            }
        }

        private void ExecuteExit(object sender, ExecutedRoutedEventArgs e)
        {
            AllowClose = true;
            NotifyIcon.Dispose();
            Close();
        }

        private void Initialize()
        {
            SettingsManager = new SettingsManager();
            SettingsManager.Initialize();
            // Load prior session
            if (File.Exists(SettingsManager.FastResumePath))
            {
                var resume = BEncodedDictionary.Decode<BEncodedDictionary>(
                    File.ReadAllBytes(SettingsManager.FastResumePath));
                foreach (var torrent in resume)
                {
                    // TODO
                }
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            var resume = new BEncodedDictionary();
            foreach (var torrent in Client.Torrents)
            {
                torrent.Torrent.Stop();
                while (torrent.Torrent.State != TorrentState.Stopped)
                    Thread.Sleep(100);
                resume.Add(torrent.Torrent.InfoHash.ToHex(), torrent.Torrent.SaveFastResume().Encode());
            }
            File.WriteAllBytes(SettingsManager.FastResumePath, resume.Encode());
            Client.Shutdown();
        }

        private void torrentGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (torrentGrid.SelectedItems.Count != 1)
            {
                lowerFill.Visibility = Visibility.Visible;
                lowerGrid.DataContext = null;
            }
            else
            {
                lowerFill.Visibility = Visibility.Collapsed;
                lowerGrid.DataContext = torrentGrid.SelectedItem;
            }
        }

        private void quickAddClicked(object sender, RoutedEventArgs e)
        {
            TorrentWrapper torrent;
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();
            var magnetLink = new MagnetLink(IgnoredClipboardValue);
            var name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(magnetLink.Name));
            var directory = Path.Combine(SettingsManager.DefaultDownloadLocation, AddTorrentWindow.CleanFileName(name));
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            torrent = new TorrentWrapper(magnetLink, directory,
                new TorrentSettings(), Path.GetTempFileName());
            Client.AddTorrent(torrent);
            UpdateTorrentGrid(); // TODO: Centralize torrent creation
            torrentGrid.SelectedItem = torrent;
        }

        private void quickAddDismissClicked(object sender, RoutedEventArgs e)
        {
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();
        }

        private void quickAddAdvancedClciked(object sender, RoutedEventArgs e)
        {
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();
            ExecuteNew(sender, null);
        }

        private void filePriorityBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = sender as ComboBox;
            var file = source.Tag as PeriodicFile;
            file.File.Priority = (Priority)source.SelectedIndex;
        }

        private void fileListGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (PeriodicFile item in fileListGrid.SelectedItems)
            {
                var extension = Path.GetExtension(item.File.Path);
                // TODO: Expand list of naughty file extensions
                bool open = true;
                if (extension == ".exe")
                {
                    open = MessageBox.Show("This file could be dangerous. Are you sure you want to open it?",
                        "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
                }
                if (open)
                {
                    var torrent = torrentGrid.SelectedItem as PeriodicTorrent;
                    Process.Start(Path.Combine(torrent.Torrent.SavePath, item.File.Path));
                }
            }
        }

        private void torrentGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (PeriodicTorrent item in torrentGrid.SelectedItems)
                Process.Start("explorer", item.Torrent.SavePath);
        }
    }
}
