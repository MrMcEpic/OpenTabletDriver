using OpenTabletDriver.Desktop.Interop.Input;
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
            var (pointer, pen, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: true, inRange: true, prevInRange: false,
                contact: false, prevContact: false,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.NEW));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INRANGE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.PRIMARY));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.Equal(PEN_FLAGS.NONE, pen);
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.NONE, change);
        }

        // Tip touches down: DOWN (not UPDATE), INCONTACT, FIRSTBUTTON, FIRSTBUTTON_DOWN change.
        [Fact]
        public void DeriveFlags_ContactBegin_EmitsDownNotUpdate()
        {
            var (pointer, _, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: false,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.FIRSTBUTTON_DOWN, change);
        }

        // Drag frame: UPDATE while contact stays true.
        [Fact]
        public void DeriveFlags_ContactContinuing_EmitsUpdate()
        {
            var (pointer, _, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: true,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.NONE, change);
        }

        // Tip lifts: UP (not UPDATE), no INCONTACT/FIRSTBUTTON anymore, FIRSTBUTTON_UP change.
        [Fact]
        public void DeriveFlags_ContactEnd_EmitsUpNotUpdate()
        {
            var (pointer, _, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: true,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.FIRSTBUTTON_UP, change);
        }

        // Eraser hovering: INVERTED but not ERASER (ERASER requires contact).
        [Fact]
        public void DeriveFlags_EraserHover_EmitsInvertedOnly()
        {
            var (_, pen, _) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: false,
                barrel: false, prevBarrel: false,
                isEraser: true);

            Assert.True(pen.HasFlag(PEN_FLAGS.INVERTED));
            Assert.False(pen.HasFlag(PEN_FLAGS.ERASER));
        }

        // Eraser pressing: INVERTED | ERASER.
        [Fact]
        public void DeriveFlags_EraserContact_EmitsInvertedAndEraser()
        {
            var (_, pen, _) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: true,
                barrel: false, prevBarrel: false,
                isEraser: true);

            Assert.True(pen.HasFlag(PEN_FLAGS.INVERTED));
            Assert.True(pen.HasFlag(PEN_FLAGS.ERASER));
        }

        // Pen hover (not eraser): no pen flags.
        [Fact]
        public void DeriveFlags_PenHover_NoPenFlags()
        {
            var (_, pen, _) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: false,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.Equal(PEN_FLAGS.NONE, pen);
        }

        // Leaving range while not in contact: no INRANGE, no UP, no UPDATE.
        [Fact]
        public void DeriveFlags_OutOfRange_NoFlags()
        {
            var (pointer, _, _) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: false, prevInRange: true,
                contact: false, prevContact: false,
                barrel: false, prevBarrel: false,
                isEraser: false);

            Assert.False(pointer.HasFlag(POINTER_FLAGS.INRANGE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.UP));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.PRIMARY));
        }

        // Barrel pressed during hover: SECONDBUTTON + PEN_FLAG_BARREL + SECONDBUTTON_DOWN, keep UPDATE.
        [Fact]
        public void DeriveFlags_BarrelBeginWhileHover_EmitsSecondButtonDown()
        {
            var (pointer, pen, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: false, prevContact: false,
                barrel: true, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.SECONDBUTTON));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.True(pen.HasFlag(PEN_FLAGS.BARREL));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.SECONDBUTTON_DOWN, change);
        }

        // Barrel released during contact: tip stays, barrel cleared, SECONDBUTTON_UP change.
        [Fact]
        public void DeriveFlags_BarrelEndDuringContact_EmitsSecondButtonUp()
        {
            var (pointer, pen, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: true,
                barrel: false, prevBarrel: true,
                isEraser: false);

            Assert.False(pointer.HasFlag(POINTER_FLAGS.SECONDBUTTON));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.INCONTACT));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.UPDATE));
            Assert.False(pen.HasFlag(PEN_FLAGS.BARREL));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.SECONDBUTTON_UP, change);
        }

        // Simultaneous tip-down and barrel transitions: tip change wins.
        [Fact]
        public void DeriveFlags_TipAndBarrelBothChange_TipWins()
        {
            var (pointer, _, change) = WindowsInkPointerBase.DeriveFlags(
                isNew: false, inRange: true, prevInRange: true,
                contact: true, prevContact: false,
                barrel: true, prevBarrel: false,
                isEraser: false);

            Assert.True(pointer.HasFlag(POINTER_FLAGS.DOWN));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.SECONDBUTTON));
            Assert.True(pointer.HasFlag(POINTER_FLAGS.FIRSTBUTTON));
            Assert.Equal(POINTER_BUTTON_CHANGE_TYPE.FIRSTBUTTON_DOWN, change);
        }
    }
}
