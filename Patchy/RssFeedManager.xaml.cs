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
    }
}
