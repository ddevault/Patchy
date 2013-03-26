using System;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MonoTorrent.Client;
using MonoTorrent.Common;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Patchy
{
    public partial class MainWindow
    {
        private void CanExecuteTorrentSpecificCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = torrentGrid.SelectedItems.Count != 0;
        }

        private void CanExecuteSingleTorrentCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = torrentGrid.SelectedItems.Count == 1;
        }

        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new CreateTorrentWindow();
            if (window.ShowDialog().Value)
            {
                var torrent = window.Torrent;
                var wrapper = new TorrentWrapper(torrent, window.FilePath, new TorrentSettings());
                var periodic = Client.AddTorrent(wrapper);
                // Save torrent to cache
                var cache = Path.Combine(SettingsManager.TorrentCachePath, torrent.TorrentPath);
                if (File.Exists(cache))
                    File.Delete(cache);
                File.Copy(torrent.TorrentPath, cache);
                periodic.CacheFilePath = cache;
                periodic.UpdateInfo();
                var serializer = new JsonSerializer();
                using (var writer = new StreamWriter(Path.Combine(SettingsManager.TorrentCachePath,
                    Path.GetFileNameWithoutExtension(periodic.CacheFilePath) + ".info")))
                    serializer.Serialize(new JsonTextWriter(writer), periodic.TorrentInfo);
            }
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow(SettingsManager);
            if (window.ShowDialog().Value)
            {
                PeriodicTorrent torrent;
                if (window.IsMagnet)
                    torrent = AddTorrent(window.MagnetLink, window.DestinationPath);
                else
                    torrent = AddTorrent(window.Torrent, window.DestinationPath);
                torrentGrid.SelectedItem = torrent;
                if (window.EditAdditionalSettings)
                    detailTabs.SelectedItem = torrentOptionsTab;

                if (Visibility == Visibility.Hidden)
                {
                    Visibility = Visibility.Visible;
                    Activate();
                }
                SaveSettings();
            }
        }

        private void ExecuteExit(object sender, ExecutedRoutedEventArgs e)
        {
            AllowClose = true;
            Close();
        }

        private void ExecuteEditPreferences(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new PreferencesWindow(SettingsManager);
            window.ShowDialog();
            SaveSettings();
            UpdateRss();
        }

        private void ExecuteDeleteTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            if (torrentGrid.SelectedItems.Count != 0)
            {
                var result = MessageBox.Show("Do you wish to remove the files associated with each torrent as well?",
                    "Confirm Removal", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                    return;
                if (result == MessageBoxResult.Yes)
                    Commands.RemoveTorrentWithFiles.Execute(null, this);
                else if (result == MessageBoxResult.No)
                    Commands.RemoveTorrent.Execute(null, this);
            }
        }

        private void ExecuteRemoveTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            var torrents = torrentGrid.SelectedItems.Cast<PeriodicTorrent>();
            foreach (var torrent in torrents)
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

        private void ExecuteRemoveTorrentWithFiles(object sender, ExecutedRoutedEventArgs e)
        {
            var torrents = torrentGrid.SelectedItems.Cast<PeriodicTorrent>();
            foreach (var torrent in torrents)
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

        private void ExecuteStopTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
            {
                if (t.Torrent.State != TorrentState.Stopped && t.Torrent.State != TorrentState.Stopped)
                    t.Stop();
            }
        }

        private void ExecutePauseTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
            {
                if (t.Torrent.State == TorrentState.Seeding || t.Torrent.State == TorrentState.Downloading)
                    t.Torrent.Pause();
            }
        }

        private void ExecuteResumeTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
            {
                if (t.Torrent.State == TorrentState.Paused || t.Torrent.State == TorrentState.Stopped)
                    t.Resume();
            }
        }

        private void ExecuteMoveTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            var torrent = torrentGrid.SelectedItems.Cast<PeriodicTorrent>().FirstOrDefault();
            if (torrent == null)
                return;
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Client.MoveTorrent(torrent, dialog.SelectedPath);
        }

        private void ExecuteRehashTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            var torrents = torrentGrid.SelectedItems.Cast<PeriodicTorrent>();
            if (!torrents.Any())
                return;
            Task.Factory.StartNew(() =>
                {
                    foreach (var torrent in torrents)
                    {
                        torrent.Stop();
                        while (torrent.Torrent.State != TorrentState.Stopped && torrent.Torrent.State != TorrentState.Error) ;
                        torrent.Torrent.HashCheck(true);
                    }
                });
        }

        private void ExecuteExcludeFromQueue(object sender, ExecutedRoutedEventArgs e)
        {
            var torrents = torrentGrid.SelectedItems.Cast<PeriodicTorrent>();
            if (!torrents.Any())
                return;
            foreach (var torrent in torrents)
            {
                torrent.ExcludeFromQueue = true;
                if (torrent.State == TorrentState.Stopped &&
                   (torrent.PriorState == TorrentState.Downloading || torrent.PriorState == TorrentState.Seeding) &&
                   !torrent.StoppedByUser)
                {
                    torrent.Resume();
                }
            }
        }

        private void ExecuteCreateLabel(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new TorrentLabelWindow();
            if (window.ShowDialog().Value)
            {
                var label = window.Label;
                SettingsManager.Labels = SettingsManager.Labels.Concat(new[] { label }).ToArray();
                SaveSettings();
                AddLabel(label);
            }
        }

        private void ExecuteClearLabel(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (PeriodicTorrent torrent in torrentGrid.SelectedItems)
                torrent.Label = null;
        }

        private void AddLabel(TorrentLabel label)
        {
            var comboItem = new ComboBoxItem
            {
                Content = label.Name,
                Background = label.Brush,
                Foreground = label.ForegroundBrush,
                Tag = label
            };
            labelList.Items.Insert(labelList.Items.Count - 2, comboItem);
            var menuItem = new MenuItem
            {
                Header = label.Name,
                Background = label.Brush,
                Foreground = label.ForegroundBrush,
                Tag = label
            };
            menuItem.Click += setLabelOnTorrentClicked;
            torrentGridContextMenuSetLabelMenu.Items.Insert(0, menuItem);
            menuItem = new MenuItem
            {
                Header = label.Name,
                Background = label.Brush,
                Foreground = label.ForegroundBrush,
                Tag = label
            };
            menuItem.Click += setLabelOnTorrentClicked;
            menuSetLabelMenu.Items.Insert(0, menuItem);
        }

        private void setLabelOnTorrentClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            var label = (TorrentLabel)(sender as MenuItem).Tag;
            foreach (PeriodicTorrent torrent in torrentGrid.SelectedItems)
                torrent.Label = label;
        }
    }

    public static class Commands
    {
        public static readonly RoutedCommand EditPreferences = new RoutedUICommand("Edit Preferences", "EditPreferences", typeof(MainWindow));

        public static readonly RoutedCommand DeleteTorrent = new RoutedUICommand("Delete Torrent", "DeleteTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand RemoveTorrent = new RoutedUICommand("Remove Torrent", "RemoveTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand RemoveTorrentWithFiles = new RoutedUICommand("Remove Torrent and Files", "RemoveTorrentWithFiles", typeof(MainWindow));

        public static readonly RoutedUICommand PauseTorrent = new RoutedUICommand("Pause Torrent", "PauseTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand StopTorrent = new RoutedUICommand("Stop Torrent", "StopTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand ResumeTorrent = new RoutedUICommand("Resume Torrent", "ResumeTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand MoveTorrent = new RoutedUICommand("Move Torrent", "MoveTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand RehashTorrent = new RoutedUICommand("Rehash Torrent", "RehashTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand ExcludeFromQueue = new RoutedUICommand("Exclude from Queue", "ExcludeTorrentFromQueue", typeof(MainWindow));

        public static readonly RoutedUICommand CreateLabel = new RoutedUICommand("Create Label", "CreateLabel", typeof(MainWindow));
        public static readonly RoutedUICommand ClearLabel = new RoutedUICommand("Clear Label", "ClearLabel", typeof(MainWindow));
    }
}
