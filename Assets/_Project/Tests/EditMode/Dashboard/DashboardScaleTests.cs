using NUnit.Framework;

namespace MediKitTwin.Dashboard.Tests
{
    public sealed class DashboardScaleTests
    {
        [Test]
        public void TheReferenceScreen_HasNoScaling()
        {
            Assert.That(DashboardScale.For(DashboardMetrics.ReferenceWidth, DashboardMetrics.ReferenceHeight), Is.EqualTo(1f));
        }

        [Test]
        public void AScreenTwiceAsLarge_ScalesUpByTwo()
        {
            Assert.That(DashboardScale.For(3840f, 2160f), Is.EqualTo(2f));
        }

        [Test]
        public void TheScarcerSide_DecidesTheScale()
        {
            // 가로는 기준과 같고 세로만 줄면, 패널이 아래로 넘치지 않게 세로에 맞춘다(자르는 하한에 걸리지 않는 값으로).
            Assert.That(DashboardScale.For(1920f, 810f), Is.EqualTo(0.75f).Within(0.0001f));
            // 세로는 기준과 같고 가로만 줄어도 같다.
            Assert.That(DashboardScale.For(1440f, 1080f), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void TheScale_NeverDropsBelowTheMinimum_SoTextStaysReadable()
        {
            Assert.That(DashboardScale.For(320f, 200f), Is.EqualTo(DashboardScale.MinScale));
        }

        [Test]
        public void TheScale_NeverGrowsAboveTheMaximum()
        {
            Assert.That(DashboardScale.For(15360f, 8640f), Is.EqualTo(DashboardScale.MaxScale));
        }

        [TestCase(0f, 1080f)]
        [TestCase(1920f, 0f)]
        [TestCase(-1f, -1f)]
        public void AnInvalidScreenSize_FallsBackToNoScaling(float width, float height)
        {
            Assert.That(DashboardScale.For(width, height), Is.EqualTo(1f));
        }
    }
}
