using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Patchy
{
    public class RssFeed
    {
        public RssFeed()
        {
            TorrentRules = new ObservableCollection<RssTorrentRule>();
        }

        public RssFeed(string address) : this()
        {
            Address = address;
        }

        public string Address { get; set; }
        public ObservableCollection<RssTorrentRule> TorrentRules { get; set; }

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
