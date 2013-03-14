using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MonoTorrent;
using MonoTorrent.Common;

namespace Patchy.IPC
{
    [ServiceContract]
    public interface ISingletonService
    {
        [OperationContract]
        void HandleArguments(string[] args);
        [OperationContract]
        void Shutdown();
        [OperationContract]
        bool AddTorrent(byte[] file, string destination);
        [OperationContract]
        bool AddMagnetLink(string link, string destination);
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

        public bool AddTorrent(byte[] file, string destination)
        {
            try
            {
                var torrent = Torrent.Load(file);
                Window.AddTorrent(torrent, destination, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddMagnetLink(string link, string destination)
        {
            try
            {
                var torrent = new MagnetLink(link);
                Window.AddTorrent(torrent, destination, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
