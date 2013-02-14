using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Patchy
{
    public class RssFeedEntry
    {
        public string Title { get; set; }
        public DateTime PublishTime { get; set; }
        public string Link { get; set; }
        public string Creator { get; set; }
    }
}
