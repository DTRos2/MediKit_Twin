using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ToolCameraPlacementTests
    {
        // 도구 끝점 좌표로 잰 몸통: 옆으로 −3~+3cm, 위아래 −5~+5cm, 앞뒤 −12~−2cm(도구 끝점보다 뒤).
        private static readonly Bounds Body = new Bounds(new Vector3(0f, 0f, -0.07f), new Vector3(0.06f, 0.10f, 0.10f));

        private static ToolCameraSettings SettingsFromJson(string json) => JsonUtility.FromJson<ToolCameraSettings>(json);

        [Test]
        public void RightSide_SitsOnThePositiveXFace_JustOutsideIt()
        {
            Pose pose = ToolCameraPlacement.OnHandSide(Body, true, 0.005f, 0.9f, 0.15f, Vector3.zero);

            Assert.That(pose.position.x, Is.EqualTo(0.035f).Within(1e-5f));
        }

        [Test]
        public void LeftSide_SitsOnTheNegativeXFace_JustOutsideIt()
        {
            Pose pose = ToolCameraPlacement.OnHandSide(Body, false, 0.005f, 0.9f, 0.15f, Vector3.zero);

            Assert.That(pose.position.x, Is.EqualTo(-0.035f).Within(1e-5f));
        }

        [Test]
        public void SitsAtTheMiddleHeightOfTheBody_AndTowardTheFrontEndByTheFraction()
        {
            Pose pose = ToolCameraPlacement.OnHandSide(Body, true, 0f, 0.9f, 0.15f, Vector3.zero);

            Assert.That(pose.position.y, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(pose.position.z, Is.EqualTo(-0.12f + 0.1f * 0.9f).Within(1e-5f));
        }

        [Test]
        public void FractionIsClampedToTheBodysLength()
        {
            Pose behind = ToolCameraPlacement.OnHandSide(Body, true, 0f, -3f, 0.15f, Vector3.zero);
            Pose ahead = ToolCameraPlacement.OnHandSide(Body, true, 0f, 7f, 0.15f, Vector3.zero);

            Assert.That(behind.position.z, Is.EqualTo(-0.12f).Within(1e-5f));
            Assert.That(ahead.position.z, Is.EqualTo(-0.02f).Within(1e-5f));
        }

        [Test]
        public void LooksTowardTheToolAxisAhead_SoTheCameraOnTheRightTurnsLeft()
        {
            Pose pose = ToolCameraPlacement.OnHandSide(Body, true, 0f, 0.9f, 0.15f, Vector3.zero);

            Vector3 forward = pose.rotation * Vector3.forward;
            Vector3 toTarget = (new Vector3(0f, 0f, 0.15f) - pose.position).normalized;

            Assert.That(Vector3.Dot(forward, toTarget), Is.GreaterThan(0.9999f));
            Assert.That(forward.x, Is.LessThan(0f), "오른쪽 옆면에 달았으면 가운데(왼쪽)로 틀어 본다");
            Assert.That(forward.z, Is.GreaterThan(0f), "물건 쪽(앞)을 본다");
        }

        [Test]
        public void LeftSideLooksTowardThePositiveX()
        {
            Pose pose = ToolCameraPlacement.OnHandSide(Body, false, 0f, 0.9f, 0.15f, Vector3.zero);

            Assert.That((pose.rotation * Vector3.forward).x, Is.GreaterThan(0f));
        }

        [Test]
        public void Nudge_MovesTheCamera_ThenItStillAimsAtTheSamePoint()
        {
            var nudge = new Vector3(0f, 0.02f, 0f);

            Pose plain = ToolCameraPlacement.OnHandSide(Body, true, 0f, 0.9f, 0.15f, Vector3.zero);
            Pose nudged = ToolCameraPlacement.OnHandSide(Body, true, 0f, 0.9f, 0.15f, nudge);

            Assert.That(nudged.position, Is.EqualTo(plain.position + nudge));
            Vector3 toTarget = (new Vector3(0f, 0f, 0.15f) - nudged.position).normalized;
            Assert.That(Vector3.Dot(nudged.rotation * Vector3.forward, toTarget), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void Resolve_UsesTheHandCenter_ByDefault_WhenTheBodyWasMeasured()
        {
            var settings = new ToolCameraSettings();

            Pose pose = ToolCameraPlacement.Resolve(settings, true, Body);

            Assert.That(settings.Mount, Is.EqualTo(ToolCameraMount.HandCenter));
            Assert.That(pose.position.x, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(pose.position.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void HandCenter_SitsAtTheMiddleOfTheBodySides_AtTheFrontEnd_LookingStraightAhead()
        {
            Pose pose = ToolCameraPlacement.AtHandCenter(Body, 1f, Vector3.zero);

            Assert.That(Vector3.Distance(pose.position, new Vector3(0f, 0f, -0.02f)), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(pose.rotation, Quaternion.identity), Is.LessThan(1e-3f), "도구 끝점과 같은 쪽을 똑바로 본다");
        }

        [Test]
        public void HandCenter_PitchUp_TiltsTheViewTowardPlusY()
        {
            Pose pose = ToolCameraPlacement.AtHandCenter(Body, 1f, Vector3.zero, 20f);

            Vector3 forward = pose.rotation * Vector3.forward;
            Assert.That(forward.y, Is.EqualTo(Mathf.Sin(20f * Mathf.Deg2Rad)).Within(1e-4f), "위로 20도 든다");
            Assert.That(forward.x, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void HandCenter_TheFractionMovesItAlongTheBody_AndNudgeMovesItFurther()
        {
            Pose half = ToolCameraPlacement.AtHandCenter(Body, 0.5f, Vector3.zero);
            Pose nudged = ToolCameraPlacement.AtHandCenter(Body, 0.5f, new Vector3(0.01f, 0.02f, 0.03f));

            Assert.That(half.position.z, Is.EqualTo(-0.07f).Within(1e-5f));
            Assert.That(Vector3.Distance(nudged.position - half.position, new Vector3(0.01f, 0.02f, 0.03f)), Is.LessThan(1e-5f));
        }

        [Test]
        public void Resolve_HandSide_StillWorks_WhenChosen()
        {
            ToolCameraSettings settings = SettingsFromJson("{\"_mount\":0}");

            Pose pose = ToolCameraPlacement.Resolve(settings, true, Body);

            Assert.That(settings.Mount, Is.EqualTo(ToolCameraMount.HandRightSide));
            Assert.That(pose.position.x, Is.GreaterThan(0.03f));
        }

        [Test]
        public void Resolve_FallsBackToTheConfiguredPose_WhenTheBodyWasNotMeasured()
        {
            var settings = new ToolCameraSettings();

            Pose pose = ToolCameraPlacement.Resolve(settings, false, default);

            Assert.That(pose.position, Is.EqualTo(settings.LocalPosition));
            Assert.That(pose.rotation, Is.EqualTo(settings.LocalRotation));
        }

        [Test]
        public void Resolve_Manual_IgnoresTheBody()
        {
            ToolCameraSettings settings = SettingsFromJson("{\"_mount\":2,\"_localPosition\":{\"x\":1.0,\"y\":2.0,\"z\":3.0}}");

            Pose pose = ToolCameraPlacement.Resolve(settings, true, Body);

            Assert.That(settings.Mount, Is.EqualTo(ToolCameraMount.Manual));
            Assert.That(pose.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void Resolve_LeftSide_PutsTheCameraOnTheOtherFace()
        {
            ToolCameraSettings settings = SettingsFromJson("{\"_mount\":1}");

            Pose pose = ToolCameraPlacement.Resolve(settings, true, Body);

            Assert.That(pose.position.x, Is.LessThan(-0.03f));
        }
    }
}
