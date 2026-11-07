using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class SegmentProjectionTests
    {
        // 길이 1m, +X 방향 구간. 높이는 서로 달라도 가로 평면에서만 본다.
        private static readonly Vector3 Start = new Vector3(0f, 0.5f, 0f);
        private static readonly Vector3 End = new Vector3(1f, 0.5f, 0f);

        [Test]
        public void TryProject_ReturnsThePositionAlongTheSegment_AndTheSidewaysDistance()
        {
            bool inside = SegmentProjection.TryProject(Start, End, new Vector3(0.25f, 3f, 0.04f), out float along, out float lateral);

            Assert.That(inside, Is.True);
            Assert.That(along, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(lateral, Is.EqualTo(0.04f).Within(1e-5f));
        }

        [Test]
        public void TryProject_IsTrue_AtTheEnds()
        {
            Assert.That(SegmentProjection.TryProject(Start, End, Start, out _, out _), Is.True);
            Assert.That(SegmentProjection.TryProject(Start, End, End, out _, out _), Is.True);
        }

        [Test]
        public void TryProject_IsFalse_WhenTheFootIsBeyondTheEnds()
        {
            Assert.That(SegmentProjection.TryProject(Start, End, new Vector3(-0.1f, 0.5f, 0f), out float before, out _), Is.False);
            Assert.That(before, Is.EqualTo(-0.1f).Within(1e-5f));
            Assert.That(SegmentProjection.TryProject(Start, End, new Vector3(1.2f, 0.5f, 0f), out float after, out _), Is.False);
            Assert.That(after, Is.EqualTo(1.2f).Within(1e-5f));
        }

        [Test]
        public void TryProject_WorksForASegmentRunningTheOtherWay()
        {
            bool inside = SegmentProjection.TryProject(End, Start, new Vector3(0.25f, 0.5f, 0f), out float along, out _);

            Assert.That(inside, Is.True);
            Assert.That(along, Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void TryProject_IsFalse_ForASegmentWithoutLength()
        {
            bool inside = SegmentProjection.TryProject(Start, Start, new Vector3(0.3f, 0.5f, 0.4f), out _, out float lateral);

            Assert.That(inside, Is.False);
            Assert.That(lateral, Is.EqualTo(0.5f).Within(1e-5f));
        }
    }
}
