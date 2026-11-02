using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BaseClearanceCheckerTests
    {
        private FakeObstacles _obstacles;

        [SetUp]
        public void SetUp()
        {
            _obstacles = new FakeObstacles();
        }

        // 베이스 링크 기준 중심 (0, 0.05, 0)에 놓인 상자.
        private static LinkVolume BaseBox() => new LinkVolume(0, new Vector3(0f, 0.05f, 0f), Quaternion.identity, new Vector3(0.075f, 0.05f, 0.075f));

        private BaseClearanceChecker Create(float margin = 0f) => new BaseClearanceChecker(new[] { BaseBox() }, _obstacles, margin);

        [Test]
        public void WithNothingInTheWay_TheBaseIsClear()
        {
            bool collides = Create().TryFindCollision(new Pose(Vector3.zero, Quaternion.identity), out string description);

            Assert.That(collides, Is.False);
            Assert.That(description, Is.Empty);
        }

        [Test]
        public void AnObstacleAtTheBoxCenter_Collides_AndNamesTheObstacle()
        {
            _obstacles.Spheres.Add((new Vector3(2f, 0.05f, 3f), 0.05f, "rail"));

            bool collides = Create().TryFindCollision(new Pose(new Vector3(2f, 0f, 3f), Quaternion.identity), out string description);

            Assert.That(collides, Is.True);
            Assert.That(description, Is.EqualTo("팔 베이스 ↔ rail"));
        }

        [Test]
        public void TheBoxFollowsTheBasePose_NotTheOrigin()
        {
            _obstacles.Spheres.Add((new Vector3(0f, 0.05f, 0f), 0.05f, "rail"));

            // 베이스가 다른 곳에 서 있으면 원점의 장애물과는 겹치지 않는다.
            bool collides = Create().TryFindCollision(new Pose(new Vector3(5f, 0f, 5f), Quaternion.identity), out _);

            Assert.That(collides, Is.False);
        }

        [Test]
        public void TheBoxTurnsWithTheBase()
        {
            var offCenter = new LinkVolume(0, new Vector3(0.1f, 0.05f, 0f), Quaternion.identity, new Vector3(0.02f, 0.05f, 0.02f));
            var checker = new BaseClearanceChecker(new[] { offCenter }, _obstacles, 0f);
            _obstacles.Spheres.Add((new Vector3(0f, 0.05f, -0.1f), 0.01f, "rail"));

            // 상자는 베이스 +X쪽 0.1m에 있다. 베이스가 +Y축으로 90도 돌면 그 상자는 월드 -Z쪽 0.1m로 간다.
            bool collides = checker.TryFindCollision(new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f)), out _);

            Assert.That(collides, Is.True);
        }

        [Test]
        public void TheMarginGrowsTheQueriedBox()
        {
            Create(0.02f).TryFindCollision(new Pose(Vector3.zero, Quaternion.identity), out _);

            Assert.That(_obstacles.LastHalfExtents.x, Is.EqualTo(0.095f).Within(1e-5f));
            Assert.That(_obstacles.LastHalfExtents.y, Is.EqualTo(0.07f).Within(1e-5f));
        }

        [Test]
        public void WithoutAnyBoxes_NothingCanCollide()
        {
            var checker = new BaseClearanceChecker(new LinkVolume[0], _obstacles, 0.01f);
            _obstacles.Spheres.Add((Vector3.zero, 10f, "everywhere"));

            Assert.That(checker.TryFindCollision(new Pose(Vector3.zero, Quaternion.identity), out _), Is.False);
            Assert.That(_obstacles.Queries, Is.EqualTo(0));
        }
    }
}
