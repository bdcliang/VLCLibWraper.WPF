using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace LibVlcWraper.WPF
{
    public unsafe class VlcPlayerCore:IDisposable
    {
        private IntPtr m_hMediaLib = IntPtr.Zero;
        protected IntPtr m_hMediaPlayer;
        private IntPtr m_hEventManager;
        private IntPtr _bufferPtr;
        private Bitmap tmpCopyBitmap;//视频帧临时图像
        private Mutex mutex = new Mutex();
        private int frameWidth = 640;//视频帧宽度
        private int frameHeight = 480;//视频帧高度
        private int framePixelBits = 4;//视频帧的像素深度
        private byte* out_buffer;//视频解析缓存



        public delegate void FrameHandler(Bitmap bit);
        /// <summary>
        /// 视频帧接收事件
        /// </summary>
        public event FrameHandler OnFrameReceived;


        private VlcEventHandlerDelegate dMediaPlaying;
        private VlcEventHandlerDelegate dMediaPaused;
        private VlcEventHandlerDelegate dMediaPos;
        private VlcEventHandlerDelegate dMediaStoped;
        private VlcEventHandlerDelegate dMediaEndReached;
        private LockEventHandler dlockEventHandler;
        private UnlockEventHandler dunlockEventHandler;
        private DisplayEventHandler ddisplayEventHandler;

        private bool isoncallback = false;
        /// <summary>
        /// 是否开启视频帧解析
        /// </summary>
        /// <param name="isoncallback"></param>
        public VlcPlayerCore(bool isoncallback=false)
        {
            this.isoncallback = isoncallback;
            string[] args = new string[]
             {
                "-I",
                "dumy",
                "--ignore-config",
                "--no-osd",
                "--disable-screensaver",
                "--ffmpeg-hw",
                "--plugin-path=./plugins"
             };

            dMediaPlaying = MediaPlaying;
            dMediaPaused = MediaPaused;
            dMediaPos=MediaPos;
            dMediaStoped=MediaStoped;
            dMediaEndReached=MediaEndReached;
            dlockEventHandler = mLockEventHandler;
            dunlockEventHandler = mUnlockEventHandler;
            ddisplayEventHandler = mDisplayEventHandler;

            m_hMediaLib = LibVlcMethods.libvlc_new(args.Length, args);
            m_hMediaPlayer = LibVlcMethods.libvlc_media_player_new(m_hMediaLib);
            m_hEventManager = LibVlcMethods.libvlc_media_player_event_manager(m_hMediaPlayer);

            EventManager.Attach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPlaying, dMediaPlaying);
            EventManager.Attach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPaused, dMediaPaused);
            EventManager.Attach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPositionChanged, dMediaPos);
            EventManager.Attach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerStopped, dMediaStoped);
            EventManager.Attach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerEndReached, dMediaEndReached);

            if (isoncallback)
            {
                //LibVlcMethods.libvlc_video_set_format(m_hMediaPlayer, new char[] { 'R', 'V', '3', '2' }, frameWidth, height, frameWidth * framePixelBits);
                LibVlcMethods.libvlc_video_set_callbacks(m_hMediaPlayer, dlockEventHandler, dunlockEventHandler, ddisplayEventHandler, IntPtr.Zero);
            }
        }

        
        void* mLockEventHandler(void* opaque, void** plane)
         {
            mutex.WaitOne();
            _bufferPtr = Marshal.AllocHGlobal(frameWidth * frameHeight * framePixelBits);
            out_buffer = (byte*)_bufferPtr.ToPointer();
            *plane = out_buffer;
            return (void*)0;
        }
        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern int memcpy(byte* dst,byte* src,int count);

        
        void mUnlockEventHandler(void* opaque, void* picture, void** plane)
        {
           
        }
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
        void mDisplayEventHandler(void* opaque, void* picture)
        {
            if (_bufferPtr == IntPtr.Zero) return;
            PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
            if (framePixelBits == 3) pixelFormat = PixelFormat.Format24bppRgb;
            if (framePixelBits == 4) pixelFormat = PixelFormat.Format32bppArgb;
            tmpCopyBitmap = new Bitmap(frameWidth, frameHeight, pixelFormat);
            BitmapData bitmapData = tmpCopyBitmap.LockBits(new Rectangle(0, 0, frameWidth, frameHeight), System.Drawing.Imaging.ImageLockMode.ReadWrite, pixelFormat);
            memcpy((byte*)bitmapData.Scan0, (byte*)_bufferPtr.ToPointer(), frameWidth * frameHeight * framePixelBits);
            tmpCopyBitmap.UnlockBits(bitmapData);
            OnFrameReceived?.Invoke((Bitmap)tmpCopyBitmap.Clone());
            Marshal.FreeHGlobal(_bufferPtr);
            _bufferPtr = IntPtr.Zero;
            tmpCopyBitmap.Dispose();
            GC.Collect();
            mutex.ReleaseMutex();
        }
        /// <summary>
        /// 支持rtsp流和本地视频播放，自动识别路径
        /// </summary>
        /// <param name="filePath"></param>
        public virtual void Open(string filePath)
        {
            ExecuteInNewThread(obj =>
            {
                if(LibVlcMethods.libvlc_media_player_is_playing(m_hMediaPlayer))
                {
                    Stop();
                }
                IntPtr libvlc_media;
                if (!File.Exists(filePath))
                    libvlc_media = LibVlcMethods.libvlc_media_new_location(m_hMediaLib, Encoding.UTF8.GetBytes(filePath));
                else
                    libvlc_media = LibVlcMethods.libvlc_media_new_path(m_hMediaLib, Encoding.UTF8.GetBytes(filePath));  //创建 libvlc_media_player 播放核心
                if (libvlc_media != IntPtr.Zero)
                {
                    LibVlcMethods.libvlc_media_parse(libvlc_media);
                    LibVlcMethods.libvlc_media_player_set_media(m_hMediaPlayer, libvlc_media);  //将视频绑定到播放器去
                    LibVlcMethods.libvlc_video_get_size(m_hMediaPlayer, 0, out frameWidth, out frameHeight);
                    LibVlcMethods.libvlc_video_set_format(m_hMediaPlayer, new char[] { 'R', 'V', '3', '2' }, frameWidth, frameHeight, frameWidth * framePixelBits);
                    LibVlcMethods.libvlc_media_release(libvlc_media);
                    LibVlcMethods.libvlc_media_player_play(m_hMediaPlayer);  //播放
                }
            });
        }
        /// <summary>
        /// 播放视频
        /// </summary>
        public virtual void Play()
        {
            ExecuteInNewThread((obj) => {
                //LibVlcMethods.libvlc_media_player_play(m_hMediaPlayer);
                LibVlcMethods.libvlc_media_player_set_pause(m_hMediaPlayer, 0);
            });
        }
        /// <summary>
        /// 暂停视频
        /// </summary>
        public virtual void Pause()
        {
            ExecuteInNewThread((obj) => {
                //LibVlcMethods.libvlc_media_player_pause(m_hMediaPlayer);
                LibVlcMethods.libvlc_media_player_set_pause(m_hMediaPlayer, 1);
            });
        }
        /// <summary>
        /// 停止视频
        /// </summary>
        public virtual void Stop()
        {
            ExecuteInNewThread((obj) =>
            {
                if (LibVlcMethods.libvlc_media_player_is_playing(m_hMediaPlayer))
                {
                    LibVlcMethods.libvlc_media_player_stop(m_hMediaPlayer);
                }
            });
        }
        private void ExecuteInNewThread(Action<object> action,object stateObject=null)
        {
            new Thread(() => {action?.Invoke(stateObject);}).Start();
        }
        /// <summary>
        /// 设置视频视窗
        /// </summary>
        /// <param name="hwnd"></param>
        public virtual void SetHwnd(IntPtr hwnd)
        {
            this.isoncallback = false;
            if (m_hMediaLib != IntPtr.Zero && hwnd !=IntPtr.Zero)
            {
                LibVlcMethods.libvlc_media_player_set_hwnd(m_hMediaPlayer,hwnd);  //设置播放容器
            }
        }
        /// <summary>
        /// 设置视频视窗
        /// </summary>
        /// <param name="hwnd"></param>
        public virtual void SetHwnd(Control hwnd)
        {
            UISync.Init(hwnd);
            SetHwnd(hwnd.Handle);
        }
        /// <summary>
        /// 主线程更新UI
        /// </summary>
        /// <param name="control"></param>
        /// <param name="action"></param>
        public void RunOnUIThread(Control control,MethodInvoker action)
        {
            UISync.Init(control);
            UISync.Execute(() => { action?.Invoke(); });
        }
        /// <summary>
        /// 设置视频分辨率
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public virtual void SetAspectRadio(int width,int height)
        {
            var aspect = string.Format("{0}:{1}",width,height);
            LibVlcMethods.libvlc_video_set_aspect_ratio(m_hMediaPlayer,Encoding.UTF8.GetBytes(aspect));
        }


        #region Event Function
        /// <summary>
        /// 视频播放到达结尾
        /// </summary>
        public event EventHandler MediaEndReachedEvent;
        /// <summary>
        /// 视频播放事件
        /// </summary>
        public event EventHandler MediaPlayEvent;
        /// <summary>
        /// 视频暂停事件
        /// </summary>
        public event EventHandler MediaPausedEvent;
        /// <summary>
        /// 视频手动停止事件
        /// </summary>
        public event EventHandler MediaStopedEvent;
        public delegate void MediaPosHandler(long percent);
        /// <summary>
        /// 视频当前播放比例
        /// </summary>
        public event MediaPosHandler MediaPosEvent;
        private void MediaEndReached(ref libvlc_event_t libvlc_event, IntPtr userData)
        {
            MediaEndReachedEvent?.Invoke(this, new EventArgs());
        }
        private void MediaPlaying(ref libvlc_event_t libvlc_event, IntPtr userData)
        {
            MediaPlayEvent?.Invoke(this, new EventArgs());
        }
        private void MediaPaused(ref libvlc_event_t libvlc_event, IntPtr userData)
        {
            MediaPausedEvent?.Invoke(this, new EventArgs());
        }
        private void MediaStoped(ref libvlc_event_t libvlc_event, IntPtr userData)
        {
            MediaStopedEvent?.Invoke(this, new EventArgs());
        }
        private void MediaPos(ref libvlc_event_t libvlc_event, IntPtr userData)
        {
            MediaPosEvent?.Invoke(libvlc_event.MediaDescriptor.media_duration_changed.new_duration);
        }
        #endregion


        private bool disposed = false;
        protected void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if(disposing)
            {

            }
            Stop();
            EventManager.Dettach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPlaying, dMediaPlaying);
            EventManager.Dettach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPaused, dMediaPaused);
            EventManager.Dettach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerPositionChanged, dMediaPos);
            EventManager.Dettach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerStopped, dMediaStoped);
            EventManager.Dettach(m_hEventManager, libvlc_event_e.libvlc_MediaPlayerEndReached, dMediaEndReached);
            LibVlcMethods.libvlc_media_player_release(m_hMediaPlayer);
            LibVlcMethods.libvlc_release(m_hMediaLib);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~VlcPlayerCore()
        {
            Dispose(false);
        }
    }
    class UISync
    {
        private static ISynchronizeInvoke Sync;

        public static void Init(ISynchronizeInvoke sync)
        {
            Sync = sync;
        }

        public static void Execute(MethodInvoker action)
        {
            if (Sync != null)
                Sync.BeginInvoke(action, null);
            else
                action();
        }
    }
}
