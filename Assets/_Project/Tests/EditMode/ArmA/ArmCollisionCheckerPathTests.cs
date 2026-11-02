using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmCollisionCheckerPathTests
    {
        private static readonly Pose BaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);

        private FakeObstacles _obstacles;
        private ArmCollisionChecker _checker;

        [SetUp]
        public void SetUp()
        {
            // 첫 관절이 0일 때 링크1 막대의 중심이 (1.5, 0, 0)이다. 장애물은 거기에 있다.
            _obstacles = new FakeObstacles();
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));
            _checker = new ArmCollisionChecker(
                PerceptionTestData.TwoLinkArm(), new[] { PerceptionTestData.Link1Bar() }, _obstacles, 0f, 0, new[] { "link_a", "link_b" });
        }

        [Test]
        public void ThePathThroughTheObstacle_Collides_EvenWhenBothEndsAreClear()
        {
            bool collides = _checker.PathCollides(new[] { 0.3f, 0f }, new[] { -0.3f, 0f }, BaseAtOrigin, 0.1f, out string description);

            Assert.That(collides, Is.True);
            Assert.That(description, Does.Contain("shelf_board"));
            Assert.That(description, Does.Contain("경로"));
        }

        [Test]
        public void APathThatNeverGetsNearTheObstacle_IsClear()
        {
            Assert.That(_checker.PathCollides(new[] { 0.3f, 0f }, new[] { 1.0f, 0f }, BaseAtOrigin, 0.1f, out string description), Is.False);
            Assert.That(description, Is.Empty);
        }

        [Test]
        public void TheEndPointIsChecked()
        {
            Assert.That(_checker.PathCollides(new[] { 0.3f, 0f }, new[] { 0f, 0f }, BaseAtOrigin, 0.1f, out _), Is.True);
        }

        [Test]
        public void TheStartPointIsNotChecked_SoAnArmAlreadyTouchingSomethingCanStillMoveAway()
        {
            Assert.That(_checker.PathCollides(new[] { 0f, 0f }, new[] { 1.0f, 0f }, BaseAtOrigin, 0.1f, out _), Is.False);
        }

        [Test]
        public void ADegenerateOrMissingPath_IsNotJudged()
        {
            Assert.That(_checker.PathCollides(null, new[] { 0f, 0f }, BaseAtOrigin, 0.1f, out _), Is.False);
            Assert.That(_checker.PathCollides(new[] { 0.5f, 0f }, new[] { 0.5f, 0f }, BaseAtOrigin, 0.1f, out _), Is.False);
        }

        [Test]
        public void ALongPathIsCutIntoManyStepsButNotWithoutLimit()
        {
            _obstacles.Queries = 0;

            _checker.PathCollides(new[] { 0.3f, 0f }, new[] { 3.0f, 0f }, BaseAtOrigin, 0.0001f, out _);

            Assert.That(_obstacles.Queries, Is.LessThanOrEqualTo(40));
        }
    }
}
