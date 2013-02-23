using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent;
using MonoTorrent.Client;
using System.Windows.Controls;
using System.Windows;
using System.Threading;
using System.Windows.Input;

namespace Patchy
{
    public partial class MainWindow
    {
        private IEnumerable<RssFeedEntry> RssEntries { get; set; }
        private Timer UpdateRssTimer { get; set; }

        private void ReloadRssTimer()
        {
            UpdateRssTimer = new Timer(o => UpdateRss(), null, 1000, 
                SettingsManager.MinutesBetweenRssUpdates * 60 * 1000);
        }

        private void UpdateRss()
        {
            // Runs outside UI thread
            var entries = new List<RssFeedEntry>();
            var newTorrents = new List<RssFeedEntry>();
            if (SettingsManager.RssFeeds == null) return;
            foreach (var feed in SettingsManager.RssFeeds)
            {
                try
                {
                    var diff = feed.Update();
                    foreach (var item in diff)
                    {
                        foreach (var rule in feed.TorrentRules)
                        {
                            if (rule.Type == RssTorrentRule.RuleType.Title)
                            {
                                if (rule.Regex.IsMatch(item.Title))
                                    newTorrents.Add(item);
                            }
                            else if (rule.Type == RssTorrentRule.RuleType.CreatedBy)
                            {
                                if (rule.Regex.IsMatch(item.Creator))
                                    newTorrents.Add(item);
                            }
                        }
                    }
                    entries.AddRange(feed.Entries);
                }
                catch { }
            }
            entries = new List<RssFeedEntry>(entries.OrderBy(e => e.PublishTime).ToArray());
            RssEntries = entries;
            Dispatcher.BeginInvoke(new Action(() => rssListView.ItemsSource = RssEntries));
            // Add new torrents
            foreach (var torrentEntry in newTorrents)
            {
                try
                {
                    var magnetLink = new MagnetLink(torrentEntry.Link);
                    Dispatcher.BeginInvoke(new Action(() =>
                        {
                            BalloonTorrent = null;
                            NotifyIcon.ShowBalloonTip(5000, "Added torrent from feed", torrentEntry.Title, System.Windows.Forms.ToolTipIcon.Info);
                            AddTorrent(magnetLink, SettingsManager.DefaultDownloadLocation, true);
                        }));
                }
                catch { }
            }
        }

        private void rssEntryAddClicked(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button);
            var entry = (RssFeedEntry)button.Tag;
            var magnetLink = new MagnetLink(entry.Link);
            if (!SettingsManager.PromptForSaveOnShellLinks)
                AddTorrent(magnetLink, SettingsManager.DefaultDownloadLocation);
            else
            {
                var window = new AddTorrentWindow(SettingsManager);
                window.MagnetLink = magnetLink;
                if (window.ShowDialog().Value)
                    AddTorrent(window.MagnetLink, window.DestinationPath);
            }
        }
    }
}
