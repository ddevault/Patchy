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

namespace Patchy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex Singleton { get; set; }
        private readonly Guid SingletonGuid = Guid.Parse("B11931EB-32BC-441F-BF57-859FE282236A");
        private ServiceHost SingletonServcieHost { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool isInitialInstance;
            Singleton = new Mutex(true, "Patchy:" + SingletonGuid.ToString(), out isInitialInstance);
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
            base.OnExit(e);
        }
    }
}
