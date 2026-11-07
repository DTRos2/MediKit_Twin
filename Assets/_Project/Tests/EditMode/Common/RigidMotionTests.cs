using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Common.Tests
{
    public sealed class RigidMotionTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void ThePoseItself_IsMovedOntoThePose_ItWasSentTo()
        {
            var fromPosition = new Vector3(1f, 2f, 3f);
            Quaternion fromRotation = Quaternion.Euler(0f, 30f, 0f);
            var toPosition = new Vector3(-2f, 0.5f, 4f);
            Quaternion toRotation = Quaternion.Euler(10f, 80f, -5f);

            Matrix4x4 motion = RigidMotion.Between(fromPosition, fromRotation, toPosition, toRotation);

            Assert.That(Vector3.Distance(motion.MultiplyPoint3x4(fromPosition), toPosition), Is.LessThan(Tolerance));
            Assert.That(Quaternion.Angle(motion.rotation * fromRotation, toRotation), Is.LessThan(Tolerance));
        }

        [Test]
        public void ThePureShift_MovesAnyPointByTheSameShift()
        {
            Matrix4x4 motion = RigidMotion.Between(Vector3.zero, Quaternion.identity, new Vector3(0.5f, 0f, -1f), Quaternion.identity);

            Vector3 moved = motion.MultiplyPoint3x4(new Vector3(2f, 3f, 4f));

            Assert.That(Vector3.Distance(moved, new Vector3(2.5f, 3f, 3f)), Is.LessThan(Tolerance));
        }

        // 손에 쥔 물건은 손에서 얼마나 떨어져 있든, 손이 돌면 손을 중심으로 같이 돌아야 한다.
        [Test]
        public void ThePointHeldAwayFromThePose_TurnsAroundThePose_WhenThePoseTurns()
        {
            var hand = new Vector3(1f, 0f, 0f);
            Matrix4x4 motion = RigidMotion.Between(hand, Quaternion.identity, hand, Quaternion.Euler(0f, 90f, 0f));
            var held = new Vector3(1f, 0f, 0.2f);

            Vector3 moved = motion.MultiplyPoint3x4(held);

            // 손(1,0,0)에서 +Z로 0.2 떨어진 점은 Y축으로 90도 돌면 +X로 0.2 떨어진 점이 된다.
            Assert.That(Vector3.Distance(moved, new Vector3(1.2f, 0f, 0f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void NoMovement_IsTheIdentity()
        {
            Matrix4x4 motion = RigidMotion.Between(new Vector3(1f, 2f, 3f), Quaternion.Euler(5f, 6f, 7f), new Vector3(1f, 2f, 3f), Quaternion.Euler(5f, 6f, 7f));

            Assert.That(Vector3.Distance(motion.MultiplyPoint3x4(new Vector3(9f, 8f, 7f)), new Vector3(9f, 8f, 7f)), Is.LessThan(Tolerance));
        }
    }
}
