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
using MonoTorrent.Client;

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
            Timer = new Timer(o =>
                {
                    // Torrents don't implement INotifyPropertyChanged
                    Dispatcher.Invoke(() =>
                        {
                            UpdateTorrentGrid(false);
                            if (Client.Torrents.Count == 0)
                                NotifyIcon.Text = "Patchy";
                            else if (Client.Torrents.Any(t => !t.Complete))
                            {
                                NotifyIcon.Text = string.Format(
                                    "Patchy - {0} torrent{3}, {1} downloading at {2}%",
                                    Client.Torrents.Count,
                                    Client.Torrents.Count(t => !t.Complete),
                                    Client.Torrents.Where(t => !t.Complete).Select(t => t.Progress)
                                        .Aggregate((t, n) => t + n) / Client.Torrents.Count(t => !t.Complete),
                                    Client.Torrents.Count == 1 ? "" : "s");
                            }
                            else
                            {
                                NotifyIcon.Text = string.Format(
                                    "Patchy - Seeding {0} torrents",
                                    Client.Torrents.Count);
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
    }
}
