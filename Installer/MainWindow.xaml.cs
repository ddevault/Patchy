using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MonoTorrent.BEncoding;
using Patchy;
using Newtonsoft.Json;
using vbAccelerator.Components.Shell;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.LICENSE"));
            licenseText.Text = reader.ReadToEnd();
            reader.Close();
            installPathTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Patchy");
            // Check for an existing install
            var priorInstall = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Patchy", "Path", null) as string;
            if (!string.IsNullOrEmpty(priorInstall))
                installPathTextBox.Text = priorInstall;
            var uTorrent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent");
            var transmission = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "transmission");

            uTorrentImportCheckBox.IsChecked = Directory.Exists(uTorrent);
            transmissionImportCheckBox.IsChecked = Directory.Exists(transmission);
        }

        private void previousButtonClick(object sender, RoutedEventArgs e)
        {
            progressTabs.SelectedIndex--;
            if (progressTabs.SelectedIndex != progressTabs.Items.Count - 1)
                nextButton.Content = "Next";
            previousButton.IsEnabled = (progressTabs.SelectedIndex != 0);
        }

        private void nextButtonClick(object sender, RoutedEventArgs e)
        {
            if (progressTabs.SelectedIndex == 1) // Install path
            {
                if (!Directory.Exists(installPathTextBox.Text))
                    Directory.CreateDirectory(installPathTextBox.Text);
            }
            if (progressTabs.SelectedIndex == progressTabs.Items.Count - 1)
            {
                // "Finish"
                FinishInstallation();
            }
            progressTabs.SelectedIndex++;
            if (progressTabs.SelectedIndex == progressTabs.Items.Count - 1)
                nextButton.Content = "Finish";
            previousButton.IsEnabled = (progressTabs.SelectedIndex != 0);
        }

        private void FinishInstallation()
        {
            // Copy files
            var path = installPathTextBox.Text;
            CopyFileFromAssembly("MonoTorrent.Dht.dll", path);
            CopyFileFromAssembly("MonoTorrent.dll", path);
            CopyFileFromAssembly("Newtonsoft.Json.dll", path);
            CopyFileFromAssembly("Patchy.exe", path);
            CopyFileFromAssembly("Uninstaller.exe", path);
            CopyFileFromAssembly("Xceed.Wpf.Toolkit.dll", path);
            // Associations
            RegisterApplication(path);
            RegisterUninstaller(path);
            if (torrentAssociationCheckBox.IsChecked.Value)
                AssociateTorrents(path);
            if (magnetAssociationCheckBox.IsChecked.Value)
                AssociateMagnetLinks(path);
            if (startWithWindowsCheckBox.IsChecked.Value)
                SetToStartup(path);
            if (addToStartMenuCheckBox.IsChecked.Value)
                CreateStartMenuIcon(path);
            // Import torrents
            ImportTorrents();

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Patchy", "Path", path);
            Process.Start(Path.Combine(path, "Patchy.exe"));
            Close();
        }

        private void ImportTorrents()
        {
            var patchy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".patchy");
            var torrentcache = Path.Combine(patchy, "torrentcache");

            var uTorrent = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "uTorrent");
            var transmission = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "transmission");

            var serializer = new JsonSerializer();

            Directory.CreateDirectory(patchy);
            Directory.CreateDirectory(Path.Combine(patchy, torrentcache));

            if (uTorrentImportCheckBox.IsChecked.Value)
            {
                try
                {
                    if (Directory.Exists(uTorrent))
                    {
                        var torrents = Directory.GetFiles(uTorrent, "*.torrent");
                        BEncodedDictionary dictionary;
                        if (File.Exists(Path.Combine(uTorrent, "resume.dat")))
                        {
                            using (var stream = File.OpenRead(Path.Combine(uTorrent, "resume.dat")))
                                dictionary = (BEncodedDictionary)BEncodedDictionary.Decode(stream);
                            foreach (var key in dictionary.Keys)
                            {
                                if (key.Text.EndsWith(".torrent"))
                                {
                                    if (File.Exists(Path.Combine(uTorrent, key.Text)))
                                    {
                                        // Add torrent
                                        var torrent = Path.Combine(uTorrent, key.Text);
                                        var info = new TorrentInfo
                                        {
                                            Label = new TorrentLabel("µTorrent", "#00853F") { Foreground = "#FFFFFF" },
                                            Path = ((BEncodedDictionary)dictionary[key.Text])["path"].ToString(),
                                            UploadSlots = 4,
                                            IsRunning = true
                                        };
                                        if (!File.Exists(Path.Combine(torrentcache, Path.GetFileName(torrent))))
                                        {
                                            using (var json = new StreamWriter(Path.Combine(torrentcache,
                                                Path.GetFileNameWithoutExtension(torrent) + ".info")))
                                                serializer.Serialize(new JsonTextWriter(json), info);
                                            File.Copy(torrent, Path.Combine(torrentcache, Path.GetFileName(torrent)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { MessageBox.Show("Failed to import from uTorrent.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            if (transmissionImportCheckBox.IsChecked.Value)
            {
                try
                {
                    if (Directory.Exists(transmission))
                    {
                        var torrents = Directory.GetFiles(Path.Combine(transmission, "Torrents"), "*.torrent");
                        var dictionaries = Path.Combine(transmission, "Resume");
                        foreach (var torrent in torrents)
                        {
                            if (File.Exists(Path.Combine(dictionaries,
                                Path.GetFileNameWithoutExtension(torrent) + ".resume")))
                            {
                                BEncodedDictionary dictionary;
                                using (var stream = File.OpenRead(Path.Combine(dictionaries,
                                    Path.GetFileNameWithoutExtension(torrent) + ".resume")))
                                    dictionary = (BEncodedDictionary)BEncodedDictionary.Decode(stream);
                                // Add torrent
                                var name = dictionary["name"].ToString();
                                var info = new TorrentInfo
                                {
                                    Label = new TorrentLabel("Transmission", "#DA0000") { Foreground = "#FFFFFF" },
                                    Path = dictionary["destination"].ToString(),
                                    UploadSlots = 4,
                                    IsRunning = true
                                };
                                using (var json = new StreamWriter(Path.Combine(torrentcache, name + ".info")))
                                    serializer.Serialize(new JsonTextWriter(json), info);
                                File.Copy(torrent, Path.Combine(torrentcache, name + ".torrent"));
                            }
                        }
                    }
                }
                catch { MessageBox.Show("Failed to import from Transmission.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void SetToStartup(string path)
        {
            path = Path.Combine(path, "Patchy.exe");
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (startMinimizedCheckBox.IsChecked.Value)
                key.SetValue("Patchy", "\"" + path + "\" --minimized");
            else
                key.SetValue("Patchy", "\"" + path + "\"");
            key.Close();
        }

        private void CreateStartMenuIcon(string path)
        {
            var startPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            using (ShellLink shortcut = new ShellLink())
            {
                shortcut.Target = Path.Combine(path, "Patchy.Exe");
                shortcut.Description = "Patchy BitTorrent Client";
                shortcut.WorkingDirectory = path;
                shortcut.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
                shortcut.Save(Path.Combine(startPath, "Patchy.lnk"));
            }
        }

        private void AssociateMagnetLinks(string path)
        {
            path = Path.Combine(path, "Patchy.exe");
            var registryPath = string.Format("\"{0}\" \"%1\"", path);

            using (var key = Registry.ClassesRoot.CreateSubKey("Magnet"))
            {
                key.SetValue(null, "Magnet URI");
                key.SetValue("Content Type", "application/x-magnet");
                key.SetValue("URL Protocol", string.Empty);
                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    defaultIcon.SetValue(null, path);
                using (var shell = key.CreateSubKey("shell"))
                {
                    shell.SetValue(null, "open");
                    using (var open = shell.CreateSubKey("open"))
                        using (var command = open.CreateSubKey("command"))
                            command.SetValue(null, registryPath);
                }
            }
        }

        private static void RegisterApplication(string installationPath)
        {
            var path = Path.Combine(installationPath, "Patchy.exe");
            var startup = string.Format("\"{0}\" \"%1\"", path);

            // Register app path
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\App Paths\Patchy.exe", null, path);
            // Register application
            using (var applications = Registry.ClassesRoot.CreateSubKey("Applications"))
                using (var patchy = applications.CreateSubKey("Patchy.exe"))
                {
                    patchy.SetValue("FriendlyAppName", "Patchy BitTorrent Client");
                    using (var types = patchy.CreateSubKey("SupportedTypes"))
                        types.SetValue(".torrent", string.Empty);
                    using (var shell = patchy.CreateSubKey("shell"))
                        using (var open = shell.CreateSubKey("Open"))
                        {
                            open.SetValue(null, "Download with Patchy");
                            using (var command = open.CreateSubKey("command"))
                                command.SetValue(null, startup);
                        }
                }
        }

        public void RegisterUninstaller(string installPath)
        {
            using (RegistryKey parent = Registry.LocalMachine.CreateSubKey(
                 @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                RegistryKey key = null;

                try
                {
                    key = parent.CreateSubKey("Patchy");

                    key.SetValue("DisplayName", "Patchy BitTorrent Client");
                    key.SetValue("ApplicationVersion", "1.0");
                    key.SetValue("Publisher", "Drew DeVault");
                    key.SetValue("DisplayIcon", Path.Combine(installPath, "Patchy.exe"));
                    key.SetValue("DisplayVersion", "1.0");
                    key.SetValue("URLInfoAbout", "http://sircmpwn.github.com/Patchy");
                    key.SetValue("Contact", "sir@cmpwn.com");
                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                    key.SetValue("UninstallString", Path.Combine(installPath, "Uninstaller.exe"));
                }
                finally
                {
                    if (key != null)
                    {
                        key.Close();
                    }
                }
            }
        }

        private static void AssociateTorrents(string installationPath)
        {
            var path = Path.Combine(installationPath, "Patchy.exe");
            var startup = string.Format("\"{0}\" \"%1\"", path);

            using (var torrent = Registry.ClassesRoot.CreateSubKey(".torrent"))
            {
                torrent.SetValue(null, "Patchy.exe");
                torrent.SetValue("Content Type", "application/x-bittorrent");
            }
            using (var patchy = Registry.ClassesRoot.CreateSubKey("Patchy.exe"))
            {
                patchy.SetValue(null, "BitTorrent File");
                patchy.SetValue("DefaultIcon", path + ",0");
                using (var shell = patchy.CreateSubKey("shell"))
                    using (var open = shell.CreateSubKey("open"))
                        using (var command = open.CreateSubKey("command"))
                            command.SetValue(null, startup);
            }
            ShellNotification.NotifyOfChange();
        }

        private void CopyFileFromAssembly(string file, string path)
        {
            var stream = App.GetEmbeddedResource(file);
            using (var destination = File.Create(Path.Combine(path, file)))
                Extensions.CopyTo(stream, destination);
            stream.Close();
        }

        private void browseSourceClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SirCmpwn/Patchy");
        }

        private void browseInstallLocationClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                installPathTextBox.Text = dialog.SelectedPath;
        }
    }
}
