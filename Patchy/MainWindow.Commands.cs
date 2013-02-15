using System.IO;
using System.Windows;
using System.Windows.Input;
using MonoTorrent.Client;
using Newtonsoft.Json;

namespace Patchy
{
    public partial class MainWindow
    {
        private void ExecuteNew(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new AddTorrentWindow(SettingsManager);
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

        private void LoadSettings()
        {
            if (!File.Exists(SettingsManager.SettingsFile))
            {
                SettingsManager.SetToDefaults();
                SaveSettings();
            }
            else
            {
                var serializer = new JsonSerializer();
                serializer.MissingMemberHandling = MissingMemberHandling.Ignore;
                using (var reader = new StreamReader(SettingsManager.SettingsFile))
                    serializer.Populate(reader, SettingsManager);
            }
        }

        private void SaveSettings()
        {
            var serializer = new JsonSerializer();
            using (var writer = new StreamWriter(SettingsManager.SettingsFile))
                serializer.Serialize(writer, SettingsManager);
        }
    }

    public static class Commands
    {
        public static readonly RoutedCommand EditPreferences = new RoutedUICommand("Edit Preferences", "EditPreferences", typeof(MainWindow));
    }
}
