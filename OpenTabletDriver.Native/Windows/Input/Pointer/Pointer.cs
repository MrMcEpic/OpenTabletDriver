using System;
using System.Runtime.InteropServices;

namespace OpenTabletDriver.Native.Windows.Input.Pointer
{
    public static class Pointer
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateSyntheticPointerDevice(
            POINTER_INPUT_TYPE pointerType,
            uint maxCount,
            POINTER_FEEDBACK_MODE mode);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InjectSyntheticPointerInput(
            IntPtr device,
            ref POINTER_TYPE_INFO pointerInfo,
            uint count);

        [DllImport("user32.dll")]
        public static extern void DestroySyntheticPointerDevice(IntPtr device);
    }
}
