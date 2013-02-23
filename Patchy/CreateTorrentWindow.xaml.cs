using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MonoTorrent;
using MonoTorrent.Common;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for CreateTorrentWindow.xaml
    /// </summary>
    public partial class CreateTorrentWindow : Window
    {
        public Torrent Torrent { get; set; }
        public bool SeedImmediately
        {
            get
            {
                return startSeedingCheckBox.IsChecked.Value;
            }
        }
        public string FilePath { get; set; }

        private TorrentCreator Creator { get; set; }
        private string Path { get; set; }

        public CreateTorrentWindow()
        {
            InitializeComponent();
            // Add a couple public trackers
            trackerListBox.Items.Add("udp://tracker.publicbt.com:80");
            trackerListBox.Items.Add("udp://tracker.openbittorrent.com:80");
        }

        private void addTrackerClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(trackerTextBox.Text))
            {
                MessageBox.Show("Please enter a tracker address.");
                return;
            }
            if (!Uri.IsWellFormedUriString(trackerTextBox.Text, UriKind.Absolute))
            {
                MessageBox.Show("This is not a valid tracker.");
                return;
            }
            if (trackerListBox.Items.Contains(trackerTextBox.Text))
            {
                MessageBox.Show("This tracker has already been added.");
                return;
            }
            trackerListBox.Items.Add(trackerTextBox.Text);
            trackerTextBox.Text = string.Empty;
        }

        private void removeTrackersClicked(object sender, RoutedEventArgs e)
        {
            var trackers = new List<string>(trackerListBox.Items.Cast<string>());
            foreach (var tracker in trackers)
                trackerListBox.Items.Remove(tracker);
        }

        private void createButtonClicked(object sender, RoutedEventArgs e)
        {
            var sourcePath = pathTextBox.Text;
            if (string.IsNullOrEmpty(sourcePath))
            {
                MessageBox.Show("Please select a file or files to add.");
                return;
            }
            if (singleFileRadioButton.IsChecked.Value)
            {
                if (!File.Exists(sourcePath))
                {
                    MessageBox.Show("The selected file does not exist!");
                    return;
                }
            }
            if (entireFolderRadioButton.IsChecked.Value)
            {
                if (!Directory.Exists(sourcePath))
                {
                    MessageBox.Show("The selected folder does not exist!");
                    return;
                }
            }
            Creator = new TorrentCreator();
            var source = new TorrentFileSource(sourcePath, ignoreHiddenFilesCheckBox.IsChecked.Value);
            var tier = new RawTrackerTier(trackerListBox.Items.Cast<string>());
            Creator.Announces.Add(tier);
            Creator.Comment = commentTextBox.Text;
            Creator.Private = privateTorrentCheckBox.IsChecked.Value;
            Creator.CreatedBy = "Patchy BitTorrent Client";
            Creator.PieceLength = TorrentCreator.RecommendedPieceSize(source.Files);
            var dialog = new SaveFileDialog();
            dialog.Filter = "Torrent Files (*.torrent)|*.torrent|All Files (*.*)|*.*";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            FilePath = sourcePath;
            if (dialog.ShowDialog().Value)
            {
                // Create the torrent
                Path = dialog.FileName;
                pathGrid.IsEnabled = trackerGrid.IsEnabled = optionsGrid.IsEnabled = createButton.IsEnabled = false;
                Creator.Hashed += Creator_Hashed;
                Creator.BeginCreate(source, CreationComplete, null);
            }
        }

        void Creator_Hashed(object sender, TorrentCreatorEventArgs e)
        {
 	        Dispatcher.Invoke(new Action(() => progressBar.Value = e.FileCompletion));
        }

        private void CreationComplete(IAsyncResult result)
        {
            Creator.EndCreate(result, Path);
            Process.Start("explorer", "/Select \"" + Path + "\"");
            Torrent = Torrent.Load(Path);
            Dispatcher.Invoke(new Action(() =>
                {
                    DialogResult = true;
                    Close();
                }));
        }

        private void cancelButtonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                Creator.AbortCreation();
            }
            catch { }
            Close();
        }

        private void browseButtonClicked(object sender, RoutedEventArgs e)
        {
            if (singleFileRadioButton.IsChecked.Value)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "All Files (*.*)|*.*";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Multiselect = false; // TODO: Add support for this, it'd be pretty neat
                if (dialog.ShowDialog().Value)
                    pathTextBox.Text = dialog.FileName;
            }
            else
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    pathTextBox.Text = dialog.SelectedPath;
            }
        }
    }
}
