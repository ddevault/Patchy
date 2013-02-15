using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using BrendanGrant.Helpers.FileAssociation;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public SettingsManager Settings { get; set; }

        public PreferencesWindow(SettingsManager manager)
        {
            InitializeComponent();
            InitializeRegistryBoundItems();
            var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Patchy.LICENSE"));
            licenseText.Text = reader.ReadToEnd();
            Settings = manager;
            rssManagerGrid.DataContext = Settings.RssFeeds;
        }

        private void InitializeRegistryBoundItems()
        {
            if (!UacHelper.IsProcessElevated)
            {
                registryBoundPreferences.IsEnabled = false;
                elevatePermissionsPanel.Visibility = Visibility.Visible;
                return;
            }
            var torrent = new FileAssociationInfo(".torrent");
            if (torrent.Exists)
                torrentAssociationCheckBox.IsChecked = torrent.ProgID == "Patchy";
            else
                torrentAssociationCheckBox.IsChecked = false;
            // Check magnet link association
            var value = Registry.GetValue(@"HKEY_CLASSES_ROOT\\Magnet", null, null);
            if (value == null)
                magnetAssociationCheckBox.IsChecked = false;
            else
            {
                var shell = (string)Registry.GetValue(@"HKEY_CLASSES_ROOT\Magnet\shell\open\command", null, null);
                magnetAssociationCheckBox.IsChecked = shell == string.Format("\"{0}\" \"%1\"", Assembly.GetEntryAssembly().Location);
            }
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var startup = key.GetValue("Patchy", null) as string;
            startOnWindowsStartupCheckBox.IsChecked = startup != null;
            if (startup == null)
                startMinimizedCheckBox.IsChecked = false;
            else
                startMinimizedCheckBox.IsChecked = startup.EndsWith("--minimized");
        }

        private void clearTorrentCacheClick(object sender, RoutedEventArgs e)
        {
            App.ClearCacheOnExit = true;
            MessageBox.Show("Cache will be deleted on exit.");
        }

        private void torrentAssociationCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            var torrent = new FileAssociationInfo(".torrent");
            if (torrentAssociationCheckBox.IsChecked.Value)
            {
                torrent.Create("Patchy");
                torrent.ContentType = "application/x-bittorrent";
                torrent.OpenWithList = new[] { "patchy.exe" };
                var program = new ProgramAssociationInfo(torrent.ProgID);
                if (!program.Exists)
                {
                    program.Create("Patchy Torrent Files", new ProgramVerb("Open", string.Format(
                        "\"{0}\" \"%1\"", Path.Combine(Directory.GetCurrentDirectory(), "Patchy.exe"))));
                    program.DefaultIcon = new ProgramIcon(Assembly.GetEntryAssembly().Location);
                }
            }
            else
                torrent.Delete();
        }

        private void magnetAssociationCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            if (magnetAssociationCheckBox.IsChecked.Value)
            {
                var path = string.Format("\"{0}\" \"%1\"", Assembly.GetEntryAssembly().Location);
                var key = Registry.ClassesRoot.CreateSubKey("Magnet");
                key.SetValue(null, "Magnet URI");
                key.SetValue("Content Type", "application/x-magnet");
                key.SetValue("URL Protocol", string.Empty);
                var defaultIcon = key.CreateSubKey("DefaultIcon");
                defaultIcon.SetValue(null, Assembly.GetEntryAssembly().Location);
                var shell = key.CreateSubKey("shell");
                shell.SetValue(null, "open");
                var open = shell.CreateSubKey("open");
                var command = open.CreateSubKey("command");
                command.SetValue(null, path);
                command.Close(); open.Close(); shell.Close(); defaultIcon.Close(); key.Close();
            }
            else
            {
                Registry.ClassesRoot.DeleteSubKeyTree("Magnet");
                Registry.ClassesRoot.Flush();
            }
        }

        private void startOnWindowsStartupChecked(object sender, RoutedEventArgs e)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (startOnWindowsStartupCheckBox.IsChecked.Value)
            {
                if (startMinimizedCheckBox.IsChecked.Value)
                    key.SetValue("Patchy", "\"" + Assembly.GetEntryAssembly().Location + "\" --minimized");
                else
                    key.SetValue("Patchy", "\"" + Assembly.GetEntryAssembly().Location + "\"");
            }
            else
                key.DeleteValue("Patchy");
            key.Close();
        }

        private void startupMinimizedChecked(object sender, RoutedEventArgs e)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (startMinimizedCheckBox.IsChecked.Value)
                key.SetValue("Patchy", "\"" + Assembly.GetEntryAssembly().Location + "\" --minimized");
            else
                key.SetValue("Patchy", "\"" + Assembly.GetEntryAssembly().Location + "\"");
            key.Close();
        }

        private void licenseText_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void browseSourceClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SirCmpwn/Patchy");
        }

        private void elevatePermissionsButtonClicked(object sender, RoutedEventArgs e)
        {
            var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location);
            info.Verb = "runas";
            (Application.Current as App).ShutdownSingleton();
            (Application.Current.MainWindow as MainWindow).AllowClose = true;
            Application.Current.MainWindow.Close();
            Process.Start(info);
            Application.Current.Shutdown();
        }

        #region RSS Manager

        private void addNewFeedButtonClick(object sender, RoutedEventArgs e)
        {
            addNewFeedButton.IsEnabled = newFeedUrlTextBox.IsEnabled = false;
            var address = newFeedUrlTextBox.Text;
            if (Settings.RssFeeds.Any(f => f.Address == address))
            {
                MessageBox.Show("The specified feed has already been added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                addNewFeedButton.IsEnabled = newFeedUrlTextBox.IsEnabled = true;
                return;
            }
            Task.Factory.StartNew(() =>
            {
                // Validate feed
                var client = new WebClient();
                try
                {
                    var feed = client.DownloadString(address);
                    var document = XDocument.Parse(feed);
                    if (!RssFeed.ValidateFeed(document))
                        throw new Exception();
                    var rss = new RssFeed(address);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Settings.RssFeeds.Add(rss);
                        addNewFeedButton.IsEnabled = newFeedUrlTextBox.IsEnabled = true;
                        newFeedUrlTextBox.Text = string.Empty;
                        feedListView.SelectedItem = rss;
                    }));
                }
                catch
                {
                    MessageBox.Show("The specified feed is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Dispatcher.BeginInvoke(new Action(() => addNewFeedButton.IsEnabled = newFeedUrlTextBox.IsEnabled = true));
                }
            });
        }

        private void addFeedRuleButtonClick(object sender, RoutedEventArgs e)
        {
            var feed = (RssFeed)feedListView.SelectedItem;
            if (string.IsNullOrEmpty(ruleRegexTextBox.Text))
                return;
            Regex regex;
            try
            {
                regex = new Regex(ruleRegexTextBox.Text, RegexOptions.IgnoreCase);
            }
            catch
            {
                MessageBox.Show("The specified regular expression is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var type = (RssTorrentRule.RuleType)ruleTypeComboBox.SelectedIndex;
            if (feed.TorrentRules.Any(r => r.Type == type && r.Regex.ToString() == regex.ToString()))
            {
                MessageBox.Show("This rule has already been added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            feed.TorrentRules.Add(new RssTorrentRule(type, regex));
            ruleRegexTextBox.Text = string.Empty;
        }

        private void removeRulesButtonClick(object sender, RoutedEventArgs e)
        {
            var rules = new List<RssTorrentRule>(rulesListView.SelectedItems.Cast<RssTorrentRule>());
            var feed = (RssFeed)feedListView.SelectedItem;
            foreach (var rule in rules)
                feed.TorrentRules.Remove(rule);
        }

        private void removeFeedsButtonClick(object sender, RoutedEventArgs e)
        {
            var feeds = new List<RssFeed>(feedListView.SelectedItems.Cast<RssFeed>());
            foreach (var feed in feeds)
                Settings.RssFeeds.Remove(feed);
        }

        #endregion
    }
}
