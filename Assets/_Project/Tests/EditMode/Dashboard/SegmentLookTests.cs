using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class SegmentLookTests
    {
        private static readonly DashboardPalette Palette = DashboardPalette.Default;

        [Test]
        public void ADoneSegment_IsFilledWithTheAccentColor()
        {
            SegmentLook look = SegmentLook.For(SegmentTone.Done, Palette);

            Assert.That(look.Fill, Is.EqualTo(Palette.Accent));
            Assert.That(look.HasOutline, Is.False);
        }

        [Test]
        public void AnActiveSegment_IsNotFilled_SoItDoesNotLookAlreadyPlaced()
        {
            SegmentLook active = SegmentLook.For(SegmentTone.Active, Palette);
            SegmentLook pending = SegmentLook.For(SegmentTone.Pending, Palette);

            Assert.That(active.Fill, Is.EqualTo(pending.Fill), "진행 중인 칸의 속은 대기 칸과 같은 빈 색이어야 한다");
            Assert.That(active.Fill, Is.Not.EqualTo(Palette.Accent));
        }

        [Test]
        public void AnActiveSegment_IsMarkedByAnAccentOutline()
        {
            SegmentLook look = SegmentLook.For(SegmentTone.Active, Palette);

            Assert.That(look.HasOutline, Is.True);
            Assert.That(look.Outline, Is.EqualTo(Palette.Accent));
        }

        [TestCase(SegmentTone.Pending)]
        [TestCase(SegmentTone.Skipped)]
        public void PendingAndSkippedSegments_HaveNoOutline_AndAreNotAccentFilled(SegmentTone tone)
        {
            SegmentLook look = SegmentLook.For(tone, Palette);

            Assert.That(look.HasOutline, Is.False);
            Assert.That(look.Fill, Is.Not.EqualTo(Palette.Accent));
        }

        [Test]
        public void OnlyTheDoneSegment_IsFilledWithAccent_AmongAllTones()
        {
            foreach (SegmentTone tone in System.Enum.GetValues(typeof(SegmentTone)))
            {
                bool isAccentFilled = SegmentLook.For(tone, Palette).Fill == Palette.Accent;

                Assert.That(isAccentFilled, Is.EqualTo(tone == SegmentTone.Done), tone.ToString());
            }
        }
    }
}
