using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Patchy.IPC
{
    [ServiceContract]
    public interface ISingletonService
    {
        [OperationContract]
        void HandleArguments(string[] args);
        [OperationContract]
        void Shutdown();
    }

    public class SingletonService : ISingletonService
    {
        public static MainWindow Window { get; set; }

        public void HandleArguments(string[] args)
        {
            Window.HandleArguments(args);
        }

        public void Shutdown()
        {
            Window.AllowClose = true;
            Window.ForceClose = true;
            Window.Close();
            Application.Current.Shutdown();
        }
    }
}
