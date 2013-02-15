using System.IO;
using System.Windows;
using System.Windows.Input;
using MonoTorrent.Client;

namespace Patchy
{
    public partial class MainWindow
    {
        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow(SettingsManager.DefaultDownloadLocation);
            if (window.ShowDialog().GetValueOrDefault(false))
            {
                if (window.IsMagnet)
                    AddTorrent(window.MagnetLink, window.DestinationPath);
                else
                    AddTorrent(window.Torrent, window.DestinationPath);

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

        private void ExecuteEditPreferences(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new PreferencesWindow(SettingsManager);
            window.ShowDialog();
        }
    }

    public static class Commands
    {
        public static readonly RoutedCommand EditPreferences = new RoutedUICommand("Edit Preferences", "EditPreferences", typeof(MainWindow));
    }
}
