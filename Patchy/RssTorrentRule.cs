using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Patchy
{
    public class RssTorrentRule
    {
        public enum RuleType
        {
            Title = 0,
            CreatedBy = 1
            // TODO: Offer more options?
        }

        public RssTorrentRule()
        {
        }

        public RssTorrentRule(RuleType type, Regex regex)
        {
            Type = type;
            Regex = regex;
        }

        public RuleType Type { get; set; }
        public Regex Regex { get; set; }
        public TorrentLabel Label { get; set; }
    }
}
