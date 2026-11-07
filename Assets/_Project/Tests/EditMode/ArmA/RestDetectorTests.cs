using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class RestDetectorTests
    {
        private static RestDetector Create(int samples = 3) => new RestDetector(0.01f, 0.2f, samples);

        [Test]
        public void IsNotAtRestUntilItHasBeenSlowForTheRequiredSamplesInARow()
        {
            RestDetector detector = Create(3);

            Assert.That(detector.Update(0f, 0f), Is.False);
            Assert.That(detector.Update(0f, 0f), Is.False);
            Assert.That(detector.Update(0f, 0f), Is.True);
        }

        [Test]
        public void AFastSampleStartsTheCountOver()
        {
            RestDetector detector = Create(3);
            detector.Update(0f, 0f);
            detector.Update(0f, 0f);

            // 바닥에 닿아 튀어 오르는 순간: 느린 두 번 뒤에 빠른 한 번이 오면 처음부터 다시 센다.
            Assert.That(detector.Update(0.5f, 0f), Is.False);
            Assert.That(detector.SlowSamples, Is.EqualTo(0));
            Assert.That(detector.Update(0f, 0f), Is.False);
            Assert.That(detector.Update(0f, 0f), Is.False);
            Assert.That(detector.Update(0f, 0f), Is.True);
        }

        [Test]
        public void SpinningFastCountsAsMovingEvenWhenTheSpeedIsLow()
        {
            RestDetector detector = Create(1);

            Assert.That(detector.Update(0f, 1f), Is.False);
        }

        [Test]
        public void TheLimitsThemselvesCountAsSlow()
        {
            RestDetector detector = Create(1);

            Assert.That(detector.Update(0.01f, 0.2f), Is.True);
        }

        [Test]
        public void StaysAtRestWhileSamplesKeepComingSlow()
        {
            RestDetector detector = Create(2);
            detector.Update(0f, 0f);
            detector.Update(0f, 0f);

            Assert.That(detector.Update(0f, 0f), Is.True);
        }

        [Test]
        public void ResetClearsTheCount()
        {
            RestDetector detector = Create(2);
            detector.Update(0f, 0f);

            detector.Reset();

            Assert.That(detector.SlowSamples, Is.EqualTo(0));
            Assert.That(detector.Update(0f, 0f), Is.False);
        }

        [Test]
        public void RejectsLimitsThatCannotBeMet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RestDetector(-0.1f, 0.2f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RestDetector(0.01f, -0.1f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RestDetector(0.01f, 0.2f, 0));
        }
    }
}
