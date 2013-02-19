using System;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
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
            window.ShowDialog();
        }

        private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow(SettingsManager);
            if (window.ShowDialog().Value)
            {
                if (window.IsMagnet)
                    AddTorrent(window.MagnetLink, window.DestinationPath);
                else
                    AddTorrent(window.Torrent, window.DestinationPath);

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
            NotifyIcon.Dispose();
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

        private void ExecutePauseOrResumeTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            // Get paused/running info
            int paused = torrentGrid.SelectedItems.Cast<PeriodicTorrent>().Count(t => t.State == TorrentState.Paused);
            int running = torrentGrid.SelectedItems.Cast<PeriodicTorrent>().Count(t => t.State != TorrentState.Paused);
            if (paused != 0 && running != 0)
            {
                // Pause all
                foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
                {
                    if (t.Torrent.State != TorrentState.Paused)
                        t.Torrent.Pause();
                }
            }
            else if (paused != 0)
            {
                foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
                    t.Torrent.Start();
            }
            else
            {
                foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
                    t.Torrent.Pause();
            }
        }

        private void ExecuteResumeTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            foreach (PeriodicTorrent t in torrentGrid.SelectedItems)
            {
                if (t.Torrent.State == TorrentState.Paused)
                    t.Torrent.Start();
            }
        }

        private void ExecuteMoveTorrent(object sender, ExecutedRoutedEventArgs e)
        {
            var torrent = torrentGrid.SelectedItems.Cast<PeriodicTorrent>().FirstOrDefault();
            if (torrent == null)
                return;
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Client.MoveTorrent(torrent.Torrent, dialog.SelectedPath);
        }
    }

    public static class Commands
    {
        public static readonly RoutedCommand EditPreferences = new RoutedUICommand("Edit Preferences", "EditPreferences", typeof(MainWindow));
        public static readonly RoutedCommand DeleteTorrent = new RoutedUICommand("Delete Torrent", "DeleteTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand RemoveTorrent = new RoutedUICommand("Remove Torrent", "RemoveTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand RemoveTorrentWithFiles = new RoutedUICommand("Remove Torrent and Files", "RemoveTorrentWithFiles", typeof(MainWindow));
        public static readonly RoutedUICommand PauseOrResumeTorrent = new RoutedUICommand("Pause or resume torrent", "PauseOrResumeTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand ResumeTorrent = new RoutedUICommand("Resume Torrent", "ResumeTorrent", typeof(MainWindow));
        public static readonly RoutedUICommand MoveTorrent = new RoutedUICommand("Move Torrent", "MoveTorrent", typeof(MainWindow));
    }
}
