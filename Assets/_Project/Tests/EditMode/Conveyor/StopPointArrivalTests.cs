using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class StopPointArrivalTests
    {
        private static readonly Vector3 Stop = new Vector3(2f, 0.5f, -1f);

        // 진행 방향이 -Z인 구간(검사 구간처럼 아래로 흐르는 구간)을 본다.
        private static readonly Vector3 FlowMinusZ = new Vector3(0f, 0f, -1f);

        [Test]
        public void AlongTrack_IsNegative_BeforeTheStopPoint_AndPositive_PastIt()
        {
            Assert.That(StopPointArrival.AlongTrack(Stop + new Vector3(0f, 0f, 0.3f), Stop, FlowMinusZ), Is.EqualTo(-0.3f).Within(1e-5f));
            Assert.That(StopPointArrival.AlongTrack(Stop + new Vector3(0f, 0f, -0.2f), Stop, FlowMinusZ), Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void AlongTrack_IgnoresSidewaysOffsetAndHeight()
        {
            Vector3 beside = Stop + new Vector3(0.4f, 0.3f, 0.1f);

            Assert.That(StopPointArrival.AlongTrack(beside, Stop, FlowMinusZ), Is.EqualTo(-0.1f).Within(1e-5f));
        }

        [Test]
        public void AlongTrack_DoesNotDependOnTheFlowVectorLength()
        {
            Vector3 past = Stop + new Vector3(0f, 0f, -0.2f);

            Assert.That(StopPointArrival.AlongTrack(past, Stop, FlowMinusZ * 7f), Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void AlongTrack_IsZero_WhenTheFlowDirectionIsUnknown()
        {
            Assert.That(StopPointArrival.AlongTrack(Stop + Vector3.right, Stop, Vector3.zero), Is.EqualTo(0f));
            Assert.That(StopPointArrival.AlongTrack(Stop + Vector3.right, Stop, Vector3.up), Is.EqualTo(0f), "세로 방향은 가로 평면에서 방향이 없다");
        }

        [Test]
        public void HasReached_IsFalse_BeforeTheStopPoint()
        {
            Assert.That(StopPointArrival.HasReached(Stop + new Vector3(0f, 0f, 0.04f), Stop, FlowMinusZ), Is.False);
        }

        [Test]
        public void HasReached_IsTrue_OnTheStopPoint_AndPastIt()
        {
            Assert.That(StopPointArrival.HasReached(Stop, Stop, FlowMinusZ), Is.True);
            Assert.That(StopPointArrival.HasReached(Stop + new Vector3(0f, 0f, -0.04f), Stop, FlowMinusZ), Is.True);
        }

        [Test]
        public void HasReached_CatchesAStepThatJumpedOverTheStopPoint()
        {
            // 거리로만 재면 4cm씩 움직이는 박스가 자리 앞 3cm에서 자리 뒤 1cm로 건너뛸 때 놓친다. 지났는지로 보면 놓치지 않는다.
            Vector3 before = Stop + new Vector3(0f, 0f, 0.03f);
            Vector3 after = Stop + new Vector3(0f, 0f, -0.01f);

            Assert.That(StopPointArrival.HasReached(before, Stop, FlowMinusZ), Is.False);
            Assert.That(StopPointArrival.HasReached(after, Stop, FlowMinusZ), Is.True);
        }

        [Test]
        public void SnapToStopPoint_MovesBackToTheStopPlane_KeepingSidewaysPositionAndHeight()
        {
            Vector3 past = new Vector3(2.03f, 0.9f, -1.04f);

            Vector3 snapped = StopPointArrival.SnapToStopPoint(past, Stop, FlowMinusZ);

            Assert.That(snapped.z, Is.EqualTo(Stop.z).Within(1e-5f));
            Assert.That(snapped.x, Is.EqualTo(2.03f).Within(1e-5f), "옆 위치는 벨트가 정한 그대로 둔다");
            Assert.That(snapped.y, Is.EqualTo(0.9f).Within(1e-5f), "높이는 벨트가 정한 그대로 둔다");
        }

        [Test]
        public void SnapToStopPoint_ReturnsThePositionUnchanged_WhenTheFlowDirectionIsUnknown()
        {
            Vector3 position = new Vector3(1f, 2f, 3f);

            Assert.That(StopPointArrival.SnapToStopPoint(position, Stop, Vector3.zero), Is.EqualTo(position));
        }

        [Test]
        public void SnapToStopPoint_WorksForADiagonalFlow()
        {
            Vector3 diagonal = new Vector3(1f, 0f, 1f);
            Vector3 past = Stop + diagonal.normalized * 0.05f + new Vector3(-0.01f, 0f, 0.01f);

            Vector3 snapped = StopPointArrival.SnapToStopPoint(past, Stop, diagonal);

            Assert.That(StopPointArrival.AlongTrack(snapped, Stop, diagonal), Is.EqualTo(0f).Within(1e-5f));
        }
    }
}
