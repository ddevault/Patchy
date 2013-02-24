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
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
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
            }
            progressTabs.SelectedIndex++;
            if (progressTabs.SelectedIndex == progressTabs.Items.Count - 1)
                nextButton.Content = "Finish";
            previousButton.IsEnabled = (progressTabs.SelectedIndex != 0);
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
