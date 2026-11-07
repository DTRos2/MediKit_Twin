using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmB.Tests
{
    public class LidCoverPlannerTests
    {
        private static readonly Vector3 LongAxis = new Vector3(-1f, 0f, -0.01f);

        [Test]
        public void ToolRotation_PointsToolForwardDown()
        {
            Quaternion rotation = LidCoverPlanner.ToolRotation(LongAxis);

            Assert.That(Vector3.Dot(rotation * Vector3.forward, Vector3.down), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void ToolRotation_AlignsToolYWithLongAxisSoFingersCloseOnShortSides()
        {
            Quaternion rotation = LidCoverPlanner.ToolRotation(LongAxis);

            Assert.That(Vector3.Dot(rotation * Vector3.up, LongAxis.normalized), Is.EqualTo(1f).Within(1e-4f));
            // 손가락이 닫히는 도구 X는 긴 변과 수직이다.
            Assert.That(Vector3.Dot(rotation * Vector3.right, LongAxis.normalized), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void GroundDirection_DropsVerticalComponent()
        {
            Vector3 direction = LidCoverPlanner.GroundDirection(new Vector3(-1f, 0.5f, 0f));

            Assert.That(direction.y, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Vector3.Dot(direction, Vector3.left), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void GroundDirection_NearlyVertical_FallsBackToWorldForward()
        {
            Assert.That(LidCoverPlanner.GroundDirection(Vector3.up), Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void CandidateAxes_PutsPreferredDirectionFirst()
        {
            Vector3[] candidates = LidCoverPlanner.CandidateAxes(Vector3.forward, Vector3.back);

            Assert.That(candidates.Length, Is.EqualTo(2));
            Assert.That(Vector3.Dot(candidates[0], Vector3.back), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Vector3.Dot(candidates[1], Vector3.forward), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void CandidateAxes_SameSideAsPreferred_KeepsOriginalFirst()
        {
            Vector3[] candidates = LidCoverPlanner.CandidateAxes(Vector3.forward, new Vector3(0.1f, 0f, 1f));

            Assert.That(Vector3.Dot(candidates[0], Vector3.forward), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void CoverHeight_PlacesLidTopPlateOnBoxTop()
        {
            // 박스 윗면 0.96, 뚜껑 높이 0.04, 윗판 두께 0.01 → 윗판 윗면이 0.97이고 중심은 그보다 0.02 아래.
            Assert.That(LidCoverPlanner.CoverHeight(0.96f, 0.04f, 0.01f), Is.EqualTo(0.95f).Within(1e-5f));
        }

        [Test]
        public void YawErrorDeg_RotationAboutVertical_ReturnsThatAngleAndNoTilt()
        {
            Quaternion target = LidCoverPlanner.ToolRotation(Vector3.right);
            Quaternion actual = Quaternion.AngleAxis(3f, Vector3.up) * target;

            Assert.That(LidCoverPlanner.YawErrorDeg(actual, target), Is.EqualTo(3f).Within(0.01f));
            Assert.That(LidCoverPlanner.TiltErrorDeg(actual, target), Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void TiltErrorDeg_TiltAboutHorizontalAxis_ReturnsThatAngleAndNoYaw()
        {
            // 도구 Y가 월드 X를 향할 때 월드 Z축으로 기울이면 접근 방향만 벌어지고 수평 방향은 그대로다.
            Quaternion target = LidCoverPlanner.ToolRotation(Vector3.right);
            Quaternion actual = Quaternion.AngleAxis(4f, Vector3.forward) * target;

            Assert.That(LidCoverPlanner.TiltErrorDeg(actual, target), Is.EqualTo(4f).Within(0.01f));
            Assert.That(LidCoverPlanner.YawErrorDeg(actual, target), Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void YawErrorDeg_IdenticalRotations_ReturnsZero()
        {
            Quaternion rotation = LidCoverPlanner.ToolRotation(new Vector3(-1f, 0f, -0.01f));

            Assert.That(LidCoverPlanner.YawErrorDeg(rotation, rotation), Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Above_RaisesOnlyThePosition()
        {
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);

            Pose pose = LidCoverPlanner.Above(new Vector3(1f, 2f, 3f), 0.1f, rotation);

            Assert.That(pose.position, Is.EqualTo(new Vector3(1f, 2.1f, 3f)));
            Assert.That(pose.rotation, Is.EqualTo(rotation));
        }

        [Test]
        public void IsSeated_WithinClearance_ReturnsTrue()
        {
            Assert.That(LidCoverPlanner.IsSeated(new Vector3(1.004f, 0.951f, 2.003f), new Vector3(1f, 0.9f, 2f), 0.95f, 0.008f), Is.True);
        }

        [Test]
        public void IsSeated_OffsetHorizontally_ReturnsFalse()
        {
            Assert.That(LidCoverPlanner.IsSeated(new Vector3(1.02f, 0.95f, 2f), new Vector3(1f, 0.9f, 2f), 0.95f, 0.008f), Is.False);
        }

        [Test]
        public void IsSeated_WrongHeight_ReturnsFalse()
        {
            Assert.That(LidCoverPlanner.IsSeated(new Vector3(1f, 1.01f, 2f), new Vector3(1f, 0.9f, 2f), 0.95f, 0.008f), Is.False);
        }
    }
}
