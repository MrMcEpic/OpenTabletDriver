# Windows Ink Absolute Mode — design

## Problem

On Windows, OTD's only pointer implementation (`WindowsVirtualMouse`) emits plain
`SendInput` mouse events. `MouseButton.Left` is generated for both `PenAction.Tip`
and `PenAction.Eraser`, so Windows Ink apps (OneNote, Illustrator, Fresco, etc.)
cannot distinguish tip from eraser during a stroke. They detect eraser state
during hover (by reading HID digitizer reports in parallel), but interpret the
contact frame as a pen-tip press and draw instead of erasing.

`IEraserHandler` is implemented on Linux (`EvdevVirtualTablet`) and macOS
(`MacOSVirtualMouse`) but **has no Windows implementation**, so the
output-mode-level eraser path in `AbsoluteOutputMode.cs:144` is a no-op on
Windows.

## Goal

Add a new Windows-only absolute output mode that emits events through
`InjectSyntheticPointerInput` with `POINTER_INPUT_TYPE.PT_PEN`, so the OS (and
Ink-aware apps) see a true pen pointer with `PEN_FLAG_INVERTED` /
`PEN_FLAG_ERASER` when the eraser end is in use.

Existing `AbsoluteMode` (mouse-based) stays the default. The new mode is
additive and user-opt-in.

## Non-goals

- Replacing or modifying `WindowsVirtualMouse` / `AbsoluteMode`.
- Relative / Artist mode support.
- Touch or multi-pointer injection.
- Backporting to Windows builds older than 10 1809 (the `CreateSyntheticPointerDevice`
  API ships in 1809, October 2018). Older systems will get a clear error and
  can continue to use regular Absolute Mode.

## Architecture

Three additions, all Windows-only:

### 1. Native interop — `OpenTabletDriver.Native/Windows/Input/Pointer/`

P/Invoke layer for the synthetic pointer API. Files:

- `POINTER_INPUT_TYPE.cs` — enum (`PT_POINTER=1`, `PT_TOUCH=2`, `PT_PEN=3`, `PT_MOUSE=4`, `PT_TOUCHPAD=5`)
- `POINTER_FLAGS.cs` — `[Flags]` enum (NONE, NEW, INRANGE, INCONTACT, FIRSTBUTTON, PRIMARY, DOWN, UPDATE, UP, …)
- `PEN_FLAGS.cs` — `[Flags]` enum (NONE=0, BARREL=1, INVERTED=2, ERASER=4)
- `PEN_MASK.cs` — `[Flags]` enum (NONE, PRESSURE, ROTATION, TILT_X, TILT_Y)
- `POINTER_FEEDBACK_MODE.cs` — enum (DEFAULT=1, INDIRECT=2, NONE=3)
- `POINTER_INFO.cs` — struct matching the Win32 layout
- `POINTER_PEN_INFO.cs` — struct (contains `POINTER_INFO` + pen-specific fields)
- `POINTER_TYPE_INFO.cs` — union (`type` discriminator + inline pen or touch info)
- `Pointer.cs` — `[DllImport("user32.dll")]` declarations for:
  - `CreateSyntheticPointerDevice(POINTER_INPUT_TYPE, uint maxCount, POINTER_FEEDBACK_MODE) → IntPtr`
  - `InjectSyntheticPointerInput(IntPtr device, POINTER_TYPE_INFO[] info, uint count) → bool`
  - `DestroySyntheticPointerDevice(IntPtr device)`

All structs use `[StructLayout(LayoutKind.Sequential)]` with explicit sizes that
match the Win32 SDK headers (critical — a single-field misalignment corrupts
every injected frame).

### 2. `WindowsInkAbsolutePointer` — `OpenTabletDriver.Desktop/Interop/Input/Absolute/`

Implements:

| Interface | Role |
| --- | --- |
| `IAbsolutePointer` | `SetPosition(Vector2)` — stores pending pixel coordinate |
| `IPressureHandler` | `SetPressure(float)` — stores 0.0–1.0, mapped to UINT 0–1024 in frame assembly |
| `ITiltHandler` | `SetTilt(Vector2)` — stores degrees, cast to INT32 |
| `IEraserHandler` | `SetEraser(bool)` — stores inverted state |
| `IMouseButtonHandler` | `MouseDown(Left)` → `contact=true`; `MouseUp(Left)` → `contact=false`. Other buttons are no-ops — barrel buttons are handled by keyboard bindings, not by this pointer. |
| `ISynchronousPointer` | `Flush()` assembles one frame and calls `InjectSyntheticPointerInput`. `Reset()` emits a final out-of-range frame and nulls pending state. |

Internal state (reset after each Flush):
- `_position: Vector2?`
- `_pressure: float?`
- `_tilt: Vector2?`
- `_isEraser: bool`
- `_contact: bool` (sticky across frames — only changes on MouseDown/Up)
- `_contactChanged: bool` (per-frame — differentiates DOWN/UPDATE/UP)
- `_inRange: bool` (sticky — cleared by `Reset()`)
- `_device: IntPtr` (lazy-initialized on first Flush)

Flag derivation in Flush:
```
POINTER_FLAG_INRANGE if _inRange
  | POINTER_FLAG_INCONTACT     if _contact
  | POINTER_FLAG_FIRSTBUTTON   if _contact
  | POINTER_FLAG_PRIMARY       always (single pointer)
  | POINTER_FLAG_DOWN          if _contact && _contactChanged
  | POINTER_FLAG_UP            if !_contact && _contactChanged
  | POINTER_FLAG_UPDATE        if _contact && !_contactChanged
PEN_FLAG_INVERTED              if _isEraser
  | PEN_FLAG_ERASER            if _isEraser && _contact
```

`pointerId = 1` (fixed; single pointer).
`frameId` increments per Flush.
`penMask = PRESSURE | TILT_X | TILT_Y` regardless of whether values were set
this frame — pen mask tells the OS which fields are populated, and we always
populate these (with 0 if unset).

Errors from `InjectSyntheticPointerInput` are logged once and then suppressed
(prevents log spam at report-rate).

### 3. `WindowsInkAbsoluteMode` — `OpenTabletDriver.Desktop/Output/`

```csharp
[PluginName("Windows Ink Absolute Mode")]
public sealed class WindowsInkAbsoluteMode : AbsoluteOutputMode
{
    public override IAbsolutePointer Pointer { get; set; } = new WindowsInkAbsolutePointer();
}
```

No `[Resolved]` — the pointer is directly instantiated because it's
platform-specific. On non-Windows, the plugin registry will still load the
class, but the first Flush will P/Invoke fail — the user sees a log error and
should pick a different output mode.

(Alternative of guarding at class-load time via a platform attribute was
rejected; `LinuxArtistMode` uses the "fail at use" pattern today.)

## Data flow

```
TabletReport (ITabletReport, ITiltReport, IEraserReport)
        │
        ▼
AbsoluteOutputMode.OnOutput
        │
        ├─ IEraserReport   → Pointer is IEraserHandler   → SetEraser(bool)    ╮
        ├─ ITiltReport     → Pointer is ITiltHandler     → SetTilt(Vector2)   │
        ├─ ITabletReport   → Pointer is IPressureHandler → SetPressure(float) │  pending state
        ├─ IAbsolutePosReport → Pointer.SetPosition(Vector2)                  │
        │  (binding handler, separately)                                      │
        └─ AdaptiveBinding(Tip|Eraser).Press → MouseButtonHandler.MouseDown   ╯
        │
        ▼
ISynchronousPointer.Flush
        │
        ▼
InjectSyntheticPointerInput → OS Pointer Input Stack → Windows Ink → app
```

`Reset()` is called on `OutOfRangeReport` by `AbsoluteOutputMode.OnOutput`
(line 158). We use that to:
1. Emit one final frame with `INRANGE` cleared (so the app sees "pen left").
2. Clear `_inRange`, `_contact`, pending fields.

## Error handling

- `CreateSyntheticPointerDevice` fails on Windows < 10 1809 or in sandboxed
  contexts: log error once with a readable message ("Windows Ink mode requires
  Windows 10 version 1809 or later"), mark the pointer as dead, early-return
  from all subsequent `Flush` calls.
- `InjectSyntheticPointerInput` returns false on malformed input: log the first
  failure with the current frame's POINTER_INFO serialized for debugging.
- Finalizer best-effort calls `DestroySyntheticPointerDevice`; ignored if
  handle is zero.

## Testing

- Manual: target reproduction is OneNote (user's bug). Pen should draw with
  tip, erase with eraser end. Verify in at least one more Ink app (Whiteboard
  or Edge PDF annotator).
- Manual: confirm old `AbsoluteMode` still works unchanged when selected.
- Diagnostic logging: add a one-time log at first successful Flush —
  "Windows Ink pen pointer active (device handle 0x…)" — so users can confirm
  the mode engaged.
- Automated tests are limited (the API is a syscall, no easy stub), but we'll
  unit-test the flag-derivation logic in isolation by extracting it to a pure
  static method that takes state and returns `(POINTER_FLAGS, PEN_FLAGS)`.

## Rollout

Single PR / single commit set:
1. Native interop structs + P/Invoke
2. `WindowsInkAbsolutePointer`
3. `WindowsInkAbsoluteMode`
4. Flag-derivation unit test

No migration needed — user opts in by selecting the new mode.
