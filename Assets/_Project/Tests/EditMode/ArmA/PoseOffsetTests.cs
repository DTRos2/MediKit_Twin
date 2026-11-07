using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PoseOffsetTests
    {
        private static void AssertSamePose(Pose actual, Pose expected)
        {
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(1e-4f));
            Assert.That(Quaternion.Angle(actual.rotation, expected.rotation), Is.LessThan(1e-2f));
        }

        [Test]
        public void ApplyingToTheSameParent_GivesTheChildBack()
        {
            var parent = new Pose(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 20f, 30f));
            var child = new Pose(new Vector3(1.2f, 2.1f, 3.4f), Quaternion.Euler(0f, 90f, 0f));

            AssertSamePose(PoseOffset.Between(parent, child).ApplyTo(parent), child);
        }

        [Test]
        public void WhenTheParentMoves_TheChildKeepsItsPlaceRelativeToIt()
        {
            var parent = new Pose(Vector3.zero, Quaternion.identity);
            var child = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            PoseOffset offset = PoseOffset.Between(parent, child);

            var movedParent = new Pose(new Vector3(5f, 0f, 0f), Quaternion.identity);

            AssertSamePose(offset.ApplyTo(movedParent), new Pose(new Vector3(5f, 0f, 1f), Quaternion.identity));
        }

        [Test]
        public void WhenTheParentTurns_TheChildSwingsAroundIt()
        {
            var parent = new Pose(Vector3.zero, Quaternion.identity);
            var child = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            PoseOffset offset = PoseOffset.Between(parent, child);

            // 부모가 위에서 보아 90도 돌면 앞(+Z)에 있던 자식은 옆(+X)으로 간다.
            var turnedParent = new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));

            AssertSamePose(offset.ApplyTo(turnedParent), new Pose(new Vector3(1f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f)));
        }

        [Test]
        public void AChildOnTheParent_StaysOnTheParentWhereverItGoes()
        {
            var parent = new Pose(new Vector3(1f, 1f, 1f), Quaternion.Euler(0f, 0f, 45f));
            PoseOffset offset = PoseOffset.Between(parent, parent);

            var elsewhere = new Pose(new Vector3(-3f, 0.5f, 2f), Quaternion.Euler(30f, 60f, 90f));

            AssertSamePose(offset.ApplyTo(elsewhere), elsewhere);
        }
    }
}
