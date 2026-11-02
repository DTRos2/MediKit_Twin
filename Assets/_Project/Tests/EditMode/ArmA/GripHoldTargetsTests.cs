using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GripHoldTargetsTests
    {
        // FR5 손가락: 직선 관절 2개, 열린 값 0, 닫힌 값 0.04m.
        private static readonly float[] Open = { 0f, 0f };
        private static readonly float[] Closed = { 0.04f, 0.04f };

        [Test]
        public void Target_IsTheStoppedPositionPlusAPreload_NotTheClosedValue()
        {
            // 물건에 막혀 0.012m, 0.004m에서 멈췄다. 닫힌 값(0.04)까지 밀지 않고 거리의 5%(0.002m)만 더 간다.
            float[] targets = new GripHoldTargets(0.05f).Compute(new[] { 0.012f, 0.004f }, Open, Closed);

            Assert.That(targets[0], Is.EqualTo(0.014f).Within(1e-6f));
            Assert.That(targets[1], Is.EqualTo(0.006f).Within(1e-6f));
        }

        [Test]
        public void Target_NeverGoesPastTheClosedValue()
        {
            float[] targets = new GripHoldTargets(0.2f).Compute(new[] { 0.039f, 0.04f }, Open, Closed);

            Assert.That(targets[0], Is.EqualTo(0.04f).Within(1e-6f));
            Assert.That(targets[1], Is.EqualTo(0.04f).Within(1e-6f));
        }

        [Test]
        public void Target_WorksWhenTheClosedValueIsBelowTheOpenValue()
        {
            // 닫는 쪽이 값이 작은 관절(방향이 반대)은 값이 줄어드는 쪽으로 더 간다.
            float[] targets = new GripHoldTargets(0.1f).Compute(new[] { -0.01f }, new[] { 0f }, new[] { -0.04f });

            Assert.That(targets[0], Is.EqualTo(-0.014f).Within(1e-6f));
        }

        [Test]
        public void Target_OfAJointThatDoesNotMove_IsTheClosedValue()
        {
            float[] targets = new GripHoldTargets(0.05f).Compute(new[] { 0.3f }, new[] { 0.3f }, new[] { 0.3f });

            Assert.That(targets[0], Is.EqualTo(0.3f));
        }

        [Test]
        public void ZeroPreload_HoldsExactlyWhereTheFingerStopped()
        {
            float[] targets = new GripHoldTargets(0f).Compute(new[] { 0.012f, 0.004f }, Open, Closed);

            Assert.That(targets[0], Is.EqualTo(0.012f).Within(1e-6f));
            Assert.That(targets[1], Is.EqualTo(0.004f).Within(1e-6f));
        }

        [Test]
        public void PreloadOutsideZeroToOne_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GripHoldTargets(-0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GripHoldTargets(1.01f));
        }

        [Test]
        public void MismatchedLengths_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new GripHoldTargets(0.05f).Compute(new[] { 0f }, Open, Closed));
        }

        [Test]
        public void CurrentOpening_IsOneWhenOpenAndZeroWhenClosed()
        {
            Assert.That(GripHoldTargets.CurrentOpening(new[] { 0f, 0f }, Open, Closed), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(GripHoldTargets.CurrentOpening(new[] { 0.04f, 0.04f }, Open, Closed), Is.EqualTo(0f).Within(1e-6f));
        }

        [Test]
        public void CurrentOpening_IsTheAverageOverMovingJoints()
        {
            // 한 손가락은 절반, 다른 하나는 4분의 1 닫혔다: 열림은 (0.5 + 0.75) / 2.
            float opening = GripHoldTargets.CurrentOpening(new[] { 0.02f, 0.01f }, Open, Closed);

            Assert.That(opening, Is.EqualTo(0.625f).Within(1e-6f));
        }

        [Test]
        public void CurrentOpening_IgnoresJointsThatDoNotMove_AndIsOneWhenNoneMove()
        {
            Assert.That(GripHoldTargets.CurrentOpening(new[] { 0.3f }, new[] { 0.3f }, new[] { 0.3f }), Is.EqualTo(1f));
        }
    }
}
