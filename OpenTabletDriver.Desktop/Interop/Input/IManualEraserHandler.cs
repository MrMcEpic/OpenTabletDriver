namespace OpenTabletDriver.Desktop.Interop.Input
{
    /// <summary>
    /// A handler that lets bindings force the eraser state on or off independently
    /// of the physical pen orientation. Exposed by pointers that can emit an
    /// inverted pen pointer (i.e. <see cref="Absolute.WindowsInkAbsolutePointer"/>
    /// and <see cref="Relative.WindowsInkRelativePointer"/>).
    /// </summary>
    public interface IManualEraserHandler
    {
        /// <summary>Force manual eraser on or off.</summary>
        void SetManualEraser(bool active);

        /// <summary>Flip the current manual eraser state.</summary>
        void ToggleManualEraser();
    }
}
