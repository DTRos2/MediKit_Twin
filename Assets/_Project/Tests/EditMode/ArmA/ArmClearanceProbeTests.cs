using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmClearanceProbeTests
    {
        private static readonly Pose BaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);

        private FakeObstacles _obstacles;
        private FakeSeededSolver _solver;
        private float[] _seed;
        private Pose _currentBase;

        [SetUp]
        public void SetUp()
        {
            _obstacles = new FakeObstacles();
            _solver = new FakeSeededSolver();
            _seed = new[] { 0.3f, 0f };
            _currentBase = BaseAtOrigin;
        }

        private ArmClearanceProbe Create(Func<float[]> seedProvider = null)
        {
            var checker = new ArmCollisionChecker(
                PerceptionTestData.TwoLinkArm(),
                new[] { PerceptionTestData.Link1Bar() },
                _obstacles,
                0f,
                0,
                new[] { "link_a", "link_b" });
            return new ArmClearanceProbe(_solver, seedProvider ?? (() => _seed), checker, () => _currentBase, 2);
        }

        // 가짜 풀이는 도구 자세의 x를 첫 관절 각도로 삼는다. x = 0이면 팔이 곧게 뻗는다(막대 중심 (1.5, 0, 0)).
        private static Pose ToolAtX(float x) => new Pose(new Vector3(x, 0f, 0f), Quaternion.identity);

        [Test]
        public void WhenTheSolvedPostureOverlapsAnObstacle_ThePoseIsNotClear()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            Assert.That(Create().IsClear(ToolAtX(0f)), Is.False);
        }

        [Test]
        public void WhenTheSolvedPostureMissesTheObstacle_ThePoseIsClear()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            Assert.That(Create().IsClear(ToolAtX(Mathf.PI / 2f)), Is.True);
        }

        [Test]
        public void APoseThatCannotBeSolved_IsNotJudged_SoItCountsAsClear()
        {
            _solver.Solves = false;
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            Assert.That(Create().IsClear(ToolAtX(0f)), Is.True);
            Assert.That(_obstacles.Queries, Is.EqualTo(0));
        }

        [Test]
        public void TheSolveStartsFromTheSeedTheCallerGives()
        {
            Create(() => new[] { 1f, 2f }).IsClear(ToolAtX(0f));

            Assert.That(_solver.LastSeed[0], Is.EqualTo(1f));
            Assert.That(_solver.LastSeed[1], Is.EqualTo(2f));
        }

        [Test]
        public void AMissingSeed_IsTreatedAsNotJudgeable()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            Assert.That(Create(() => null).IsClear(ToolAtX(0f)), Is.True);
        }

        [Test]
        public void TheArmIsPlacedAtTheGivenBase_ForPlanningBeforeStandingThere()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            ArmClearanceProbe probe = Create();

            Assert.That(probe.IsClear(ToolAtX(0f), BaseAtOrigin), Is.False);
            Assert.That(probe.IsClear(ToolAtX(0f), new Pose(new Vector3(0f, 0f, 5f), Quaternion.identity)), Is.True);
        }

        [Test]
        public void TheCurrentBaseIsReadEveryTime_SoAMovedArmIsJudgedWhereItNowStands()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            ArmClearanceProbe probe = Create();

            Assert.That(probe.IsClear(ToolAtX(0f)), Is.False);
            _currentBase = new Pose(new Vector3(0f, 0f, 5f), Quaternion.identity);
            Assert.That(probe.IsClear(ToolAtX(0f)), Is.True);
        }

        [Test]
        public void AJointRoute_IsJudgedByItsJointAngles_NotByTheToolPose()
        {
            var planned = new GraspTargets(Quaternion.identity, Vector3.zero, Vector3.zero, Vector3.zero, null);
            TaughtJointRoute straight = BuildRoute(0f);
            TaughtJointRoute turned = BuildRoute(Mathf.PI / 2f);
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            ArmClearanceProbe probe = Create();

            Assert.That(probe.IsClear(new GraspTargets(planned.Rotation, planned.Near, planned.Grasp, planned.Retreat, straight)), Is.False);
            Assert.That(probe.IsClear(new GraspTargets(planned.Rotation, planned.Near, planned.Grasp, planned.Retreat, turned)), Is.True);
        }

        [Test]
        public void WithoutARoute_AllThreeToolPosesMustBeClear()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            ArmClearanceProbe probe = Create();
            float turned = Mathf.PI / 2f;

            var allTurned = new GraspTargets(Quaternion.identity, new Vector3(turned, 0f, 0f), new Vector3(turned, 0f, 0f), new Vector3(turned, 0f, 0f), null);
            var oneStraight = new GraspTargets(Quaternion.identity, new Vector3(turned, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(turned, 0f, 0f), null);

            Assert.That(probe.IsClear(allTurned), Is.True);
            Assert.That(probe.IsClear(oneStraight), Is.False, "쥐는 자리 하나만 겹쳐도 겹치는 것으로 본다");
        }

        [Test]
        public void TheCollisionIsDescribed_ForTheLog()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            ArmClearanceProbe probe = Create();

            Assert.That(probe.DescribeCollision(ToolAtX(0f)), Does.Contain("shelf_board"));
            Assert.That(probe.DescribeCollision(ToolAtX(Mathf.PI / 2f)), Is.Empty);
        }

        private static TaughtJointRoute BuildRoute(float firstJointRad)
        {
            var solver = new FakeSeededSolver();
            var builder = new SeededRouteBuilder(solver);
            var at = new Pose(new Vector3(firstJointRad, 0f, 0f), Quaternion.identity);
            Assert.That(builder.TryBuild(new float[] { 0f, 0f }, at, at, at, out TaughtJointRoute route), Is.True);
            return route;
        }
    }
}
