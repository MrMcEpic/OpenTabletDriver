using System.Diagnostics.CodeAnalysis;

namespace OpenTabletDriver.Native.Windows.Input.Pointer
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public enum POINTER_FEEDBACK_MODE
    {
        DEFAULT = 1,
        INDIRECT = 2,
        NONE = 3
    }
}
