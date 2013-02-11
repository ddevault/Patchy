using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent.Common;

namespace Patchy
{
    public class PeriodicFile : INotifyPropertyChanged
    {
        public TorrentFile File { get; set; }
        private bool Updating { get; set; }

        public PeriodicFile(TorrentFile file)
        {
            File = file;
            Update();
        }

        internal void Update()
        {
            Updating = true;
            Name = Path.GetFileName(File.FullPath);
            Length = File.Length;
            Progress = ((double)File.BytesDownloaded / (double)File.Length) * 100;
            Priority = File.Priority;
            Updating = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private long length;
        public long Length
        {
            get
            {
                return length;
            }
            private set
            {
                var fire = length != value;
                length = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Length"));
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

        private Priority priority;
        public Priority Priority
        {
            get
            {
                return priority;
            }
            set
            {
                var fire = priority != value;
                priority = value;
                if (fire && PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Priority"));
                if (!Updating)
                    File.Priority = value;
            }
        }
    }
}
