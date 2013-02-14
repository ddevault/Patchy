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
using BrendanGrant.Helpers.FileAssociation;
using System.Reflection;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public PreferencesWindow()
        {
            InitializeComponent();
            InitializeRegistryBoundItems();
        }

        private void InitializeRegistryBoundItems()
        {
            if (!UacHelper.IsProcessElevated)
            {
                registryBoundPreferences.IsEnabled = false;
                elevatePermissionsPanel.Visibility = Visibility.Visible;
                return;
            }
            var torrent = new FileAssociationInfo(".torrent");
            if (torrent.Exists)
                torrentAssociationCheckBox.IsChecked = torrent.ProgID == "Patchy";
            else
                torrentAssociationCheckBox.IsChecked = false;
            // Check magnet link association
            var value = Registry.GetValue(@"HKEY_CLASSES_ROOT\\Magnet", null, null);
            if (value == null)
                magnetAssociationCheckBox.IsChecked = false;
            else
            {
                var shell = (string)Registry.GetValue(@"HKEY_CLASSES_ROOT\Magnet\shell\open\command", null, null);
                magnetAssociationCheckBox.IsChecked = shell == string.Format("\"{0}\" \"%1\"", Assembly.GetEntryAssembly().Location);
            }
        }

        private void clearTorrentCacheClick(object sender, RoutedEventArgs e)
        {
            App.ClearCacheOnExit = true;
            MessageBox.Show("Cache will be deleted on exit.");
        }

        private void torrentAssociationCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            var torrent = new FileAssociationInfo(".torrent");
            if (torrentAssociationCheckBox.IsChecked.Value)
            {
                torrent.Create("Patchy");
                torrent.ContentType = "application/x-bittorrent";
                torrent.OpenWithList = new[] { "patchy.exe" };
                var program = new ProgramAssociationInfo(torrent.ProgID);
                if (!program.Exists)
                {
                    program.Create("Patchy Torrent Files", new ProgramVerb("Open", string.Format(
                        "\"{0}\" \"%1\"", Path.Combine(Directory.GetCurrentDirectory(), "Patchy.exe"))));
                    program.DefaultIcon = new ProgramIcon(Assembly.GetEntryAssembly().Location);
                }
            }
            else
                torrent.Delete();
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
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (startOnWindowsStartupCheckBox.IsChecked.Value)
                key.SetValue("Patchy", Assembly.GetEntryAssembly().Location);
            else
                key.DeleteValue("Patchy");
            key.Close();
        }
    }
}
