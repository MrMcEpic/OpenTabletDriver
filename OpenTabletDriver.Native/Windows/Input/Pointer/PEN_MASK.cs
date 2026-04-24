using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenTabletDriver.Native.Windows.Input.Pointer
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum PEN_MASK : uint
    {
        NONE = 0x00000000,
        PRESSURE = 0x00000001,
        ROTATION = 0x00000002,
        TILT_X = 0x00000004,
        TILT_Y = 0x00000008
    }
}
