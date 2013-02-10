using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;

namespace Patchy
{
    public class ClientManager
    {
        public void Initialize()
        {
            Torrents = new List<TorrentWrapper>();

            // TODO: Customize most of these settings
            var settings = new EngineSettings(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), 22239);
            settings.PreferEncryption = true;
            settings.AllowedEncryption = EncryptionTypes.RC4Full | EncryptionTypes.RC4Header;
            Client = new ClientEngine(settings);
            Client.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, 22239));
            var listener = new DhtListener(new IPEndPoint(IPAddress.Any, 22239));
            var dht = new DhtEngine(listener);
            Client.RegisterDht(dht);
            listener.Start();
            Client.DhtEngine.Start();
        }

        public void AddTorrent(TorrentWrapper torrent)
        {
            Torrents.Add(torrent);
            torrent.Index = Torrents.Count;
            Client.Register(torrent);
            torrent.Start();
        }

        public List<TorrentWrapper> Torrents { get; set; }

        private static ClientEngine Client { get; set; }
    }

    public class TorrentWrapper : TorrentManager
    {
        public string Name { get; private set; }
        public long Size { get; private set; }
        public int Index { get; set; }

        public TorrentWrapper(Torrent torrent, string savePath, TorrentSettings settings)
            : base(torrent, savePath, settings)
        {
            Name = torrent.Name;
            Size = torrent.Size;
        }

        public TorrentWrapper(MagnetLink magnetLink, string savePath, TorrentSettings settings, string torrentSave)
            : base(magnetLink, savePath, settings, torrentSave)
        {
            Name = magnetLink.Name;
            Size = -1;
        }
    }
}
