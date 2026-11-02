using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PoseRelocatorTests
    {
        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-4f), $"기대 {expected}, 실제 {actual}");
        }

        [Test]
        public void WhenThePlannedBaseIsTheCurrentBase_TheTargetStaysWhereItIs()
        {
            var current = new Pose(new Vector3(1f, 0.5f, 2f), Quaternion.Euler(0f, 30f, 0f));
            var target = new Pose(new Vector3(2f, 1f, 3f), Quaternion.Euler(10f, 40f, 5f));

            Pose relocated = PoseRelocator.Relocate(target, current, current);

            AssertVector(relocated.position, target.position);
            Assert.That(Quaternion.Angle(relocated.rotation, target.rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void WhenThePlannedBaseIsFurtherAhead_TheTargetIsAskedFromTheSameDistanceInFrontOfTheCurrentBase()
        {
            var current = new Pose(Vector3.zero, Quaternion.identity);
            var planned = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            var target = new Pose(new Vector3(0f, 1f, 1.5f), Quaternion.identity);

            Pose relocated = PoseRelocator.Relocate(target, current, planned);

            AssertVector(relocated.position, new Vector3(0f, 1f, 0.5f));
        }

        [Test]
        public void WhenThePlannedBaseIsTurned_TheTargetIsAskedFromTheSameSideOfTheCurrentBase()
        {
            // 예정 베이스가 90도 돌아 서면 월드 +X 쪽 목표는 그 베이스의 정면(+Z) 쪽에 있다.
            var current = new Pose(Vector3.zero, Quaternion.identity);
            var planned = new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));
            var target = new Pose(new Vector3(1f, 0f, 0f), planned.rotation);

            Pose relocated = PoseRelocator.Relocate(target, current, planned);

            AssertVector(relocated.position, new Vector3(0f, 0f, 1f));
            Assert.That(Quaternion.Angle(relocated.rotation, current.rotation), Is.LessThan(1e-3f), "예정 베이스와 같은 방향이면 현재 베이스와도 같은 방향이다");
        }

        [Test]
        public void TheCurrentBaseRotationCarriesTheLocalTargetIntoTheWorld()
        {
            var current = new Pose(new Vector3(5f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            var planned = new Pose(Vector3.zero, Quaternion.identity);
            var target = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);

            Pose relocated = PoseRelocator.Relocate(target, current, planned);

            // 예정 베이스 정면 1m는 90도 돈 현재 베이스에서는 월드 +X 쪽 1m다.
            AssertVector(relocated.position, new Vector3(6f, 0f, 0f));
        }
    }
}
