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

namespace Patchy
{
    /// <summary>
    /// Interaction logic for AddTorrentWindow.xaml
    /// </summary>
    public partial class AddTorrentWindow : Window
    {
        public string DefaultLocation { get; set; }

        public bool IsMagnet { get { return magnetLinkRadioButton.IsChecked.Value; } }
        public Torrent Torrent { get; set; }
        public MagnetLink MagnetLink { get; set; }
        public string DestinationPath { get; set; }
        public bool EditAdditionalSettings { get { return editSettingsCheckBox.IsChecked.Value; } }

        private class FolderBrowserItem
        {
            public FolderBrowserItem(string fullPath)
            {
                FullPath = fullPath;
            }

            public string FullPath { get; set; }
            public string Name { get { return Path.GetFileName(FullPath); } }
        }

        public AddTorrentWindow()
        {
            // TODO: Allow for customization
            DefaultLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            InitializeComponent();
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
        }

        private void UpdateFileBrower(string path)
        {
            customDestinationTextBox.Text = path;
            var directories = Directory.GetDirectories(path);
            var items = new FolderBrowserItem[directories.Length];
            for (int i = 0; i < items.Length; i++)
                items[i] = new FolderBrowserItem(directories[i]);
            folderBrowser.ItemsSource = items;
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void torrentFileRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (magnetLinkTextBox == null)
                return;
            magnetLinkTextBox.IsEnabled = false;
            torrentFileTextBox.IsEnabled = browseTorrentTextBox.IsEnabled = true;
        }

        private void magnetLinkRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            magnetLinkTextBox.IsEnabled = true;
            torrentFileTextBox.IsEnabled = browseTorrentTextBox.IsEnabled = false;
        }

        private void recentRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            recentItemsComboBox.IsEnabled = true;
            folderBrowser.IsEnabled = customDestinationTextBox.IsEnabled = false;
        }

        private void otherRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            recentItemsComboBox.IsEnabled = false;
            folderBrowser.IsEnabled = customDestinationTextBox.IsEnabled = true;
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
                    name = MagnetLink.Name;
                    name = Uri.UnescapeDataString(MagnetLink.Name);
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
                    // TODO
                    MessageBox.Show("Recent locations is not yet implemented.");
                    return;
                }
                else
                    DestinationPath = customDestinationTextBox.Text;

                DestinationPath = Path.Combine(DestinationPath, CleanFileName(name));

                if (!Directory.Exists(DestinationPath))
                    Directory.CreateDirectory(DestinationPath);
            }
            catch
            {
                MessageBox.Show("Unable to load this torrent.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private static string CleanFileName(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
    }
}
