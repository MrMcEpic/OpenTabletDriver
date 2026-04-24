using System.Diagnostics.CodeAnalysis;
using OpenTabletDriver.Desktop.Interop.Input.Relative;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace OpenTabletDriver.Desktop.Output
{
    // Relative-position equivalent of Windows Ink Absolute Mode — emits pen
    // events via InjectSyntheticPointerInput, so Ink-aware apps still see a
    // true pen pointer (with pressure, tilt, inverted/eraser state). The pen's
    // absolute position tracks the cursor rather than the tablet area.
    [PluginName("Windows Ink Relative Mode")]
    [SupportedPlatform(PluginPlatform.Windows)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public sealed class WindowsInkRelativeMode : RelativeOutputMode
    {
        public override IRelativePointer Pointer { get; set; } = new WindowsInkRelativePointer();
    }
}
