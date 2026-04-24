using OpenTabletDriver.Desktop.Interop.Input.Absolute;
using OpenTabletDriver.Native.Windows.Input.Pointer;
using Xunit;

namespace OpenTabletDriver.Tests
{
    public class WindowsInkAbsolutePointerTest
    {
        // First-ever frame while hovering: NEW + INRANGE + UPDATE, and PRIMARY always.
        [Fact]
        public void DeriveFlags_FirstHoverFrame_EmitsNewAndUpdate()
        {
            var (pointer, pen) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: true, inRange: true, prevInRange: false,
                contact: false, prevContact: false, isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.NEW));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INRANGE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.PRIMARY));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.Equal(PEN_FLAGS.NONE, pen);
        }

        // Tip touches down: DOWN (not UPDATE), INCONTACT, FIRSTBUTTON.
        [Fact]
        public void DeriveFlags_ContactBegin_EmitsDownNotUpdate()
        {
            var (pointer, _) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: false, isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
        }

        // Drag frame: UPDATE while contact stays true.
        [Fact]
        public void DeriveFlags_ContactContinuing_EmitsUpdate()
        {
            var (pointer, _) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: true, isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
        }

        // Tip lifts: UP (not UPDATE), no INCONTACT/FIRSTBUTTON anymore.
        [Fact]
        public void DeriveFlags_ContactEnd_EmitsUpNotUpdate()
        {
            var (pointer, _) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: true, isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
        }

        // Eraser hovering: INVERTED but not ERASER (ERASER requires contact).
        [Fact]
        public void DeriveFlags_EraserHover_EmitsInvertedOnly()
        {
            var (_, pen) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: false, isEraser: true);

            Assert.True(pen.HasFlag(PEN_FLAGS.INVERTED));
            Assert.False(pen.HasFlag(PEN_FLAGS.ERASER));
        }

        // Eraser pressing: INVERTED | ERASER.
        [Fact]
        public void DeriveFlags_EraserContact_EmitsInvertedAndEraser()
        {
            var (_, pen) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: true, isEraser: true);

            Assert.True(pen.HasFlag(PEN_FLAGS.INVERTED));
            Assert.True(pen.HasFlag(PEN_FLAGS.ERASER));
        }

        // Pen hover (not eraser): no pen flags.
        [Fact]
        public void DeriveFlags_PenHover_NoPenFlags()
        {
            var (_, pen) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: false, isEraser: false);

            Assert.Equal(PEN_FLAGS.NONE, pen);
        }

        // Leaving range while not in contact: no INRANGE, no UP, no UPDATE.
        [Fact]
        public void DeriveFlags_OutOfRange_NoFlags()
        {
            var (pointer, _) = WindowsInkAbsolutePointer.DeriveFlags(
                isNew: false, inRange: false, prevInRange: true,
                contact: false, prevContact: false, isEraser: false);

            Assert.False(pointer.HasFlag(POINTER_FLAGS.INRANGE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.PRIMARY));
        }
    }
}
