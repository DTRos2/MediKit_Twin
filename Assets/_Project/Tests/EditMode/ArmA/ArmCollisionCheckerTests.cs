using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmCollisionCheckerTests
    {
        private static readonly Pose BaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);
        private static readonly string[] Names = { "link_a", "link_b" };

        private FakeObstacles _obstacles;

        [SetUp]
        public void SetUp()
        {
            _obstacles = new FakeObstacles();
        }

        private ArmCollisionChecker Create(float clearance = 0f, int skipBaseLinks = 0, params LinkVolume[] volumes)
        {
            return new ArmCollisionChecker(
                PerceptionTestData.TwoLinkArm(),
                volumes.Length > 0 ? volumes : new[] { PerceptionTestData.Link1Bar() },
                _obstacles,
                clearance,
                skipBaseLinks,
                Names);
        }

        [Test]
        public void ForwardLinks_PlacesEachLinkWhereTheChainPutsIt()
        {
            KinematicChain chain = PerceptionTestData.TwoLinkArm();
            var poses = new Pose[2];

            chain.ForwardLinks(new[] { Mathf.PI / 2f, 0f }, poses);

            Assert.That(Vector3.Distance(poses[0].position, Vector3.zero), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(poses[1].position, new Vector3(0f, 1f, 0f)), Is.LessThan(1e-5f));
        }

        [Test]
        public void ForwardLinks_AgreesWithTheToolPositionOfTheSameChain()
        {
            KinematicChain chain = PerceptionTestData.TwoLinkArm();
            var angles = new[] { 0.4f, -0.7f };
            var poses = new Pose[2];

            chain.ForwardLinks(angles, poses);
            Pose tool = chain.Forward(angles);

            // 도구 끝점은 마지막 링크에서 +X로 0.5m다.
            Vector3 expected = poses[1].position + poses[1].rotation * new Vector3(0.5f, 0f, 0f);
            Assert.That(Vector3.Distance(tool.position, expected), Is.LessThan(1e-5f));
        }

        [Test]
        public void WithNothingInTheWay_ThereIsNoCollision()
        {
            Assert.That(Create().Collides(new[] { 0f, 0f }, BaseAtOrigin), Is.False);
        }

        [Test]
        public void ALinkBarOverlappingAnObstacle_Collides_AndNamesBothSides()
        {
            // 링크1이 +X로 곧게 뻗으면 막대 중심은 (1.5, 0, 0).
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            bool collides = Create().TryFindCollision(new[] { 0f, 0f }, BaseAtOrigin, out string description);

            Assert.That(collides, Is.True);
            Assert.That(description, Does.Contain("link_b"));
            Assert.That(description, Does.Contain("shelf_board"));
        }

        [Test]
        public void TurningTheArmAway_ClearsTheObstacle()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            // 첫 관절을 90도 돌리면 막대 중심은 (0, 1.5, 0)이라 장애물에서 벗어난다.
            Assert.That(Create().Collides(new[] { Mathf.PI / 2f, 0f }, BaseAtOrigin), Is.False);
        }

        [Test]
        public void TheArmIsPlacedWhereTheBaseStands_NotWhereItStandsNow()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            // 팔 베이스를 Z로 5m 옮기거나 X로 0.5m 옮기면 같은 관절 각도라도 막대가 장애물에서 벗어난다. 장애물이 옮긴 막대 자리에 있으면 겹친다.
            Assert.That(Create().Collides(new[] { 0f, 0f }, new Pose(new Vector3(0f, 0f, 5f), Quaternion.identity)), Is.False);
            Assert.That(Create().Collides(new[] { 0f, 0f }, new Pose(new Vector3(0.5f, 0f, 0f), Quaternion.identity)), Is.False);
            _obstacles.Spheres.Clear();
            _obstacles.Spheres.Add((new Vector3(2f, 0f, 0f), 0.1f, "far_post"));
            Assert.That(Create().Collides(new[] { 0f, 0f }, new Pose(new Vector3(0.5f, 0f, 0f), Quaternion.identity)), Is.True);
        }

        [Test]
        public void TheArmBaseOrientationTurnsTheWholeArm()
        {
            _obstacles.Spheres.Add((new Vector3(0f, 0f, 1.5f), 0.1f, "wall"));
            var faceZ = new Pose(Vector3.zero, Quaternion.Euler(0f, -90f, 0f));

            // 베이스를 Y축으로 -90도 돌리면 로컬 +X가 월드 +Z를 향한다.
            Assert.That(Create().Collides(new[] { 0f, 0f }, faceZ), Is.True);
            Assert.That(Create().Collides(new[] { 0f, 0f }, BaseAtOrigin), Is.False);
        }

        [Test]
        public void SkippedBaseLinks_AreNotChecked()
        {
            _obstacles.Spheres.Add((Vector3.zero, 0.5f, "mount_plate"));

            Assert.That(Create(0f, 0, PerceptionTestData.Link0Block()).Collides(new[] { 0f, 0f }, BaseAtOrigin), Is.True);
            Assert.That(Create(0f, 1, PerceptionTestData.Link0Block()).Collides(new[] { 0f, 0f }, BaseAtOrigin), Is.False);
        }

        [Test]
        public void TheClearanceEnlargesEachBox_AndANegativeOneShrinksIt()
        {
            Create(clearance: 0.05f).Collides(new[] { 0f, 0f }, BaseAtOrigin);
            Vector3 enlarged = _obstacles.LastHalfExtents;
            Create(clearance: -0.05f).Collides(new[] { 0f, 0f }, BaseAtOrigin);
            Vector3 shrunk = _obstacles.LastHalfExtents;

            Assert.That(enlarged.x, Is.EqualTo(0.55f).Within(1e-5f));
            Assert.That(shrunk.x, Is.EqualTo(0.45f).Within(1e-5f));
        }

        [Test]
        public void AClearanceThatWouldInvertTheBox_LeavesATinyBoxInstead()
        {
            Create(clearance: -1f).Collides(new[] { 0f, 0f }, BaseAtOrigin);

            Assert.That(_obstacles.LastHalfExtents.x, Is.GreaterThan(0f));
            Assert.That(_obstacles.LastHalfExtents.y, Is.GreaterThan(0f));
        }

        [Test]
        public void AnAngleListThatIsTooShort_IsNotChecked()
        {
            _obstacles.Spheres.Add((new Vector3(1.5f, 0f, 0f), 0.1f, "shelf_board"));

            Assert.That(Create().Collides(new[] { 0f }, BaseAtOrigin), Is.False);
            Assert.That(_obstacles.Queries, Is.EqualTo(0));
        }
    }
}
