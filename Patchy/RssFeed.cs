using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Web;

namespace Patchy
{
    public class RssFeed
    {
        private static WebClient WebClient { get; set; }

        static RssFeed()
        {
            WebClient = new WebClient();
        }

        public RssFeed()
        {
            TorrentRules = new ObservableCollection<RssTorrentRule>();
            Entries = new List<RssFeedEntry>();
        }

        public RssFeed(string address) : this()
        {
            Address = address;
        }

        public string Address { get; set; }
        public ObservableCollection<RssTorrentRule> TorrentRules { get; set; }
        [JsonIgnore]
        public List<RssFeedEntry> Entries { get; set; }

        /// <summary>
        /// Returns all new entries
        /// </summary>
        public List<RssFeedEntry> Update()
        {
            List<RssFeedEntry> diff = new List<RssFeedEntry>();
            var dc = XNamespace.Get("http://purl.org/dc/elements/1.1/");
            var oldEntries = Entries.ToArray();
            try
            {
                var rawFeed = WebClient.DownloadString(Address);
                var feed = XDocument.Parse(rawFeed);
                var channel = feed.Root.Element("channel");
                Entries = new List<RssFeedEntry>();
                foreach (var item in channel.Elements("item"))
                {
                    Entries.Add(new RssFeedEntry
                    {
                        Title = HttpUtility.HtmlDecode(item.Element("title").Value),
                        Creator = item.Element(dc + "creator").Value,
                        Link = item.Element("link").Value,
                        PublishTime = DateTime.Parse(item.Element("pubDate").Value)
                    });
                }
                foreach (var entry in Entries)
                {
                    if (!oldEntries.Contains(entry))
                        diff.Add(entry);
                }
            }
            catch {  }
            return diff;
        }

        public static bool ValidateFeed(XDocument document)
        {
            var dc = XNamespace.Get("http://purl.org/dc/elements/1.1/");
            if (document.Root.Element("channel") == null)
                return false;
            var channel = document.Root.Element("channel");
            if (channel.Element("title") == null)
                return false;
            if (channel.Element("description") == null)
                return false;
            if (!channel.Elements("item").Any())
                return false;
            foreach (var item in channel.Elements("item"))
            {
                if (item.Element("title") == null)
                    return false;
                if (item.Element("link") == null)
                    return false;
                if (item.Element("pubDate") == null)
                    return false;
                if (item.Element(dc + "creator") == null)
                    return false;
            }
            return true;
        }
    }
}
