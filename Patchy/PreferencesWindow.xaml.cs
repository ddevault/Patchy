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
using System.Reflection;
using System.Collections.ObjectModel;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MonoTorrent.Client.Encryption;

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
            proxyPasswordBox.Password = manager.ProxyPassword;
            InitializeRegistryBoundItems();
            var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Patchy.LICENSE"));
            licenseText.Text = reader.ReadToEnd();
            reader.Close();
            Settings = manager;
            DataContext = Settings;
            if (Settings.EncryptionSettings == EncryptionTypes.PlainText)
                encryptionSettingsComboBox.SelectedIndex = 0;
            else if (Settings.EncryptionSettings == EncryptionTypes.All)
                encryptionSettingsComboBox.SelectedIndex = 1;
            else
                encryptionSettingsComboBox.SelectedIndex = 2;
            seedingTorrentDoubleClickComboBox.SelectedIndex = (int)Settings.DoubleClickSeeding;
            downloadingTorrentDoubleClickComboBox.SelectedIndex = (int)Settings.DoubleClickDownloading;
            foreach (var label in Settings.Labels)
            {
                var comboItem = new ComboBoxItem
                {
                    Content = label.Name,
                    Background = label.Brush,
                    Foreground = label.ForegroundBrush,
                    Tag = label
                };
                rssLabelComboBox.Items.Add(comboItem);
                comboItem = new ComboBoxItem
                {
                    Content = label.Name,
                    Background = label.Brush,
                    Foreground = label.ForegroundBrush,
                    Tag = label
                };
                automaticLabelComboBox.Items.Add(comboItem);
            }
        }

        private void InitializeRegistryBoundItems()
        {
            if (!UacHelper.IsProcessElevated)
            {
                registryBoundPreferences.IsEnabled = false;
                elevatePermissionsPanel.Visibility = Visibility.Visible;
            }
            try // We know we don't have write access, but we might have read access
            {
                if (!Registry.ClassesRoot.GetSubKeyNames().Contains(".torrent"))
                    torrentAssociationCheckBox.IsChecked = false;
                else
                {
                    using (var torrent = Registry.ClassesRoot.OpenSubKey(".torrent"))
                        torrentAssociationCheckBox.IsChecked = torrent.GetValue(null).ToString() == "Patchy";
                }
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
            catch { }
        }

        private void clearTorrentCacheClick(object sender, RoutedEventArgs e)
        {
            App.ClearCacheOnExit = true;
            MessageBox.Show("Cache will be deleted on exit.");
        }

        private void torrentAssociationCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            if (torrentAssociationCheckBox.IsChecked.Value)
            {
                RegisterApplication(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                AssociateTorrents(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            }
            else
            {
                if (Registry.ClassesRoot.GetSubKeyNames().Contains(".torrent"))
                    Registry.ClassesRoot.DeleteSubKey(".torrent");
            }
        }

        private static void RegisterApplication(string installationPath)
        {
            var path = Path.Combine(installationPath, "Patchy.exe");
            var startup = string.Format("\"{0}\" \"%1\"", path);

            // Register app path
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths\Patchy.exe", null, path);
            // Register application
            using (var applications = Registry.ClassesRoot.CreateSubKey("Applications"))
            using (var patchy = applications.CreateSubKey("Patchy.exe"))
            {
                patchy.SetValue("FriendlyAppName", "Patchy BitTorrent Client");
                using (var types = patchy.CreateSubKey("SupportedTypes"))
                    types.SetValue(".torrent", string.Empty);
                using (var shell = patchy.CreateSubKey("shell"))
                using (var open = shell.CreateSubKey("Open"))
                {
                    open.SetValue(null, "Download with Patchy");
                    using (var command = open.CreateSubKey("command"))
                        command.SetValue(null, startup);
                }
            }
        }

        private static void AssociateTorrents(string installationPath)
        {
            var path = Path.Combine(installationPath, "Patchy.exe");
            var startup = string.Format("\"{0}\" \"%1\"", path);

            using (var torrent = Registry.ClassesRoot.CreateSubKey(".torrent"))
            {
                torrent.SetValue(null, "Patchy.exe");
                torrent.SetValue("Content Type", "application/x-bittorrent");
            }
            using (var patchy = Registry.ClassesRoot.CreateSubKey("Patchy.exe"))
            {
                patchy.SetValue(null, "BitTorrent File");
                patchy.SetValue("DefaultIcon", path + ",0");
                using (var shell = patchy.CreateSubKey("shell"))
                using (var open = shell.CreateSubKey("open"))
                using (var command = open.CreateSubKey("command"))
                    command.SetValue(null, startup);
            }
            ShellNotification.NotifyOfChange();
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
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
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

        private void proxyInfoClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://sircmpwn.github.com/Patchy/proxy.html");
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

        private void EncryptionSettingsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (encryptionSettingsComboBox.SelectedIndex == 0)
                Settings.EncryptionSettings = EncryptionTypes.PlainText;
            else if (encryptionSettingsComboBox.SelectedIndex == 1)
                Settings.EncryptionSettings = EncryptionTypes.All;
            else
                Settings.EncryptionSettings = EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;
        }

        private void seedingTorrentDoubleClickComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // NOTE: This can probably be data bound, investigate
            if (Settings == null) return;
            Settings.DoubleClickSeeding = (DoubleClickAction)seedingTorrentDoubleClickComboBox.SelectedIndex;
        }

        private void downloadingTorrentDoubleClickComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Settings == null) return;
            Settings.DoubleClickDownloading = (DoubleClickAction)downloadingTorrentDoubleClickComboBox.SelectedIndex;
        }

        private void randomizeIncomingPortClicked(object sender, RoutedEventArgs e)
        {
            Settings.IncomingPort = new Random().Next(0, 65536);
        }

        private void proxyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.ProxyPassword = proxyPasswordBox.Password;
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
                        Settings.RssFeeds = Settings.RssFeeds.Concat(new[] { rss }).ToArray();
                        Settings.OnPropertyChanged("RssFeeds");
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
            var path = Settings.DefaultDownloadLocation;
            if (rssOtherLocationRadioButton.IsChecked.Value)
                path = rssSaveDirectoryTextBox.Text;
            if (!Directory.Exists(path))
            {
                MessageBox.Show("The specified directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            var rule = new RssTorrentRule(type, regex);
            rule.Label = (rssLabelComboBox.SelectedItem as ComboBoxItem).Tag as TorrentLabel;
            rule.DownloadPath = path;
            feed.TorrentRules.Add(rule);
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
                Settings.RssFeeds = Settings.RssFeeds.Where(f => f != feed).ToArray();
            Settings.OnPropertyChanged("RssFeeds");
        }

        private void regexHelpButtonClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://sircmpwn.github.com/Patchy/regex.html");
        }

        #endregion

        #region Automatic Directories

        private void removeSelectedAutomaticDirectoriesClicked(object sender, RoutedEventArgs e)
        {
            var items = automaticAddDirectoryListBox.SelectedItems.Cast<WatchedDirectory>();
            Settings.WatchedDirectories = Settings.WatchedDirectories.Where(d => !items.Contains(d)).ToArray();
        }

        private void automaticAddButtonClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(automaticAddTextBox.Text))
            {
                MessageBox.Show("Please enter a directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (Settings.WatchedDirectories.Any(w => w.Path == automaticAddTextBox.Text))
            {
                MessageBox.Show("This directory has already been added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var watch = new WatchedDirectory();
            watch.Path = automaticAddTextBox.Text;
            watch.Label = (automaticLabelComboBox.SelectedItem as ComboBoxItem).Tag as TorrentLabel;
            Settings.WatchedDirectories = Settings.WatchedDirectories.Concat(new[] { watch }).ToArray();
            automaticAddTextBox.Text = string.Empty;
        }

        #endregion
    }
}
