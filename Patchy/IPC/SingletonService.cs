using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Patchy.IPC
{
    [ServiceContract]
    public interface ISingletonService
    {
        [OperationContract]
        void HandleArguments(string[] args);
    }

    public class SingletonService : ISingletonService
    {
        public static MainWindow Window { get; set; }

        public void HandleArguments(string[] args)
        {
            Window.HandleArguments(args);
        }
    }
}
