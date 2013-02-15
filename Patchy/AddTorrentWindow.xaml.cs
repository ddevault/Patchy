using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using MonoTorrent.Common;
using MonoTorrent;
using System.Web;
using Microsoft.Win32;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for AddTorrentWindow.xaml
    /// </summary>
    public partial class AddTorrentWindow : Window
    {
        private SettingsManager Settings { get; set; }

        public string DefaultLocation { get; set; }

        public bool IsMagnet { get { return magnetLinkRadioButton.IsChecked.Value; } }
        public Torrent Torrent { get; set; }
        private MagnetLink _magnetLink;
        public MagnetLink MagnetLink
        {
            get { return _magnetLink; }
            set
            {
                _magnetLink = value;
                magnetLinkRadioButton.IsChecked = true;
                magnetLinkTextBox.Text = ConvertMagnetToString(value);
            }
        }

        private string ConvertMagnetToString(MagnetLink value)
        {
            var result = "magnet:?";
            result += "xt=urn:btih:" + value.InfoHash.ToHex();
            result += "&dn=" + value.Name;
            foreach (var url in value.AnnounceUrls)
                result += "&tr=" + Uri.EscapeUriString(url);
            return result;
        }

        public string DestinationPath { get; set; }
        public bool EditAdditionalSettings { get { return editSettingsCheckBox.IsChecked.Value; } }

        private class FolderBrowserItem
        {
            public FolderBrowserItem(string fullPath, bool isDrive)
            {
                FullPath = fullPath;
                IsDrive = isDrive;
            }

            public string FullPath { get; set; }
            private bool IsDrive { get; set; }

            public override string ToString()
            {
                if (IsDrive)
                    return FullPath;
                return Path.GetFileName(FullPath);
            }
        }

        public AddTorrentWindow(SettingsManager settingsManager, string torrentPath = null)
        {
            Settings = settingsManager;
            InitializeComponent();
            foreach (var path in settingsManager.RecentDownloadLocations)
                recentItemsComboBox.Items.Add(new FolderBrowserItem(path, false));
            if (recentItemsComboBox.Items.Count != 0)
                recentItemsComboBox.SelectedIndex = 0;

            DefaultLocation = settingsManager.DefaultDownloadLocation;
            defaultDestinationRadioButton.Content = Path.GetFileName(DefaultLocation) + " (default)";
            // Check for auto-population of magnet link
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                {
                    var uri = new Uri(text);
                    if (uri.Scheme == "magnet")
                    {
                        magnetLinkRadioButton.IsChecked = true;
                        magnetLinkTextBox.Text = text;
                    }
                }
            }
            UpdateFileBrower("C:\\");
            if (torrentPath != null)
                torrentFileRadioButton.IsChecked = true;
        }

        private void UpdateFileBrower(string path)
        {
            try
            {
                string[] directories;
                try
                {
                    if (path != null)
                        directories = Directory.GetDirectories(path);
                    else
                        directories = Directory.GetLogicalDrives();
                }
                catch
                {
                    UpdateFileBrower(folderBrowser.Tag as string);
                    return;
                }
                var items = new FolderBrowserItem[directories.Length + (path != null ? 1 : 0)];
                for (int i = 0; i < directories.Length; i++)
                    items[i + (path != null ? 1 : 0)] = new FolderBrowserItem(directories[i], path == null);
                if (path != null)
                    items[0] = new FolderBrowserItem("..", true);
                folderBrowser.ItemsSource = items;
                folderBrowser.Tag = path;
                customDestinationTextBox.Text = path;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access is denied.");
            }
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddClicked(object sender, RoutedEventArgs e)
        {
            if (customDestinationTextBox.IsFocused)
                return;
            try
            {
                string name;
                if (magnetLinkRadioButton.IsChecked.Value)
                {
                    MagnetLink = new MagnetLink(magnetLinkTextBox.Text);
                    name = HttpUtility.HtmlDecode(HttpUtility.UrlDecode(MagnetLink.Name));
                }
                else
                {
                    Torrent = Torrent.Load(torrentFileTextBox.Text);
                    name = Torrent.Name;
                }
                if (defaultDestinationRadioButton.IsChecked.Value)
                    DestinationPath = DefaultLocation;
                else if (recentRadioButton.IsChecked.Value)
                {
                    var recent = recentItemsComboBox.SelectedItem as FolderBrowserItem;
                    DestinationPath = recent.FullPath;
                }
                else
                    DestinationPath = customDestinationTextBox.Text;

                if (otherRadioButton.IsChecked.Value)
                    Settings.RecentDownloadLocations = new[] { DestinationPath }.Concat(Settings.RecentDownloadLocations).Take(5).ToArray();

                DestinationPath = Path.Combine(DestinationPath, ClientManager.CleanFileName(name));
            }
            catch
            {
                MessageBox.Show("Unable to load this torrent.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        #region UI Interaction

        private void BrowseTorrentButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = string.Empty;
            dialog.Filter = "Torrents (.torrent)|*.torrent|All Files|*.*";
            dialog.DefaultExt = ".torrent";
            if (dialog.ShowDialog().GetValueOrDefault(false))
                torrentFileTextBox.Text = dialog.FileName;
        }

        private void FolderBrowserMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var file = folderBrowser.SelectedItem as FolderBrowserItem;
            var path = file.FullPath;
            if (file.FullPath == "..")
            {
                var parent = Directory.GetParent(folderBrowser.Tag as string);
                if (parent != null)
                    path = parent.FullName;
                else
                    path = null;
            }
            UpdateFileBrower(path);
        }

        private void FolderBrowserTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!Directory.Exists(customDestinationTextBox.Text))
                {
                    MessageBox.Show("Directory does not exist.");
                    customDestinationTextBox.Text = folderBrowser.Tag as string;
                    return;
                }
                UpdateFileBrower(customDestinationTextBox.Text);
            }
        }

        #region Radio Button Visibility Hooks

        private void TorrentFileRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (magnetLinkTextBox == null)
                return;
            magnetLinkTextBox.IsEnabled = false;
            torrentFileTextBox.IsEnabled = browseTorrentTextBox.IsEnabled = true;
        }

        private void MagnetLinkRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            magnetLinkTextBox.IsEnabled = true;
            torrentFileTextBox.IsEnabled = browseTorrentTextBox.IsEnabled = false;
        }

        private void RecentRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            recentItemsComboBox.IsEnabled = true;
            folderBrowser.IsEnabled = customDestinationTextBox.IsEnabled = false;
        }

        private void OtherRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            recentItemsComboBox.IsEnabled = false;
            folderBrowser.IsEnabled = customDestinationTextBox.IsEnabled = true;
        }

        #endregion

        #endregion
    }
}
