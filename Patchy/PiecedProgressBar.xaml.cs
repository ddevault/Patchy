using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for PiecedProgressBar.xaml
    /// </summary>
    public partial class PiecedProgressBar : UserControl
    {
        private PeriodicTorrent Torrent { get; set; }

        public PiecedProgressBar()
        {
            InitializeComponent();
            DataContextChanged += PiecedProgressBar_DataContextChanged;
        }

        void PiecedProgressBar_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() => this.InvalidateVisual()));
            var torrent = DataContext as PeriodicTorrent;
            if (torrent != null)
            {
                if (Torrent != null)
                    Torrent.PropertyChanged -= torrent_PropertyChanged;
                Torrent = torrent;
                torrent.PropertyChanged += torrent_PropertyChanged;
            }
        }

        void torrent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RecievedPieces")
                Dispatcher.Invoke(new Action(() => this.InvalidateVisual()));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var torrent = DataContext as PeriodicTorrent;
            if (torrent == null)
            {
                drawingContext.DrawRectangle(null, new Pen(Brushes.Black, 1), new Rect(0, 0, this.ActualWidth, this.ActualHeight));
                return;
            }
            var pieces = torrent.RecievedPieces;
            if (pieces == null)
                return;
            double width = ActualWidth / pieces.Length;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i])
                {
                    drawingContext.DrawRectangle(Brushes.LightGreen, null,
                        new Rect(Math.Ceiling(i * width), 0, Math.Ceiling(width), ActualHeight));
                }
                else
                    drawingContext.DrawRectangle(Brushes.White, null,
                        new Rect(Math.Ceiling(i * width), 0, Math.Ceiling(width), ActualHeight));
            }
            drawingContext.DrawRectangle(null, new Pen(Brushes.DarkGray, 1), new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            base.OnRender(drawingContext);
        }
    }
}
