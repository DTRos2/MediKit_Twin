using System;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GripClosureJudgeTests
    {
        // FR5 손가락: 직선 관절 2개, 열린 값 0, 닫힌 값 0.04m.
        private static readonly float[] Open = { 0f, 0f };
        private static readonly float[] Closed = { 0.04f, 0.04f };

        private static GripClosureJudge Judge() => new GripClosureJudge(0.85f);

        [Test]
        public void NothingBetweenTheFingers_WhenBothCloseAllTheWay()
        {
            Assert.That(Judge().IsBlocked(new[] { 0.04f, 0.04f }, Open, Closed), Is.False);
        }

        [Test]
        public void AlmostClosed_CountsAsClosed()
        {
            // 0.04의 90%까지 닫혔다: 기준(85%)을 넘었으므로 끝까지 닫힌 것으로 본다.
            Assert.That(Judge().IsBlocked(new[] { 0.036f, 0.036f }, Open, Closed), Is.False);
        }

        [Test]
        public void BlockedHalfway_WhenSomethingIsBetweenTheFingers()
        {
            Assert.That(Judge().IsBlocked(new[] { 0.008f, 0.008f }, Open, Closed), Is.True);
        }

        [Test]
        public void BlockedWhenOnlyOneFingerStopsShort_ForExampleAnOffCenterItem()
        {
            Assert.That(Judge().IsBlocked(new[] { 0.04f, 0.015f }, Open, Closed), Is.True);
        }

        [Test]
        public void WorksWhenTheClosedValueIsBelowTheOpenValue()
        {
            // 닫는 쪽이 값이 작은 관절(방향이 반대)도 같은 비율로 잰다.
            var open = new[] { 0f };
            var closed = new[] { -0.04f };

            Assert.That(Judge().IsBlocked(new[] { -0.04f }, open, closed), Is.False);
            Assert.That(Judge().IsBlocked(new[] { -0.01f }, open, closed), Is.True);
        }

        [Test]
        public void IgnoresJointsThatDoNotMove()
        {
            var open = new[] { 0f, 0.5f };
            var closed = new[] { 0.04f, 0.5f };

            Assert.That(Judge().IsBlocked(new[] { 0.04f, 0.5f }, open, closed), Is.False);
        }

        [Test]
        public void NoJoints_IsNotBlocked()
        {
            Assert.That(Judge().IsBlocked(new float[0], new float[0], new float[0]), Is.False);
        }

        [Test]
        public void RejectsMismatchedCounts()
        {
            Assert.Throws<ArgumentException>(() => Judge().IsBlocked(new[] { 0f }, Open, Closed));
        }

        [TestCase(0f)]
        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        public void RejectsAnInvalidRatio(float ratio)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GripClosureJudge(ratio));
        }
    }
}
