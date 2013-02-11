using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Patchy
{
    public class SettingsManager
    {
        public static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".patchy");
            }
        }

        public static string FastResumePath
        {
            get
            {
                return Path.Combine(SettingsPath, "fastresume");
            }
        }

        internal static void Initialize()
        {
            if (!Directory.Exists(SettingsPath))
                Directory.CreateDirectory(SettingsPath);
        }
    }
}
