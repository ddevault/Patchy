using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Patchy.IPC;
using System.IO;
using System.Diagnostics;
using System.Windows.Threading;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool ClearCacheOnExit { get; set; }

        private Mutex Singleton { get; set; }
        private readonly string SingletonGuid = "B11931EB-32BC-441F-BF57-859FE282236A";
        private ServiceHost SingletonServcieHost { get; set; }

        internal void ShutdownSingleton()
        {
            Singleton.ReleaseMutex();
            Singleton.Dispose();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isInitialInstance;
            Singleton = new Mutex(true, "Patchy:" + SingletonGuid, out isInitialInstance);
            if (!isInitialInstance)
            {
                PassArgumentsToInstance(e.Args);
                Current.Shutdown();
                return;
            }
            MainWindow = new MainWindow();
            CreateServiceHost();
            (MainWindow as MainWindow).HandleArguments(e.Args);
            MainWindow.Show();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var window = new UnhandledExceptionWindow(e.Exception);
            window.ShowDialog();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var window = new UnhandledExceptionWindow(e.ExceptionObject as Exception);
            window.ShowDialog();
        }

        private void CreateServiceHost()
        {
            SingletonService.Window = (MainWindow)MainWindow;
            SingletonServcieHost = new ServiceHost(typeof(SingletonService), new Uri("net.pipe://localhost/patchy"));
            SingletonServcieHost.AddServiceEndpoint(typeof(ISingletonService), new NetNamedPipeBinding(), "singleton");
            SingletonServcieHost.Open();
        }

        private void PassArgumentsToInstance(string[] args)
        {
            try
            {
                var serviceFactory = new ChannelFactory<ISingletonService>(
                    new NetNamedPipeBinding(), new EndpointAddress("net.pipe://localhost/patchy/singleton"));
                var service = serviceFactory.CreateChannel();
                service.HandleArguments(args);
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (SingletonServcieHost != null)
                SingletonServcieHost.Close();
            if (ClearCacheOnExit)
                Directory.Delete(SettingsManager.TorrentCachePath, true);
            try
            {
                Singleton.ReleaseMutex();
                Singleton.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
