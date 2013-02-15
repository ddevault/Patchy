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
        public DateTime CompletionTime { get; set; }

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
            if (Torrent.Progress == 100 && Progress != 100)
                CompletionTime = DateTime.Now;
            Progress = Torrent.Progress;
            Complete = Torrent.Complete;
            DownloadSpeed = Torrent.Monitor.DownloadSpeed;
            UploadSpeed = Torrent.Monitor.UploadSpeed;
            if (Torrent.StartTime != DateTime.MinValue)
                ElapsedTime = DateTime.Now - Torrent.StartTime;
            else
                ElapsedTime = TimeSpan.MinValue;
            if (Torrent.Complete)
                EstimatedTime = CompletionTime - DateTime.Now;
            else
            {
                if (Torrent.State == TorrentState.Metadata || DownloadSpeed == 0)
                    EstimatedTime = TimeSpan.MaxValue;
                else
                    EstimatedTime = TimeSpan.FromSeconds((Size - (Size * (Progress / 100))) / DownloadSpeed);
            }
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

        protected internal virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
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

        private int _Index;
        public int Index
        {
            get
            {
                return _Index;
            }
            set
            {
                _Index = value;
                OnPropertyChanged("Index");
            }
        }

        private TorrentState _State;
        public TorrentState State 
        {
            get
            {
                return _State;
            }
            private set
            {
                _State = value;
                OnPropertyChanged("State");
            }
        }

        private TimeSpan _ElapsedTime;
        public TimeSpan ElapsedTime
        {
            get
            {
                return _ElapsedTime;
            }
            private set
            {
                _ElapsedTime = value;
                OnPropertyChanged("ElapsedTime");
            }
        }

        private double _Progress;
        public double Progress
        {
            get
            {
                return _Progress;
            }
            private set
            {
                _Progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private int _DownloadSpeed;
        public int DownloadSpeed
        {
            get
            {
                return _DownloadSpeed;
            }
            private set
            {
                _DownloadSpeed = value;
                OnPropertyChanged("DownloadSpeed");
            }
        }

        private int _UploadSpeed;
        public int UploadSpeed
        {
            get
            {
                return _UploadSpeed;
            }
            private set
            {
                _UploadSpeed = value;
                OnPropertyChanged("UploadSpeed");
            }
        }

        private TimeSpan _EstimatedTime;
        public TimeSpan EstimatedTime
        {
            get
            {
                return _EstimatedTime;
            }
            private set
            {
                _EstimatedTime = value;
                OnPropertyChanged("EstimatedTime");
            }
        }

        private long _TotalDownloaded;
        public long TotalDownloaded
        {
            get
            {
                return _TotalDownloaded;
            }
            private set
            {
                _TotalDownloaded = value;
                OnPropertyChanged("TotalDownloaded");
            }
        }

        private long _TotalUploaded;
        public long TotalUploaded
        {
            get
            {
                return _TotalUploaded;
            }
            private set
            {
                _TotalUploaded = value;
                OnPropertyChanged("TotalUploaded");
            }
        }

        private double _Ratio;
        public double Ratio
        {
            get
            {
                return _Ratio;
            }
            private set
            {
                _Ratio = value;
                OnPropertyChanged("Ratio");
            }
        }

        private bool _Complete;
        public bool Complete
        {
            get
            {
                return _Complete;
            }
            private set
            {
                _Complete = value;
                OnPropertyChanged("Complete");
            }
        }

        private long _Size;
        public long Size
        {
            get
            {
                return _Size;
            }
            private set
            {
                _Size = value;
                OnPropertyChanged("Size");
            }
        }

        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            private set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }

        public int _Peers;
        public int Peers
        {
            get
            {
                return _Peers;
            }
            private set
            {
                _Peers = value;
                OnPropertyChanged("Peers");
            }
        }

        public int _Seeders;
        public int Seeders
        {
            get
            {
                return _Seeders;
            }
            private set
            {
                _Seeders = value;
                OnPropertyChanged("Seeders");
            }
        }

        public int _Leechers;
        public int Leechers
        {
            get
            {
                return _Leechers;
            }
            private set
            {
                _Leechers = value;
                OnPropertyChanged("Leechers");
            }
        }
    }
}
