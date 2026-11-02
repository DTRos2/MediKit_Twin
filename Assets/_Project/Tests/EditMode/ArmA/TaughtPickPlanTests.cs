using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtPickPlanTests
    {
        // 칸 밖으로 나가는 방향은 월드 -Z, 도구는 칸 안쪽(+Z)을 향한다.
        private static readonly Vector3 Outward = Vector3.back;

        private static TaughtPickPose Pose(Quaternion toolRotation) => TaughtPickPose.Capture(
            "A-1",
            new Vector3(1.85f, 0f, 1.40f),
            7f,
            new Vector3(1.88f, 0.55f, 1.42f),
            new Pose(new Vector3(2.02f, 0.73f, 2.30f), toolRotation),
            new[] { 0f, 0f, 0f });

        private static TaughtPickPlan Plan(Quaternion toolRotation, Vector3 outward, float waypointExtra = 0.3f, float approach = 0.15f, float retreat = 0.2f) =>
            TaughtPickPlan.For(Pose(toolRotation), outward, waypointExtra, approach, retreat);

        [Test]
        public void StandsAtTheTaughtArmBaseAndFacesTheTaughtYaw()
        {
            TaughtPickPlan plan = Plan(Quaternion.identity, Outward);

            Assert.That(plan.ArmBaseStand.x, Is.EqualTo(1.88f).Within(1e-5f));
            Assert.That(plan.ArmBaseStand.z, Is.EqualTo(1.42f).Within(1e-5f));
            Assert.That(plan.RootYawDeg, Is.EqualTo(7f));
        }

        [Test]
        public void TheWaypointIsBehindTheStand_AlongTheOutwardDirection()
        {
            TaughtPickPlan plan = Plan(Quaternion.identity, Outward, waypointExtra: 0.3f);

            Assert.That(plan.ArmBaseWaypoint.HasValue, Is.True);
            Assert.That(plan.ArmBaseWaypoint.Value.x, Is.EqualTo(1.88f).Within(1e-5f));
            Assert.That(plan.ArmBaseWaypoint.Value.z, Is.EqualTo(1.42f - 0.3f).Within(1e-5f), "칸에서 더 바깥(-Z)으로 0.3m 물러나 있다");
        }

        [Test]
        public void TheOutwardDirectionIsFlattenedAndNormalized()
        {
            TaughtPickPlan plan = Plan(Quaternion.identity, new Vector3(0f, 5f, -2f), waypointExtra: 0.3f);

            Assert.That(plan.ArmBaseWaypoint.Value.z, Is.EqualTo(1.42f - 0.3f).Within(1e-5f));
            Assert.That(plan.ArmBaseWaypoint.Value.y, Is.EqualTo(0f));
        }

        [Test]
        public void WithoutWaypointDistance_ThereIsNoWaypoint()
        {
            Assert.That(Plan(Quaternion.identity, Outward, waypointExtra: 0f).ArmBaseWaypoint.HasValue, Is.False);
        }

        [Test]
        public void WithoutAnOutwardDirection_ThereIsNoWaypoint()
        {
            Assert.That(Plan(Quaternion.identity, Vector3.zero).ArmBaseWaypoint.HasValue, Is.False);
            Assert.That(Plan(Quaternion.identity, Vector3.up).ArmBaseWaypoint.HasValue, Is.False, "수평 성분이 없으면 방향을 알 수 없다");
        }

        [Test]
        public void GraspsAtTheTaughtToolPositionWithTheTaughtRotation()
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, 90f);

            TaughtPickPlan plan = Plan(rotation, Outward);

            Assert.That(Vector3.Distance(plan.GraspPosition, new Vector3(2.02f, 0.73f, 2.30f)), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(plan.ToolRotation, rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void NearAndRetreatAreBackAlongTheToolAxis()
        {
            // 도구가 +Z(칸 안쪽)를 향하면 들어가기 전과 뺀 뒤 자리는 그 뒤쪽(-Z)에 있다.
            TaughtPickPlan plan = Plan(Quaternion.identity, Outward, approach: 0.15f, retreat: 0.2f);

            Assert.That(plan.NearPosition.z, Is.EqualTo(2.30f - 0.15f).Within(1e-5f));
            Assert.That(plan.RetreatPosition.z, Is.EqualTo(2.30f - 0.2f).Within(1e-5f));
            Assert.That(plan.NearPosition.y, Is.EqualTo(0.73f).Within(1e-5f));
        }

        [Test]
        public void ATiltedTool_ApproachesAlongItsOwnAxis()
        {
            // 도구 끝이 아래로 30도 기울어 있으면 뒤로 빼는 쪽은 위로도 올라간다.
            Quaternion tilted = Quaternion.Euler(30f, 0f, 0f);
            Vector3 axis = tilted * Vector3.forward;

            TaughtPickPlan plan = Plan(tilted, Outward, approach: 0.2f);

            Vector3 expected = new Vector3(2.02f, 0.73f, 2.30f) - axis * 0.2f;
            Assert.That(Vector3.Distance(plan.NearPosition, expected), Is.LessThan(1e-5f));
            Assert.That(plan.NearPosition.y, Is.GreaterThan(0.73f), "도구가 아래로 기울었으니 뒤로 빼면 위쪽이다");
        }

        [Test]
        public void ARolledToolKeepsTheSameApproachPositions()
        {
            // 손목만 비튼 자세는 칸으로 들어가는 축이 같으므로 접근 위치도 같다.
            TaughtPickPlan plain = Plan(Quaternion.identity, Outward);
            TaughtPickPlan rolled = Plan(Quaternion.Euler(0f, 0f, 90f), Outward);

            Assert.That(Vector3.Distance(plain.NearPosition, rolled.NearPosition), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(plain.RetreatPosition, rolled.RetreatPosition), Is.LessThan(1e-5f));
        }

        [Test]
        public void NullPose_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TaughtPickPlan.For(null, Outward, 0.3f, 0.15f, 0.15f));
        }
    }
}
