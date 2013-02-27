using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using Patchy.IPC;
using System.ServiceModel;

namespace Uninstaller
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
        }

        private void KillCurrentInstance()
        {
            try
            {
                var serviceFactory = new ChannelFactory<ISingletonService>(
                        new NetNamedPipeBinding() { SendTimeout = TimeSpan.FromSeconds(1) }, new EndpointAddress("net.pipe://localhost/patchy/singleton"));
                var service = serviceFactory.CreateChannel();
                service.Shutdown();
            }
            catch { }
        }
    }
}
