using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LibVlcWraper.WPF
{
    public class VlcPlayer: System.Windows.Controls.Image
    {
        private VlcPlayerCore player = null;
        public VlcPlayer()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                player = new VlcPlayerCore(true);
                player.OnFrameReceived += Player_OnFrameReceived;
            }

        }

        private void Player_OnFrameReceived(Bitmap bit)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Source = BitmapToBitmapSource(bit);
            }));
        }

        public VlcPlayerCore MediaPlayer { get { return player; } }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource source;
            try
            {
                source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return source;
        }
    }
}
