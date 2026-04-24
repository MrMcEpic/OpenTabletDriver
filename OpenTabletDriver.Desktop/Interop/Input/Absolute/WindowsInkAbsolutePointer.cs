using System;
using System.Numerics;
using System.Runtime.InteropServices;
using OpenTabletDriver.Native.Windows;
using OpenTabletDriver.Native.Windows.Input.Pointer;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Platform.Pointer;

namespace OpenTabletDriver.Desktop.Interop.Input.Absolute
{
    [PluginIgnore]
    public class WindowsInkAbsolutePointer :
        IAbsolutePointer,
        IPressureHandler,
        ITiltHandler,
        IEraserHandler,
        IMouseButtonHandler,
        ISynchronousPointer,
        IDisposable
    {
        private const string LogGroup = "Windows Ink";
        private const uint MaxPressure = 1024;

        private IntPtr _device = IntPtr.Zero;
        private bool _deviceUnavailable;
        private bool _initErrorLogged;
        private bool _injectErrorLogged;
        private uint _frameId;
        private bool _isNew = true;

        private Vector2 _position;
        private float _pressure;
        private Vector2 _tilt;
        private bool _isEraser;

        private bool _contact;
        private bool _prevContact;
        private bool _barrel;
        private bool _prevBarrel;
        private bool _inRange;
        private bool _prevInRange;

        public void SetPosition(Vector2 pos)
        {
            _position = pos;
            _inRange = true;
        }

        public void SetPressure(float percentage) => _pressure = percentage;

        public void SetTilt(Vector2 tilt) => _tilt = tilt;

        public void SetEraser(bool isEraser) => _isEraser = isEraser;

        public void MouseDown(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: _contact = true; break;
                case MouseButton.Right: _barrel = true; break;
            }
        }

        public void MouseUp(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: _contact = false; break;
                case MouseButton.Right: _barrel = false; break;
            }
        }

        public void Reset()
        {
            _contact = false;
            _barrel = false;
            _inRange = false;
            _pressure = 0f;
            _tilt = Vector2.Zero;
        }

        public void Flush()
        {
            if (_deviceUnavailable)
                return;

            if (!EnsureDevice())
                return;

            // Nothing changed and nothing's pending — skip the OS roundtrip
            if (!_inRange && !_prevInRange && !_contact && !_prevContact && !_barrel && !_prevBarrel && !_isNew)
                return;

            var (pointerFlags, penFlags, buttonChange) = DeriveFlags(
                isNew: _isNew,
                inRange: _inRange,
                prevInRange: _prevInRange,
                contact: _contact,
                prevContact: _prevContact,
                barrel: _barrel,
                prevBarrel: _prevBarrel,
                isEraser: _isEraser);

            var pressureScaled = (uint)Math.Clamp(_pressure * MaxPressure, 0, MaxPressure);

            var info = new POINTER_TYPE_INFO
            {
                type = POINTER_INPUT_TYPE.PT_PEN,
                penInfo = new POINTER_PEN_INFO
                {
                    pointerInfo = new POINTER_INFO
                    {
                        pointerType = POINTER_INPUT_TYPE.PT_PEN,
                        pointerId = 1,
                        frameId = _frameId++,
                        pointerFlags = pointerFlags,
                        ptPixelLocation = new POINT((int)_position.X, (int)_position.Y),
                        ButtonChangeType = buttonChange
                    },
                    penFlags = penFlags,
                    penMask = PEN_MASK.PRESSURE | PEN_MASK.TILT_X | PEN_MASK.TILT_Y,
                    pressure = pressureScaled,
                    tiltX = (int)_tilt.X,
                    tiltY = (int)_tilt.Y
                }
            };

            if (!Pointer.InjectSyntheticPointerInput(_device, ref info, 1))
            {
                if (!_injectErrorLogged)
                {
                    var err = Marshal.GetLastWin32Error();
                    Log.Write(LogGroup,
                        $"InjectSyntheticPointerInput failed (Win32 error {err}); further errors will be suppressed.",
                        LogLevel.Error);
                    _injectErrorLogged = true;
                }
            }

            _isNew = false;
            _prevContact = _contact;
            _prevBarrel = _barrel;
            _prevInRange = _inRange;
        }

        /// <summary>
        /// Pure derivation of pointer flags, pen flags, and button change type from pointer state.
        /// Extracted for testing.
        /// </summary>
        public static (POINTER_FLAGS pointerFlags, PEN_FLAGS penFlags, POINTER_BUTTON_CHANGE_TYPE buttonChange) DeriveFlags(
            bool isNew,
            bool inRange,
            bool prevInRange,
            bool contact,
            bool prevContact,
            bool barrel,
            bool prevBarrel,
            bool isEraser)
        {
            var pointerFlags = POINTER_FLAGS.PRIMARY;

            if (isNew)
                pointerFlags |= POINTER_FLAGS.NEW;

            if (inRange)
                pointerFlags |= POINTER_FLAGS.INRANGE;

            if (contact)
                pointerFlags |= POINTER_FLAGS.INCONTACT | POINTER_FLAGS.FIRSTBUTTON;

            if (barrel)
                pointerFlags |= POINTER_FLAGS.SECONDBUTTON;

            // Primary (tip) transitions drive DOWN/UP; other frames use UPDATE while in range.
            // Barrel transitions keep UPDATE and signal via ButtonChangeType.
            if (contact && !prevContact)
                pointerFlags |= POINTER_FLAGS.DOWN;
            else if (!contact && prevContact)
                pointerFlags |= POINTER_FLAGS.UP;
            else if (inRange)
                pointerFlags |= POINTER_FLAGS.UPDATE;

            var penFlags = PEN_FLAGS.NONE;
            if (isEraser)
            {
                penFlags |= PEN_FLAGS.INVERTED;
                if (contact)
                    penFlags |= PEN_FLAGS.ERASER;
            }
            if (barrel)
                penFlags |= PEN_FLAGS.BARREL;

            // Tip change takes priority; only one button change per frame.
            var buttonChange = POINTER_BUTTON_CHANGE_TYPE.NONE;
            if (contact && !prevContact)
                buttonChange = POINTER_BUTTON_CHANGE_TYPE.FIRSTBUTTON_DOWN;
            else if (!contact && prevContact)
                buttonChange = POINTER_BUTTON_CHANGE_TYPE.FIRSTBUTTON_UP;
            else if (barrel && !prevBarrel)
                buttonChange = POINTER_BUTTON_CHANGE_TYPE.SECONDBUTTON_DOWN;
            else if (!barrel && prevBarrel)
                buttonChange = POINTER_BUTTON_CHANGE_TYPE.SECONDBUTTON_UP;

            return (pointerFlags, penFlags, buttonChange);
        }

        private bool EnsureDevice()
        {
            if (_device != IntPtr.Zero)
                return true;

            _device = Pointer.CreateSyntheticPointerDevice(
                POINTER_INPUT_TYPE.PT_PEN,
                maxCount: 1,
                POINTER_FEEDBACK_MODE.DEFAULT);

            if (_device == IntPtr.Zero)
            {
                if (!_initErrorLogged)
                {
                    var err = Marshal.GetLastWin32Error();
                    Log.Write(LogGroup,
                        $"CreateSyntheticPointerDevice failed (Win32 error {err}). Windows Ink Absolute Mode requires Windows 10 1809 or later.",
                        LogLevel.Error);
                    _initErrorLogged = true;
                }
                _deviceUnavailable = true;
                return false;
            }

            Log.Debug(LogGroup, $"Synthetic pen pointer device created (handle 0x{_device.ToInt64():X}).");
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (_device != IntPtr.Zero)
            {
                Pointer.DestroySyntheticPointerDevice(_device);
                _device = IntPtr.Zero;
            }

            _isDisposed = true;
        }

        ~WindowsInkAbsolutePointer() => Dispose(false);
    }
}
