using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using Patchy.IPC;

namespace Installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly string SingletonGuid = "B11931EB-32BC-441F-BF57-859FE282236A";
        private Mutex Singleton { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Check for running instances of Patchy and kill them
            bool isInitialInstance;
            Singleton = new Mutex(true, "Patchy:" + SingletonGuid, out isInitialInstance);
            if (!isInitialInstance)
                KillCurrentInstance();
            Singleton.Close();
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

        private void KillCurrentInstance()
        {
            try
            {
                var serviceFactory = new ChannelFactory<ISingletonService>(
                        new NetNamedPipeBinding() { SendTimeout = TimeSpan.FromSeconds(1) }, new EndpointAddress("net.pipe://localhost/patchy/singleton"));
                var service = serviceFactory.CreateChannel();
                service.Shutdown();
            } catch { }
        }
    }
}
