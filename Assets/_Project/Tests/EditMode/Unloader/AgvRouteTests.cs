using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Unloader.Tests
{
    public sealed class AgvRouteTests
    {
        // 서쪽 끝(0,0)에서 동쪽(10,0)으로 갔다가 북쪽(10,10)으로 꺾이는 경로.
        private static readonly List<Vector3> LShape = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(10f, 0f, 10f),
        };

        [Test]
        public void WaypointsBefore_IsZero_WhenThereIsNoPath()
        {
            Assert.That(AgvRoute.WaypointsBefore(null, Vector3.zero), Is.EqualTo(0));
            Assert.That(AgvRoute.WaypointsBefore(new List<Vector3>(), Vector3.zero), Is.EqualTo(0));
        }

        [Test]
        public void WaypointsBefore_IsOne_WhenThereIsOnlyOneWaypoint()
        {
            Assert.That(AgvRoute.WaypointsBefore(new List<Vector3> { Vector3.zero }, new Vector3(5f, 0f, 5f)), Is.EqualTo(1));
        }

        [Test]
        public void WaypointsBefore_StopsAtTheSegmentStart_WhenTheTargetIsBesideTheMiddleOfASegment()
        {
            // 첫 구간(0,0 - 10,0)의 한가운데 옆. 첫 웨이포인트까지만 가고 거기서 목표로 간다.
            Assert.That(AgvRoute.WaypointsBefore(LShape, new Vector3(5f, 0f, 2f)), Is.EqualTo(1));
        }

        [Test]
        public void WaypointsBefore_IncludesTheBendPoint_WhenTheTargetIsOnTheSecondSegment()
        {
            // 둘째 구간(10,0 - 10,10)의 옆. 꺾이는 점(10,0)까지 지나고 거기서 목표로 간다.
            Assert.That(AgvRoute.WaypointsBefore(LShape, new Vector3(10.3f, 0f, 6f)), Is.EqualTo(2));
        }

        [Test]
        public void WaypointsBefore_IncludesTheLastWaypoint_WhenTheTargetIsPastTheEnd()
        {
            Assert.That(AgvRoute.WaypointsBefore(LShape, new Vector3(10f, 0f, 10.5f)), Is.EqualTo(3));
        }

        [Test]
        public void WaypointsBefore_GoesToTheFirstWaypoint_WhenTheTargetIsBeforeTheStart()
        {
            Assert.That(AgvRoute.WaypointsBefore(LShape, new Vector3(-3f, 0f, 0f)), Is.EqualTo(1));
        }

        [Test]
        public void WaypointsBefore_IgnoresHeight()
        {
            Assert.That(AgvRoute.WaypointsBefore(LShape, new Vector3(10.3f, 5f, 6f)), Is.EqualTo(2));
        }

        [Test]
        public void StagePoint_IsBehindThePose_AlongTheOppositeOfItsFacing()
        {
            // yaw 90도는 +x를 본다. 스테이징은 그 반대인 -x쪽이다.
            Vector3 stage = AgvRoute.StagePoint(new Vector3(8.3f, 0f, 7.2f), 90f, 0.5f);

            Assert.That(stage.x, Is.EqualTo(7.8f).Within(1e-4f));
            Assert.That(stage.z, Is.EqualTo(7.2f).Within(1e-4f));
        }

        [Test]
        public void StagePoint_FollowsTheYaw()
        {
            // yaw 0도는 +z를 본다. 스테이징은 -z쪽이다. 높이는 자리와 같다.
            Vector3 stage = AgvRoute.StagePoint(new Vector3(1f, 0.2f, 2f), 0f, 0.5f);

            Assert.That(stage.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(stage.z, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(stage.y, Is.EqualTo(0.2f).Within(1e-4f));
        }
    }
}
