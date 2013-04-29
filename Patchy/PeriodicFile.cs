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
            if (Name != null)
                Name = System.IO.Path.GetFileName(File.FullPath);
            Length = File.Length;
            Progress = ((double)File.BytesDownloaded / (double)File.Length) * 100;
            Priority = File.Priority;
            Updating = false;
        }

        protected internal virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private long _Length;
        public long Length
        {
            get
            {
                return _Length;
            }
            private set
            {
                _Length = value;
                OnPropertyChanged("Length");
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

        private Priority _Priority;
        public Priority Priority
        {
            get
            {
                return _Priority;
            }
            set
            {
                _Priority = value;
                OnPropertyChanged("Priority");
                if (!Updating)
                    File.Priority = value;
            }
        }

        public string Path
        {
            get { return File.Path; }
        }

        public string FolderPath
        {
            get { return "/" + System.IO.Path.GetDirectoryName(File.Path); }
        }
    }
}
