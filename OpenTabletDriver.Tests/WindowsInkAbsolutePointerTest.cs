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

        // Positions round to the nearest pixel instead of truncating toward zero,
        // which matters on displays left of / above the primary (negative coords).
        [Theory]
        [InlineData(10.4f, 10)]
        [InlineData(10.6f, 11)]
        [InlineData(10.5f, 11)]
        [InlineData(-0.4f, 0)]
        [InlineData(-0.6f, -1)]
        [InlineData(-1919.7f, -1920)]
        public void ToPixel_RoundsToNearest(float value, int expected)
        {
            var point = WindowsInkPointerBase.ToPixel(new System.Numerics.Vector2(value, value));

            Assert.Equal(expected, point.X);
            Assert.Equal(expected, point.Y);
        }

        // Normalized barrel rotation maps onto Windows' 0-359 degree range, wrapping 1.0 back to 0.
        [Theory]
        [InlineData(0f, 0u)]
        [InlineData(0.25f, 90u)]
        [InlineData(0.5f, 180u)]
        [InlineData(0.999f, 0u)]
        [InlineData(1f, 0u)]
        [InlineData(-0.1f, 0u)]
        public void ToRotationDegrees_MapsPercentageToDegrees(float percentage, uint expected)
        {
            Assert.Equal(expected, WindowsInkPointerBase.ToRotationDegrees(percentage));
        }

        // Rotation is only advertised once the tablet has actually reported it.
        [Fact]
        public void BuildPenMask_WithoutRotation_OmitsRotation()
        {
            var mask = WindowsInkPointerBase.BuildPenMask(hasRotation: false);

            Assert.Equal(PEN_MASK.PRESSURE | PEN_MASK.TILT_X | PEN_MASK.TILT_Y, mask);
        }

        [Fact]
        public void BuildPenMask_WithRotation_IncludesRotation()
        {
            var mask = WindowsInkPointerBase.BuildPenMask(hasRotation: true);

            Assert.Equal(PEN_MASK.PRESSURE | PEN_MASK.ROTATION | PEN_MASK.TILT_X | PEN_MASK.TILT_Y, mask);
        }
    }
}
