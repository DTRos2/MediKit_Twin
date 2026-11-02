using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ReleaseHeightsTests
    {
        [Test]
        public void SortsFromLowestToHighest()
        {
            var heights = new ReleaseHeights(new[] { 0.11f, 0f, 0.07f, 0.04f });

            Assert.That(heights, Is.EqualTo(new[] { 0f, 0.04f, 0.07f, 0.11f }));
        }

        [Test]
        public void HighestIsTheLastTriedHeight()
        {
            Assert.That(new ReleaseHeights(new[] { 0.04f, 0.11f, 0.07f }).Highest, Is.EqualTo(0.11f));
        }

        [Test]
        public void NeverGoesBelowTheSlotCenter()
        {
            var heights = new ReleaseHeights(new[] { -0.05f, 0.04f });

            Assert.That(heights, Is.EqualTo(new[] { 0f, 0.04f }));
        }

        [Test]
        public void MergesHeightsThatAreTheSame()
        {
            // 음수는 0으로 고쳐지므로 0과 합쳐지고, 인스펙터에서 적은 오차 수준의 차이도 한 번만 시도한다.
            var heights = new ReleaseHeights(new[] { 0f, -0.01f, 0.05f, 0.05f, 0.05004f });

            Assert.That(heights.Count, Is.EqualTo(2));
        }

        [Test]
        public void IgnoresValuesThatAreNotNumbers()
        {
            var heights = new ReleaseHeights(new[] { float.NaN, float.PositiveInfinity, 0.06f });

            Assert.That(heights, Is.EqualTo(new[] { 0.06f }));
        }

        [Test]
        public void WithoutAnyHeight_ItIsTheSlotCenterOnly()
        {
            Assert.That(new ReleaseHeights(null), Is.EqualTo(new[] { 0f }));
            Assert.That(new ReleaseHeights(new float[0]), Is.EqualTo(new[] { 0f }));
            Assert.That(ReleaseHeights.AtSlotCenter, Is.EqualTo(new[] { 0f }));
        }
    }
}
