using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shell;
using MonoTorrent;
using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Common;
using System.Windows.Media;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon NotifyIcon { get; set; }
        private PeriodicTorrent BalloonTorrent { get; set; }
        private string IgnoredClipboardValue { get; set; }
        private FileSystemWatcher AutoWatcher { get; set; }
        internal bool AllowClose { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
            torrentGrid.LoadingRow += (s, e) =>
                {
                    e.Row.MouseDoubleClick -= TorrentGridMouseDoubleClick; // Rumoured to be called again sometimes when rows are sorted
                    e.Row.MouseDoubleClick += TorrentGridMouseDoubleClick;
                };
            fileListGrid.LoadingRow += (s, e) =>
            {
                e.Row.MouseDoubleClick -= FileListGridMouseDoubleClick;
                e.Row.MouseDoubleClick += FileListGridMouseDoubleClick;
            };
            
            Client = new ClientManager();

            Initialize();
            if (string.IsNullOrEmpty(SettingsManager.AutomaticAddDirectory))
                AutoWatcher = new FileSystemWatcher();
            else
                AutoWatcher = new FileSystemWatcher(SettingsManager.AutomaticAddDirectory);
            AutoWatcher.Filter = "*.torrent";
            if (!string.IsNullOrEmpty(SettingsManager.AutomaticAddDirectory))
                AutoWatcher.EnableRaisingEvents = true;
            AutoWatcher.Created += AutoWatcher_Created;

            torrentGrid.ItemsSource = Client.Torrents;

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            ReloadRssTimer();

            if (UacHelper.IsProcessElevated)
                elevatedPermissionsGrid.Visibility = Visibility.Visible;
            StateChanged += MainWindow_StateChanged;
        }

        void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (SettingsManager.MinimizeToSystemTray && SettingsManager.ShowTrayIcon)
                {
                    Visibility = Visibility.Hidden;
                    WindowState = WindowState.Normal;
                }
            }
        }

        void AutoWatcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                var torrent = Torrent.Load(e.FullPath);
                AddTorrent(torrent, SettingsManager.DefaultDownloadLocation, true);
                BalloonTorrent = null;
                NotifyIcon.ShowBalloonTip(5000, "Added torrent from torrent path", torrent.Name, System.Windows.Forms.ToolTipIcon.Info);
            }
            catch { }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (WindowStyle == WindowStyle.None)
                Visibility = Visibility.Hidden;
        }

        private void InitializeNotifyIcon()
        {
            NotifyIcon = new System.Windows.Forms.NotifyIcon 
            {
                Text = "Patchy",
                Icon = new System.Drawing.Icon(Application.GetResourceStream(
                    new Uri("pack://application:,,,/Patchy;component/Images/patchy.ico" )).Stream),
                Visible = true
            };
            NotifyIcon.DoubleClick += NotifyIconClick;
            NotifyIcon.BalloonTipClicked += NotifyIconBalloonTipClicked;
            var menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("Add Torrent", (s, e) => ExecuteNew(null, null));
            menu.MenuItems.Add("Exit", (s, e) =>
            {
                NotifyIcon.Dispose();
                AllowClose = true;
                Close();
            });
            NotifyIcon.ContextMenu = menu;
        }

        private void UpdateNotifyIcon()
        {
            if (Client.Torrents.Count == 0)
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                NotifyIcon.Text = "Patchy";
            }
            else if (Client.Torrents.Any(t => !t.Complete))
            {
                int progress = (int)(Client.Torrents.Where(t => !t.Complete).Select(t => t.Progress)
                        .Aggregate((t, n) => t + n) / Client.Torrents.Count(t => !t.Complete));
                NotifyIcon.Text = string.Format(
                    "Patchy - {0} torrent{3}, {1} downloading at {2}%",
                    Client.Torrents.Count,
                    Client.Torrents.Count(t => !t.Complete),
                    progress,
                    Client.Torrents.Count == 1 ? "" : "s");
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = progress / 100.0;
            }
            else
            {
                NotifyIcon.Text = string.Format(
                    "Patchy - Seeding {0} torrent{1}",
                    Client.Torrents.Count,
                    Client.Torrents.Count == 1 ? "" : "s");
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                TaskbarItemInfo.ProgressValue = 0;
            }
            if (Client.Torrents.Any(t => t.State == TorrentState.Error))
            {
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                TaskbarItemInfo.ProgressValue = 1;
            }
        }

        private void NotifyIconClick(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Hidden)
            {
                Visibility = Visibility.Visible;
                Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ShowInTaskbar = true;
                        ShowActivated = true;
                        WindowStyle = WindowStyle.ThreeDBorderWindow;
                        Width = 1200;
                        Height = 600;
                        Activate();
                    }));
            }
            else
                Visibility = Visibility.Hidden;
        }

        private void NotifyIconBalloonTipClicked(object sender, EventArgs e)
        {
            Process.Start("explorer", "\"" + BalloonTorrent.Torrent.SavePath + "\"");
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (AllowClose || (!SettingsManager.CloseToSystemTray && SettingsManager.ShowTrayIcon))
            {
                if (SettingsManager.ConfirmExitWhenActive && Client.Torrents.Any(t => t.State == TorrentState.Downloading || t.State == TorrentState.Seeding))
                {
                    e.Cancel = MessageBox.Show("You still have active torrents! Are you sure you want to exit?",
                        "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No;
                }
                return;
            }
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            if (SettingsManager.SaveSession)
            {
                var resume = new BEncodedDictionary();
                foreach (var torrent in Client.Torrents)
                {
                    torrent.Torrent.Stop();
                    while (torrent.Torrent.State != TorrentState.Stopped && torrent.Torrent.State != TorrentState.Error)
                        Thread.Sleep(100);
                    // TODO: Notify users on error? The application is shutting down here, it wouldn't be particualry
                    // easy to get information to the user
                    resume.Add(torrent.Torrent.InfoHash.ToHex(), torrent.Torrent.SaveFastResume().Encode());
                }
                File.WriteAllBytes(SettingsManager.FastResumePath, resume.Encode());
            }
            Client.Shutdown();
        }

        private void TorrentGridSelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void QuickAddClicked(object sender, RoutedEventArgs e)
        {
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();

            var link = new MagnetLink(IgnoredClipboardValue);
            var name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(link.Name));

            var path = Path.Combine(SettingsManager.DefaultDownloadLocation, 
                ClientManager.CleanFileName(name));

            AddTorrent(link, path);
        }

        private void QuickAddDismissClicked(object sender, RoutedEventArgs e)
        {
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();
        }

        private void QuickAddAdvancedClciked(object sender, RoutedEventArgs e)
        {
            IgnoredClipboardValue = Clipboard.GetText();
            CheckMagnetLinks();
            ExecuteNew(sender, null);
        }

        private void FilePriorityBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = sender as ComboBox;
            var file = source.Tag as PeriodicFile;
            file.Priority = (Priority)source.SelectedIndex;
        }

        private void FileListGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (PeriodicFile item in fileListGrid.SelectedItems)
            {
                var extension = Path.GetExtension(item.File.Path);

                // TODO: Expand list of naughty file extensions

                bool open = true;
                if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) && SettingsManager.WarnOnDangerousFiles)
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

        private void TorrentGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (PeriodicTorrent item in torrentGrid.SelectedItems)
            {
                DoubleClickAction action;
                if (item.State == TorrentState.Downloading)
                    action = SettingsManager.DoubleClickDownloading;
                else
                    action = SettingsManager.DoubleClickSeeding;
                switch (action)
                {
                    case DoubleClickAction.OpenFolder:
                        Process.Start("explorer", "\"" + item.Torrent.SavePath + "\"");
                        break;
                    case DoubleClickAction.OpenLargestFile:
                        var largest = item.Torrent.Torrent.Files.OrderBy(f => f.Length).FirstOrDefault();
                        if (largest != null)
                            Process.Start(largest.FullPath);
                        break;
                    case DoubleClickAction.ToggleActive:
                        if (item.State == TorrentState.Paused)
                            item.Torrent.Start();
                        else
                            item.Torrent.Pause();
                        break;
                }
            }
        }

        private void torrentGridContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = torrentGrid.SelectedItems.Count == 0;
            torrentGridToggleStreamingMenuItem.IsEnabled = torrentGrid.SelectedItems.Count == 1;
            torrentGridToggleStreamingMenuItem.IsChecked = false;
            if (torrentGrid.SelectedItems.Count == 1)
            {
                var torrent = torrentGrid.SelectedItem as PeriodicTorrent;
                torrentGridToggleStreamingMenuItem.Checked -= torrentGridToggleStreaming;
                torrentGridToggleStreamingMenuItem.IsChecked = torrent.PiecePicker is SlidingWindowPicker;
                torrentGridToggleStreamingMenuItem.Checked += torrentGridToggleStreaming;
            }
        }

        private void torrentGridOpenFolder(object sender, RoutedEventArgs e)
        {
            foreach (PeriodicTorrent torrent in torrentGrid.SelectedItems)
                Process.Start("explorer", "\"" + torrent.Torrent.SavePath + "\"");
        }

        private void torrentGridRemoveTorrent(object sender, RoutedEventArgs e)
        {
            foreach (PeriodicTorrent torrent in torrentGrid.SelectedItems)
            {
                if (torrent.State == TorrentState.Downloading)
                {
                    if (MessageBox.Show(torrent.Name + " is not complete. Are you sure you want to remove it?",
                        "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        continue;
                }
                Client.RemoveTorrent(torrent);
            }
        }

        private void torrentGridRemoveTorrentAndData(object sender, RoutedEventArgs e)
        {
            foreach (PeriodicTorrent torrent in torrentGrid.SelectedItems)
            {
                if (torrent.State == TorrentState.Downloading)
                {
                    if (MessageBox.Show(torrent.Name + " is not complete. Are you sure you want to remove it?",
                        "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        continue;
                }
                Client.RemoveTorrentAndFiles(torrent);
            }
        }

        private void torrentGridCopyMagnentLink(object sender, RoutedEventArgs e)
        {
            var torrent = torrentGrid.SelectedItem as PeriodicTorrent;
            var link = string.Format("magnet:?xl={0}&dn={1}&xt=urn:btih:{2}",
                torrent.Size, Uri.EscapeUriString(torrent.Name), Uri.EscapeUriString(torrent.Torrent.InfoHash.ToHex()));
            if (torrent.Torrent.TrackerManager.CurrentTracker != null)
                link += "&tr=" + Uri.EscapeUriString(torrent.Torrent.TrackerManager.CurrentTracker.Uri.ToString());
            Clipboard.SetText(link);
        }

        private void fileListGridContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = fileListGrid.SelectedItems.Count == 0;
        }

        private void torrentGridToggleStreaming(object sender, RoutedEventArgs e)
        {
            var torrent = torrentGrid.SelectedItem as PeriodicTorrent;
            if (torrent.PiecePicker is SlidingWindowPicker)
                torrent.ChangePicker(new RandomisedPicker(new StandardPicker()));
            else
            {
                var sliding = new SlidingWindowPicker(torrent.PiecePicker);
                // TODO: Perhaps integrate streaming properly into the client
                sliding.HighPrioritySetStart = 0;
                sliding.HighPrioritySetSize = 1;
                torrent.ChangePicker(sliding);
            }
        }

        private void fileListGridOpenFile(object sender, RoutedEventArgs e)
        {
            var item = fileListGrid.SelectedItem as PeriodicFile;
            var extension = Path.GetExtension(item.File.Path);

            // TODO: Expand list of naughty file extensions

            bool open = true;
            if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) && SettingsManager.WarnOnDangerousFiles)
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

        private void fileListGridOpenFolder(object sender, RoutedEventArgs e)
        {
            var item = fileListGrid.SelectedItem as PeriodicFile;
            Process.Start("explorer", "/Select, \"" + item.File.FullPath + "\"");
        }

        private void menuSourceCodeClicked(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SirCmpwn/Patchy");
        }

        private void menuReportBugClicked(object sender, RoutedEventArgs e)
        {
            var systemInfo = string.Format("OS Name: {0}" + Environment.NewLine +
                "Edition: {1}" + Environment.NewLine +
                "Service Pack: {2}" + Environment.NewLine +
                "Version: {3}" + Environment.NewLine +
                "Architecture: {4} bit", OSInfo.Name, OSInfo.Edition, OSInfo.ServicePack, OSInfo.Version, OSInfo.Bits);
            Process.Start(string.Format("https://github.com/SirCmpwn/Patchy/issues/new?title={0}&body={1}",
                Uri.EscapeUriString("A brief description of your problem"),
                Uri.EscapeUriString("[A more detailed description of your problem]" + Environment.NewLine + Environment.NewLine + systemInfo)));
        }

        private void ElevatedGridDismissClicked(object sender, RoutedEventArgs e)
        {
            elevatedPermissionsGrid.Visibility = Visibility.Collapsed;
        }
    }
}
