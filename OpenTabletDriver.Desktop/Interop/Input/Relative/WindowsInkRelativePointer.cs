using System.Numerics;
using OpenTabletDriver.Native.Windows;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Platform.Pointer;
using NativeWindows = OpenTabletDriver.Native.Windows.Windows;

namespace OpenTabletDriver.Desktop.Interop.Input.Relative
{
    [PluginIgnore]
    public class WindowsInkRelativePointer : WindowsInkPointerBase, IRelativePointer
    {
        private Vector2 _subpixelError;
        private bool _positionSeeded;

        public void SetPosition(Vector2 delta)
        {
            if (!_positionSeeded)
            {
                if (NativeWindows.GetCursorPos(out var cursor))
                    _position = new Vector2(cursor.X, cursor.Y);
                _positionSeeded = true;
            }

            // Accumulate sub-pixel deltas so slow pen movement still registers.
            var moved = delta + _subpixelError;
            var whole = new Vector2((int)moved.X, (int)moved.Y);
            _subpixelError = moved - whole;
            _position += whole;
            _inRange = true;
        }

        public override void Reset()
        {
            base.Reset();
            _positionSeeded = false;
            _subpixelError = Vector2.Zero;
        }
    }
}
