using NUnit.Framework;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class SliderMappingTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void TheFraction_IsWhereTheValueSitsInTheRange()
        {
            Assert.That(SliderMapping.ToFraction(0f, -90f, 90f), Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(SliderMapping.ToFraction(-90f, -90f, 90f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(SliderMapping.ToFraction(90f, -90f, 90f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void AValueOutsideTheRange_IsClampedToTheEnds()
        {
            Assert.That(SliderMapping.ToFraction(500f, 0f, 100f), Is.EqualTo(1f));
            Assert.That(SliderMapping.ToFraction(-5f, 0f, 100f), Is.EqualTo(0f));
        }

        [Test]
        public void ARangeWithoutWidth_PutsTheThumbAtTheStart()
        {
            Assert.That(SliderMapping.ToFraction(5f, 10f, 10f), Is.EqualTo(0f));
            Assert.That(SliderMapping.ToFraction(5f, 10f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void FromFraction_ClampsBeforeInterpolating()
        {
            Assert.That(SliderMapping.FromFraction(0.25f, 0f, 200f), Is.EqualTo(50f).Within(Tolerance));
            Assert.That(SliderMapping.FromFraction(2f, 0f, 200f), Is.EqualTo(200f));
            Assert.That(SliderMapping.FromFraction(-1f, 0f, 200f), Is.EqualTo(0f));
        }

        [Test]
        public void TheMouseX_MapsToTheValueAlongTheTrack()
        {
            // 움직이는 구간이 x 20부터 100칸이고 값 범위가 0~10이면, x 70은 한가운데(5), x 45는 4분의 1 지점(2.5)이다.
            Assert.That(SliderMapping.ValueAtX(70f, 20f, 100f, 0f, 10f), Is.EqualTo(5f).Within(Tolerance));
            Assert.That(SliderMapping.ValueAtX(45f, 20f, 100f, 0f, 10f), Is.EqualTo(2.5f).Within(Tolerance));
        }

        [Test]
        public void TheMouseBeyondTheTrack_StopsAtTheEnds()
        {
            Assert.That(SliderMapping.ValueAtX(-300f, 20f, 100f, 0f, 10f), Is.EqualTo(0f));
            Assert.That(SliderMapping.ValueAtX(900f, 20f, 100f, 0f, 10f), Is.EqualTo(10f));
        }

        [Test]
        public void ATrackWithoutWidth_GivesTheMinimum()
        {
            Assert.That(SliderMapping.ValueAtX(70f, 20f, 0f, 3f, 10f), Is.EqualTo(3f));
        }

        [Test]
        public void TheThumbPosition_AndTheMouseValue_AreInverseOfEachOther()
        {
            const float trackStart = 30f;
            const float trackWidth = 240f;
            float x = SliderMapping.XOfValue(45f, trackStart, trackWidth, -90f, 90f);

            Assert.That(SliderMapping.ValueAtX(x, trackStart, trackWidth, -90f, 90f), Is.EqualTo(45f).Within(Tolerance));
        }
    }
}
