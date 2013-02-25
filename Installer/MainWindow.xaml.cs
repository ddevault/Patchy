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
using BrendanGrant.Helpers.FileAssociation;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using IWshFile = IWshRuntimeLibrary.File;
using File = System.IO.File;
using System.ComponentModel;
using System.Runtime.InteropServices;
using MonoTorrent.BEncoding;
using Patchy;
using Newtonsoft.Json;

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

            uTorrentImportCheckBox.IsChecked = Directory.Exists(uTorrent);
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
                {
                    var result = MessageBox.Show("The specified directory does not exist. Create it?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Directory.CreateDirectory(installPathTextBox.Text);
                        }
                        catch
                        {
                            MessageBox.Show("Unable to create directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                        return;
                }
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
            CopyFileFromAssembly("FileAssociation.dll", path);
            CopyFileFromAssembly("MonoTorrent.Dht.dll", path);
            CopyFileFromAssembly("MonoTorrent.dll", path);
            CopyFileFromAssembly("Newtonsoft.Json.dll", path);
            CopyFileFromAssembly("Patchy.exe", path);
            CopyFileFromAssembly("Xceed.Wpf.Toolkit.dll", path);
            // Associations
            if (torretnAssociationCheckBox.IsChecked.Value)
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
                            foreach (var torrent in torrents)
                            {
                                if (dictionary.ContainsKey(Path.GetFileName(torrent)))
                                {
                                    // Add torrent
                                    var info = new TorrentInfo
                                    {
                                        Label = new TorrentLabel("µTorrent", "#00853F") { Foreground = "#FFFFFF" },
                                        Path = ((BEncodedDictionary)dictionary[Path.GetFileName(torrent)])["path"].ToString()
                                    };
                                    using (var json = new StreamWriter(Path.Combine(torrentcache,
                                        Path.GetFileNameWithoutExtension(torrent) + ".info")))
                                        serializer.Serialize(new JsonTextWriter(json), info);
                                    File.Copy(torrent, Path.Combine(torrentcache, Path.GetFileName(torrent)));
                                }
                            }
                        }
                    }
                }
                catch { MessageBox.Show("Failed to import from uTorrent.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
            WshShell shell = new WshShell();
            IWshShortcut shortcut;
            var startPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            shortcut = (IWshShortcut)shell.CreateShortcut(Path.Combine(startPath, "Patchy.lnk"));
            shortcut.TargetPath = Path.Combine(path, "Patchy.exe");
            shortcut.Description = "Launch the App!";
            shortcut.IconLocation = Path.Combine(path, "Patchy.exe");
            shortcut.Save(); 
        }

        private void AssociateMagnetLinks(string path)
        {
            path = Path.Combine(path, "Patchy.exe");
            var registryPath = string.Format("\"{0}\" \"%1\"", path);
            var key = Registry.ClassesRoot.CreateSubKey("Magnet");
            key.SetValue(null, "Magnet URI");
            key.SetValue("Content Type", "application/x-magnet");
            key.SetValue("URL Protocol", string.Empty);
            var defaultIcon = key.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue(null, path);
            var shell = key.CreateSubKey("shell");
            shell.SetValue(null, "open");
            var open = shell.CreateSubKey("open");
            var command = open.CreateSubKey("command");
            command.SetValue(null, registryPath);
            command.Close(); open.Close(); shell.Close(); defaultIcon.Close(); key.Close();
        }

        private static void AssociateTorrents(string installationPath)
        {
            var path = Path.Combine(installationPath, "Patchy.exe");
            var torrent = new FileAssociationInfo(".torrent");
            torrent.Create("Patchy");
            torrent.ContentType = "application/x-bittorrent";
            torrent.OpenWithList = new[] { "patchy.exe" };
            var program = new ProgramAssociationInfo(torrent.ProgID);
            if (!program.Exists)
            {
                program.Create("Patchy Torrent File", new ProgramVerb("Open", string.Format(
                    "\"{0}\" \"%1\"", path)));
                program.DefaultIcon = new ProgramIcon(path);
            }
        }

        private void CopyFileFromAssembly(string file, string path)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Installer.Binaries." + file);
            using (var destination = File.Create(Path.Combine(path, file)))
                Extensions.CopyTo(stream, destination);
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
