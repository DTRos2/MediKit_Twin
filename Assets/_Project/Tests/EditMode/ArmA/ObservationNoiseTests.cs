using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ObservationNoiseTests
    {
        [Test]
        public void ASensorWithoutNoise_AddsNothing()
        {
            Assert.That(ObservationNoise.None.SampleOffset(), Is.EqualTo(Vector3.zero));
            Assert.That(new ObservationNoise(0f, 5).SampleOffset(), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TheSameSeed_GivesTheSameOffsetsInTheSameOrder()
        {
            var first = new ObservationNoise(0.01f, 42);
            var second = new ObservationNoise(0.01f, 42);

            for (int i = 0; i < 5; i++)
            {
                Assert.That(first.SampleOffset(), Is.EqualTo(second.SampleOffset()));
            }
        }

        [Test]
        public void EachAxisStaysWithinThreeSigma_SoNoSampleThrowsTheArmFarOff()
        {
            var noise = new ObservationNoise(0.01f, 3);

            for (int i = 0; i < 2000; i++)
            {
                Vector3 offset = noise.SampleOffset();
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(0.03f + 1e-6f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(0.03f + 1e-6f));
                Assert.That(Mathf.Abs(offset.z), Is.LessThanOrEqualTo(0.03f + 1e-6f));
            }
        }

        [Test]
        public void TheOffsets_AverageNearZero_AndSpreadAboutSigma()
        {
            var noise = new ObservationNoise(0.01f, 11);
            const int count = 4000;
            float sum = 0f;
            float sumSquares = 0f;

            for (int i = 0; i < count; i++)
            {
                float x = noise.SampleOffset().x;
                sum += x;
                sumSquares += x * x;
            }

            float mean = sum / count;
            float deviation = Mathf.Sqrt(sumSquares / count - mean * mean);
            Assert.That(mean, Is.EqualTo(0f).Within(0.001f));
            Assert.That(deviation, Is.EqualTo(0.01f).Within(0.002f));
        }
    }
}
