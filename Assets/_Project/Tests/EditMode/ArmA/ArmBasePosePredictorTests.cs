using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmBasePosePredictorTests
    {
        private static readonly Pose CurrentBase = new Pose(new Vector3(1f, 0.4f, 2f), Quaternion.Euler(0f, 20f, 0f));

        [Test]
        public void Position_TakesTheTargetOnTheFloor_AndKeepsTheCurrentHeight()
        {
            Pose predicted = ArmBasePosePredictor.Predict(CurrentBase, 20f, new Vector3(3f, 0f, 4f), 20f);

            Assert.That(predicted.position.x, Is.EqualTo(3f).Within(1e-5f));
            Assert.That(predicted.position.y, Is.EqualTo(0.4f).Within(1e-5f));
            Assert.That(predicted.position.z, Is.EqualTo(4f).Within(1e-5f));
        }

        [Test]
        public void WhenTheFacingIsTheCurrentYaw_TheRotationIsUnchanged()
        {
            Pose predicted = ArmBasePosePredictor.Predict(CurrentBase, 20f, Vector3.zero, 20f);

            Assert.That(Quaternion.Angle(predicted.rotation, CurrentBase.rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void WhenTheRootTurns_TheArmBaseTurnsByTheSameAmountAroundTheVerticalAxis()
        {
            var straight = new Pose(Vector3.zero, Quaternion.identity);

            Pose predicted = ArmBasePosePredictor.Predict(straight, 0f, Vector3.zero, 90f);

            Vector3 forward = predicted.rotation * Vector3.forward;
            Assert.That(Vector3.Distance(forward, Vector3.right), Is.LessThan(1e-4f), "정면이 +Z에서 +X로 돈다");
        }

        [Test]
        public void TurnIsMeasuredFromTheCurrentYaw_NotFromZero()
        {
            // 루트가 이미 90도를 보고 있고 180도를 보라고 하면 90도만 더 돈다.
            var current = new Pose(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));

            Pose predicted = ArmBasePosePredictor.Predict(current, 90f, Vector3.zero, 180f);

            Vector3 forward = predicted.rotation * Vector3.forward;
            Assert.That(Vector3.Distance(forward, Vector3.back), Is.LessThan(1e-4f));
        }

        [Test]
        public void TheMountTilt_IsKeptWhenTheRootTurns()
        {
            // 팔 베이스가 루트에 기울어 붙어 있어도 수평으로만 돌고 기울기는 유지된다.
            var tilted = new Pose(Vector3.zero, Quaternion.Euler(10f, 0f, 0f));

            Pose predicted = ArmBasePosePredictor.Predict(tilted, 0f, Vector3.zero, 90f);

            float tilt = Vector3.Angle(predicted.rotation * Vector3.up, Vector3.up);
            Assert.That(tilt, Is.EqualTo(10f).Within(1e-3f));
        }
    }
}
