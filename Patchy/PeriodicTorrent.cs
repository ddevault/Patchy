using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using MonoTorrent.Client;
using MonoTorrent.Common;

namespace Patchy
{
    /// <summary>
    /// Periodically checks various properties and fires PropertyChanged
    /// as needed.
    /// </summary>
    public class PeriodicTorrent : INotifyPropertyChanged // TODO: Consider replacing TorrentWrapper entirely with this
    {
        public TorrentWrapper Torrent { get; set; }
        public bool CompletedOnAdd { get; set; }
        public bool NotifiedComplete { get; set; }
        public bool[] RecievedPieces { get; set; }
        public string CacheFilePath { get; set; }
        public PiecePicker PiecePicker { get; set; }

        public PeriodicTorrent(TorrentWrapper wrapper)
        {
            Torrent = wrapper;
            PeerList = new ObservableCollection<PeerId>();
            Update();
            Name = Torrent.Name;
            Size = Torrent.Size;
            CompletedOnAdd = Torrent.Complete;
            NotifiedComplete = false;
            PiecePicker = new RandomisedPicker(new StandardPicker());
            wrapper.PieceManager.BlockReceived += PieceManager_BlockReceived;
            wrapper.PieceHashed += wrapper_PieceHashed;
        }

        void wrapper_PieceHashed(object sender, PieceHashedEventArgs e)
        {
            if (RecievedPieces == null)
            {
                RecievedPieces = new bool[Torrent.Torrent.Pieces.Count];
                for (int i = 0; i < RecievedPieces.Length; i++)
                    RecievedPieces[i] = false;
            }
            if (e.HashPassed)
                RecievedPieces[e.PieceIndex] = true;
        }

        void PieceManager_BlockReceived(object sender, BlockEventArgs e)
        {
            if (RecievedPieces == null)
            {
                RecievedPieces = new bool[Torrent.Torrent.Pieces.Count];
                for (int i = 0; i < RecievedPieces.Length; i++)
                    RecievedPieces[i] = false;
            }
            RecievedPieces[e.Piece.Index] = true;
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("RecievedPieces"));
        }

        internal void Update()
        {
            if (Torrent.State == TorrentState.Seeding && State == TorrentState.Stopped)
                CompletedOnAdd = true;
            State = Torrent.State;
            Progress = Torrent.Progress;
            Complete = Torrent.Complete;
            DownloadSpeed = Torrent.Monitor.DownloadSpeed;
            UploadSpeed = Torrent.Monitor.UploadSpeed;
            ElapsedTime = DateTime.Now - Torrent.StartTime;
            if (Torrent.State == TorrentState.Metadata)
                EstimatedTime = TimeSpan.MaxValue;
            else
                EstimatedTime = new TimeSpan((long)((DateTime.Now - Torrent.StartTime).Ticks / (Torrent.Progress / 100)));
            TotalDownloaded = Torrent.Monitor.DataBytesDownloaded;
            TotalUploaded = Torrent.Monitor.DataBytesUploaded;
            Ratio = (double)Torrent.Monitor.DataBytesUploaded / Torrent.Monitor.DataBytesDownloaded;
            if ((Torrent.State == TorrentState.Downloading || Torrent.State == TorrentState.Seeding) && files == null)
            {
                files = new PeriodicFile[Torrent.Torrent.Files.Length];
                for (int i = 0; i < files.Length; i++)
                    files[i] = new PeriodicFile(Torrent.Torrent.Files[i]);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Files"));
            }
            if (Torrent.IsMagnet && (Torrent.State == TorrentState.Downloading || Torrent.State == TorrentState.Seeding) && Torrent.Size == -1)
                Size = Torrent.Torrent.Files.Select(f => f.Length).Aggregate((a, b) => a + b);
            if (files != null)
            {
                foreach (var file in files)
                    file.Update();
            }
            Peers = Torrent.Peers.Available;
            Seeders = Torrent.Peers.Seeds;
            Leechers = Torrent.Peers.Leechs;
            var peerList = Torrent.GetPeers();
            foreach (var peer in peerList)
            {
                if (!PeerList.Contains(peer))
                    PeerList.Add(peer);
            }
            for (int i = 0; i < PeerList.Count; i++)
            {
                if (!peerList.Contains(PeerList[i]))
                    PeerList.RemoveAt(i--);
            }
        }

        public void ChangePicker(PiecePicker piecePicker)
        {
            Torrent.ChangePicker(piecePicker);
            PiecePicker = piecePicker;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private PeriodicFile[] files;
        public PeriodicFile[] Files
        {
            get
            {
                return files;
            }
        }

        public ObservableCollection<PeerId> PeerList { get; set; }

        private TorrentState state;
        public TorrentState State 
        {
            get
            {
                return state;
            }
            private set
            {
                var fire = state != value;
                state = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("State"));
            }
        }

        private TimeSpan elapsedTime;
        public TimeSpan ElapsedTime
        {
            get
            {
                return elapsedTime;
            }
            private set
            {
                var fire = elapsedTime != value;
                elapsedTime = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ElapsedTime"));
            }
        }

        private double progress;
        public double Progress
        {
            get
            {
                return progress;
            }
            private set
            {
                var fire = progress != value;
                progress = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Progress"));
            }
        }

        private int downloadSpeed;
        public int DownloadSpeed
        {
            get
            {
                return downloadSpeed;
            }
            private set
            {
                var fire = downloadSpeed != value;
                downloadSpeed = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DownloadSpeed"));
            }
        }

        private int uploadSpeed;
        public int UploadSpeed
        {
            get
            {
                return uploadSpeed;
            }
            private set
            {
                var fire = uploadSpeed != value;
                uploadSpeed = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("UploadSpeed"));
            }
        }

        private TimeSpan estimatedTime;
        public TimeSpan EstimatedTime
        {
            get
            {
                return estimatedTime;
            }
            private set
            {
                var fire = estimatedTime != value;
                estimatedTime = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("EstimatedTime"));
            }
        }

        private long totalDownloaded;
        public long TotalDownloaded
        {
            get
            {
                return totalDownloaded;
            }
            private set
            {
                var fire = totalDownloaded != value;
                totalDownloaded = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalDownloaded"));
            }
        }

        private long totalUploaded;
        public long TotalUploaded
        {
            get
            {
                return totalUploaded;
            }
            private set
            {
                var fire = totalUploaded != value;
                totalUploaded = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalUploaded"));
            }
        }

        private double ratio;
        public double Ratio
        {
            get
            {
                return ratio;
            }
            private set
            {
                var fire = ratio != value;
                ratio = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Ratio"));
            }
        }

        private bool complete;
        public bool Complete
        {
            get
            {
                return complete;
            }
            private set
            {
                var fire = complete != value;
                complete = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Complete"));
            }
        }

        private long size;
        public long Size
        {
            get
            {
                return size;
            }
            private set
            {
                var fire = size != value;
                size = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Size"));
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            private set
            {
                var fire = name != value;
                name = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public int peers;
        public int Peers
        {
            get
            {
                return peers;
            }
            private set
            {
                var fire = peers != value;
                peers = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Peers"));
            }
        }

        public int seeders;
        public int Seeders
        {
            get
            {
                return seeders;
            }
            private set
            {
                var fire = seeders != value;
                seeders = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Seeders"));
            }
        }

        public int leechers;
        public int Leechers
        {
            get
            {
                return leechers;
            }
            private set
            {
                var fire = leechers != value;
                leechers = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Leechers"));
            }
        }
    }
}
