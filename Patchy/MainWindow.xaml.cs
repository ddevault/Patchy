using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
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
                            foreach (var torrent in Client.Torrents)
                                torrent.Update();
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

        private void UpdateTorrentGrid(bool recreate)
        {
            if (recreate)
            {
                torrentGrid.Items.Clear();
                foreach (var torrent in Client.Torrents)
                    torrentGrid.Items.Add(torrent);
            }
            else
            {
                if (torrentGrid.Items.Count != Client.Torrents.Count)
                {
                    UpdateTorrentGrid(true);
                    return;
                }
                for (int i = 0; i < Client.Torrents.Count; i++)
                    torrentGrid.Items[i] = Client.Torrents[i];
            }
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

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (AllowClose)
                return;
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow();
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
                UpdateTorrentGrid(true);
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
    }
}
