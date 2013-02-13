using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

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
    }
}
