using System;
using System.Diagnostics.CodeAnalysis;

namespace OpenTabletDriver.Native.Windows.Input.Pointer
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum PEN_FLAGS : uint
    {
        NONE = 0x00000000,
        BARREL = 0x00000001,
        INVERTED = 0x00000002,
        ERASER = 0x00000004
    }
}
