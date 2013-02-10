using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public PeriodicTorrent(TorrentWrapper wrapper)
        {
            Torrent = wrapper;
            Update();
            Name = Torrent.Name;
            Size = Torrent.Size;
        }

        internal void Update()
        {
            State = Torrent.State;
            Progress = Torrent.Progress;
            DownloadSpeed = Torrent.Monitor.DownloadSpeed;
            UploadSpeed = Torrent.Monitor.UploadSpeed;
            if (Torrent.State == TorrentState.Metadata)
                EstimatedTime = TimeSpan.MaxValue;
            else
                EstimatedTime = new TimeSpan((long)((DateTime.Now - Torrent.StartTime).Ticks / (Torrent.Progress / 100)));
            TotalDownloaded = Torrent.Monitor.DataBytesDownloaded;
            TotalUploaded = Torrent.Monitor.DataBytesUploaded;
            DownloadToUploadRatio = (double)Torrent.Monitor.DataBytesDownloaded / (double)Torrent.Monitor.DataBytesUploaded;
            if (Torrent.IsMagnet && Torrent.State == TorrentState.Downloading && Torrent.Size == -1)
            {
                Size = Torrent.Torrent.Files.Select(f => f.Length).Aggregate((a, b) => a + b);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private TorrentState state;
        public TorrentState State 
        {
            get
            {
                return state;
            }
            set
            {
                var fire = state != value;
                state = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("State"));
            }
        }

        private double progress;
        public double Progress
        {
            get
            {
                return progress;
            }
            set
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
            set
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
            set
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
            set
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
            set
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
            set
            {
                var fire = totalUploaded != value;
                totalUploaded = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TotalUploaded"));
            }
        }

        private double downloadToUploadRatio;
        public double DownloadToUploadRatio
        {
            get
            {
                return downloadToUploadRatio;
            }
            set
            {
                var fire = downloadToUploadRatio != value;
                downloadToUploadRatio = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DownloadToUploadRatio"));
            }
        }

        private bool complete;
        public bool Complete
        {
            get
            {
                return complete;
            }
            set
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
            set
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
            set
            {
                var fire = name != value;
                name = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Name"));
            }
        }
    }
}
