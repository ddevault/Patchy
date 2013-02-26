using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for UnhandledExceptionWindow.xaml
    /// </summary>
    public partial class UnhandledExceptionWindow : Window
    {
        public Exception Exception { get; set; }
        public string Details { get; set; }

        public UnhandledExceptionWindow(Exception exception)
        {
            InitializeComponent();
            Exception = exception;
            headerTextBlock.Text = "Unhandled " + exception.GetType().Name + " occured!";
            Details = string.Format("Stack trace:" + Environment.NewLine +
                "{0}" + Environment.NewLine + Environment.NewLine +
                "OS Name: {1}" + Environment.NewLine +
                "Edition: {2}" + Environment.NewLine +
                "Service Pack: {3}" + Environment.NewLine +
                "Version: {4}" + Environment.NewLine +
                "Architecture: {5} bit",
                Exception.ToString(), OSInfo.Name, OSInfo.Edition, OSInfo.ServicePack, OSInfo.Version, OSInfo.Bits);
            technobabbleTextBox.Text = Details;
            technobabbleTextBox.Focus();
            technobabbleTextBox.SelectAll();
            technobabbleTextBox.ScrollToLine(0);
        }

        private void technobabbleTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // All this extra junk is an easy way to delay execution until after the textbox has had its say over the selection
            Task.Factory.StartNew(() => Dispatcher.BeginInvoke(new Action(() => { technobabbleTextBox.SelectAll(); technobabbleTextBox.ScrollToLine(0); })));
        }

        private void closePatchyClicked(object sender, RoutedEventArgs e)
        {
            // We can't predict the current (broken) state of the application, so we don't try to shut down cleanly
            // In theory, they should be able to recover from this anyway without losing their session
            Process.GetCurrentProcess().Kill();
            while (true) ;
        }

        private void carryOnClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void copyDetailsClicked(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Details);
        }

        private void createGithubIssueClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(string.Format("https://github.com/SirCmpwn/Patchy/issues/new?title={0}&body={1}",
                Uri.EscapeUriString("Unhandled Exception: " + Exception.GetType().Name),
                Uri.EscapeUriString(Details)));
        }

        private void emailDevelopersClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(string.Format("mailto:patchy@sircmpwn.com?subject={0}&body={1}",
                Uri.EscapeUriString("Unhandled Exception: " + Exception.GetType().Name),
                Uri.EscapeUriString(Details)));
        }

        private void reportToRedditClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(string.Format("http://www.reddit.com/r/Patchy/submit?selftext=true&title=%5BBug%5D+{0}&text={1}",
                Uri.EscapeUriString("Unhandled Exception: " + Exception.GetType().Name),
                Uri.EscapeUriString(Details)));
        }

        private void chatOnIrcClicked(object sender, RoutedEventArgs e)
        {
            Process.Start("http://webchat.freenode.net/?channels=patchy");
        }

        private void consolatoryHugClicked(object sender, RoutedEventArgs e)
        {
            Process.Start("http://thenicestplaceontheinter.net");
        }
    }
}
