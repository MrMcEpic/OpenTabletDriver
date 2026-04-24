using System.Linq;
using OpenTabletDriver.Desktop.Interop.Input;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Tablet;

#nullable enable

namespace OpenTabletDriver.Desktop.Binding
{
    // State binding that toggles or holds the eraser for Windows Ink modes
    // independently of the pen's physical orientation. Useful for pens that
    // have no eraser end (or when the user wants a button-driven eraser).
    [PluginName(PluginName)]
    [SupportedPlatform(PluginPlatform.Windows)]
    public sealed class WindowsInkEraserBinding : IStateBinding
    {
        private const string PluginName = "Windows Ink Eraser";

        public static string[] ValidModes { get; } = { "Toggle", "Hold" };

        [Property(nameof(Mode)), PropertyValidated(nameof(ValidModes))]
        public string? Mode { get; set; } = "Toggle";

        [Resolved] public IManualEraserHandler? Handler { set; get; }

        [OnDependencyLoad]
        public void VerifyInitialization()
        {
            if (Handler == null)
                Log.Write(PluginName,
                    $"{PluginName} requires a Windows Ink output mode (Absolute or Relative) to be active.",
                    LogLevel.Error);
        }

        public void Press(TabletReference tablet, IDeviceReport report)
        {
            if (Handler == null) return;
            switch (Mode)
            {
                case "Toggle": Handler.ToggleManualEraser(); break;
                case "Hold": Handler.SetManualEraser(true); break;
            }
        }

        public void Release(TabletReference tablet, IDeviceReport report)
        {
            if (Handler != null && Mode == "Hold")
                Handler.SetManualEraser(false);
        }

        public override string ToString() => ValidModes.Contains(Mode)
            ? $"{PluginName}: {Mode}"
            : PluginName;
    }
}
