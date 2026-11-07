using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspCorrectionTests
    {
        private const float Min = 0.004f;
        private const float Max = 0.03f;

        [Test]
        public void ADifferenceBelowTheMinimum_IsIgnored()
        {
            Vector3 shift = GraspCorrection.Compute(Vector3.zero, new Vector3(0.002f, 0f, 0f), Min, Max, out bool isClamped);

            Assert.That(shift, Is.EqualTo(Vector3.zero));
            Assert.That(isClamped, Is.False);
        }

        [Test]
        public void ADifferenceInsideTheLimits_IsFollowedExactly()
        {
            Vector3 shift = GraspCorrection.Compute(new Vector3(1f, 1f, 1f), new Vector3(1.01f, 1f, 0.99f), Min, Max, out bool isClamped);

            Assert.That(shift.x, Is.EqualTo(0.01f).Within(1e-5f));
            Assert.That(shift.z, Is.EqualTo(-0.01f).Within(1e-5f));
            Assert.That(isClamped, Is.False);
        }

        [Test]
        public void ADifferenceAboveTheMaximum_IsCutToTheMaximumInTheSameDirection()
        {
            Vector3 shift = GraspCorrection.Compute(Vector3.zero, new Vector3(0f, 0.5f, 0f), Min, Max, out bool isClamped);

            Assert.That(isClamped, Is.True);
            Assert.That(shift.magnitude, Is.EqualTo(Max).Within(1e-5f));
            Assert.That(shift.normalized, Is.EqualTo(Vector3.up));
        }

        [Test]
        public void NoDifference_GivesNoShift_EvenWithAZeroMinimum()
        {
            Vector3 shift = GraspCorrection.Compute(Vector3.one, Vector3.one, 0f, Max, out bool isClamped);

            Assert.That(shift, Is.EqualTo(Vector3.zero));
            Assert.That(isClamped, Is.False);
        }
    }
}
