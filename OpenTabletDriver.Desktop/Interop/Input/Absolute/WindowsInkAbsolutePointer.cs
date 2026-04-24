using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace OpenTabletDriver.Desktop.Interop.Input.Absolute
{
    [PluginIgnore]
    public class WindowsInkAbsolutePointer : WindowsInkPointerBase, IAbsolutePointer
    {
        public void SetPosition(Vector2 pos)
        {
            _position = pos;
            _inRange = true;
        }
    }
}
