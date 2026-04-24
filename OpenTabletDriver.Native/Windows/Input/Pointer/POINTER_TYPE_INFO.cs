using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenTabletDriver.Native.Windows.Input.Pointer
{
    // Win32 POINTER_TYPE_INFO is a discriminated union with a trailing union of
    // POINTER_TOUCH_INFO | POINTER_PEN_INFO. We only ever emit pens, so we model
    // the struct with explicit layout: the type discriminator at 0, penInfo at 8
    // (after 4-byte padding for 8-byte alignment of the union's ulong fields),
    // and a fixed Size that matches the larger (touch) variant the OS expects.
    [StructLayout(LayoutKind.Explicit, Size = 152)]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct POINTER_TYPE_INFO
    {
        [FieldOffset(0)]
        public POINTER_INPUT_TYPE type;

        [FieldOffset(8)]
        public POINTER_PEN_INFO penInfo;
    }
}
