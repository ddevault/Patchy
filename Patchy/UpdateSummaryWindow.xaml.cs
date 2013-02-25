using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for UpdateSummaryWindow.xaml
    /// </summary>
    public partial class UpdateSummaryWindow : Window
    {
        public AutomaticUpdate Update { get; set; }

        public UpdateSummaryWindow(AutomaticUpdate update)
        {
            InitializeComponent();
            DataContext = update;
            Update = update;
        }

        private void applyClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void viewDiffClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Update.DiffUrl);
        }
    }
}
