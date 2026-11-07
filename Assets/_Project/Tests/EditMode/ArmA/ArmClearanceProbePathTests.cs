using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmClearanceProbePathTests
    {
        private static readonly Pose BaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);

        private FakeObstacles _obstacles;
        private FakeSeededSolver _solver;

        [SetUp]
        public void SetUp()
        {
            // 첫 관절이 0일 때 링크1 막대의 중심이 (1.5, 0, 0)이다. 장애물은 거기에 있다. 팔은 첫 관절 0.3에서 출발한다.
            _obstacles = new FakeObstacles();
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            _solver = new FakeSeededSolver();
        }

        private ArmClearanceProbe Create()
        {
            var checker = new ArmCollisionChecker(
                PerceptionTestData.TwoLinkArm(), new[] { PerceptionTestData.Link1Bar() }, _obstacles, 0f, 0, new[] { "link_a", "link_b" });
            return new ArmClearanceProbe(_solver, () => new[] { 0.3f, 0f }, checker, () => BaseAtOrigin, 2);
        }

        private static TaughtJointRoute RouteAt(float firstJointRad)
        {
            var at = new Pose(new Vector3(firstJointRad, 0f, 0f), Quaternion.identity);
            Assert.That(new SeededRouteBuilder(new FakeSeededSolver()).TryBuild(new float[] { 0f, 0f }, at, at, at, out TaughtJointRoute route), Is.True);
            return route;
        }

        private static GraspTargets Targets(TaughtJointRoute route, float x) =>
            new GraspTargets(Quaternion.identity, new Vector3(x, 0f, 0f), new Vector3(x, 0f, 0f), new Vector3(x, 0f, 0f), route);

        [Test]
        public void ARouteWhosePointsAreClearButWhoseWayCrossesTheObstacle_IsNotClear()
        {
            // 세 자리 모두 첫 관절 −0.3으로 겹치지 않지만, 0.3에서 −0.3까지 가는 길이 장애물(첫 관절 0 근처)을 지난다.
            Assert.That(Create().IsClear(Targets(RouteAt(-0.3f), -0.3f)), Is.False);
        }

        [Test]
        public void ARouteThatStaysOnTheSameSideOfTheObstacle_IsClear()
        {
            Assert.That(Create().IsClear(Targets(RouteAt(1.0f), 1.0f)), Is.True);
        }

        [Test]
        public void WithoutARoute_ThePosesAreSolvedAndTheWayBetweenThemIsJudgedToo()
        {
            Assert.That(Create().IsClear(Targets(null, -0.3f)), Is.False);
            Assert.That(Create().IsClear(Targets(null, 1.0f)), Is.True);
        }

        [Test]
        public void APoseThatCannotBeSolved_LeavesItsPartOfTheWayUnjudged()
        {
            _solver.Solves = false;

            Assert.That(Create().IsClear(Targets(null, -0.3f)), Is.True);
            Assert.That(_obstacles.Queries, Is.EqualTo(0));
        }

        [Test]
        public void TheCollisionOnTheWay_IsDescribedForTheLog()
        {
            ArmClearanceProbe probe = Create();

            Assert.That(probe.DescribeCollision(Targets(RouteAt(-0.3f), -0.3f)), Does.Contain("shelf_board"));
            Assert.That(probe.DescribeCollision(Targets(RouteAt(1.0f), 1.0f)), Is.Empty);
        }
    }
}
