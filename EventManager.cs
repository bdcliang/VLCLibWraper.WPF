using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibVlcWraper.WPF
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void VlcEventHandlerDelegate(ref libvlc_event_t libvlc_event, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* LockEventHandler(void* opaque, void** plane);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void UnlockEventHandler(void* opaque, void* picture, void** plane);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void DisplayEventHandler(void* opaque, void* picture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* CallbackEventHandler(void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int VideoFormatCallback(void** opaque, char* chroma, int* width, int* height, int* pitches, int* lines);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void CleanupCallback(void* opaque);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void PlayCallbackEventHandler(void* data, void* samples, uint count, long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void VolumeCallbackEventHandler(void* data, float volume, bool mute);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int SetupCallbackEventHandler(void** data, char* format, int* rate, int* channels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void AudioCallbackEventHandler(void* data, long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void AudioDrainCallbackEventHandler(void* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int ImemGet(void* data, char* cookie, long* dts, long* pts, int* flags, uint* dataSize, void** ppData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void ImemRelease(void* data, char* cookie, uint dataSize, void* pData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void LogCallback(void* data, libvlc_log_level level, char* fmt, char* args);
    public class EventManager
    {
        public static  void Attach(IntPtr event_manager,libvlc_event_e eType, VlcEventHandlerDelegate handler)
        {
            if (LibVlcMethods.libvlc_event_attach(event_manager, eType, Marshal.GetFunctionPointerForDelegate(handler), IntPtr.Zero) != 0)
            {
                throw new OutOfMemoryException("Failed to subscribe to event notification");
            }
        }

        public static void Dettach(IntPtr event_manager, libvlc_event_e eType, VlcEventHandlerDelegate handler)
        {
            LibVlcMethods.libvlc_event_detach(event_manager, eType, Marshal.GetFunctionPointerForDelegate(handler), IntPtr.Zero);
        }
    }
}
