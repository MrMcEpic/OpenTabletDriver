using System.Diagnostics.CodeAnalysis;
using OpenTabletDriver.Desktop.Interop.Input.Absolute;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace OpenTabletDriver.Desktop.Output
{
    // Injects pen events via Windows' synthetic pointer API so Ink-aware apps
    // (OneNote, Whiteboard, Edge, Fresco, etc.) can distinguish tip from eraser
    // mid-stroke. Requires Windows 10 1809+; on older versions the device fails
    // to initialize and an error is logged. The default Absolute Mode stays
    // available for users who prefer mouse-style input.
    [PluginName("Windows Ink Absolute Mode")]
    [SupportedPlatform(PluginPlatform.Windows)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public sealed class WindowsInkAbsoluteMode : AbsoluteOutputMode
    {
        public override IAbsolutePointer Pointer { get; set; } = new WindowsInkAbsolutePointer();
    }
}
