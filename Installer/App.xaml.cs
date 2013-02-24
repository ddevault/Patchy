using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace Installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check for permissions
            if (!Patchy.UacHelper.IsProcessElevated && !Debugger.IsAttached)
            {
                var info = new ProcessStartInfo(Assembly.GetEntryAssembly().Location);
                info.Verb = "runas";
                Process.Start(info);
                Application.Current.Shutdown();
                return;
            }
            // Check for .NET 4.0
            var value = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Install", null);
            if (value == null)
            {
                var result = MessageBox.Show("You must install .NET 4.0 to run Patchy. Would you like to do so now?", 
                    "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                    Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=17851");
                Application.Current.Shutdown();
                return;
            }
        }
    }
}
