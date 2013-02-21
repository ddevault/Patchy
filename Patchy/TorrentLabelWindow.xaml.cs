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
using System.Windows.Shapes;

namespace Patchy
{
    /// <summary>
    /// Interaction logic for TorrentLabelWindow.xaml
    /// </summary>
    public partial class TorrentLabelWindow : Window
    {
        public TorrentLabel Label
        {
            get
            {
                return new TorrentLabel(labelNameTextBox.Text, colorPicker.SelectedColor.ToString());
            }
        }

        public TorrentLabelWindow()
        {
            InitializeComponent();
        }

        private void addClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void LabelNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            addButton.IsEnabled = !string.IsNullOrEmpty(labelNameTextBox.Text);
        }
    }
}
