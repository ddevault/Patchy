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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Net;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for RssFeedManager.xaml
    /// </summary>
    public partial class RssFeedManager : Window
    {
        public ObservableCollection<RssFeed> RssFeeds { get; set; }

        public RssFeedManager(List<RssFeed> rssFeeds)
        {
            RssFeeds = new ObservableCollection<RssFeed>(rssFeeds);
            InitializeComponent();
            layoutRoot.DataContext = RssFeeds;
        }

        private void addNewFeedButtonClick(object sender, RoutedEventArgs e)
        {
            addNewFeedButton.IsEnabled = newFeedUrlTextBox.IsEnabled = false;
            var address = newFeedUrlTextBox.Text;
            if (RssFeeds.Any(f => f.Address == address))
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
                                RssFeeds.Add(rss);
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
                regex = new Regex(ruleRegexTextBox.Text);
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
                RssFeeds.Remove(feed);
        }
    }
}
