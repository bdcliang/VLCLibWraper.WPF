using Microsoft.Win32;
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

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            videoplayer.MediaPlayer.Open(@"C:\Users\Administrator.Delphi-PC\Videos\suzhou.mp4");
        }

        private void Opencmd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog().Value)
            {
                videoplayer.MediaPlayer.Open(dlg.FileName);
            }
        }

        private void Pausecmd_Click(object sender, RoutedEventArgs e)
        {
            videoplayer.MediaPlayer.Pause();
        }

        private void Continuecmd_Click(object sender, RoutedEventArgs e)
        {
            videoplayer.MediaPlayer.Play();
        }

        private void Stopcmd_Click(object sender, RoutedEventArgs e)
        {
            videoplayer.MediaPlayer.Stop();
        }
    }
}
