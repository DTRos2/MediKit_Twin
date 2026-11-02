using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtPoseAnchorTests
    {
        private static readonly Vector3 TaughtGrasp = new Vector3(2f, 0.7f, 2.1f);

        // 칸 밖으로 나가는 방향은 월드 -Z. 팔 베이스는 알려 준 쥐는 자리에서 0.7m 물러서 있다.
        private static TaughtPickPlan Plan() => TaughtPickPlan.For(
            TaughtPickPose.Capture("A-1", new Vector3(2f, 0f, 1.4f), 0f, new Vector3(2f, 0.5f, 1.4f), new Pose(TaughtGrasp, Quaternion.identity), new[] { 0f, 0f }),
            Vector3.back, 0.3f, 0.15f, 0.2f);

        [Test]
        public void AnItemDeeperInTheShelf_MovesTheStandAndTheToolPathByTheSameAmount()
        {
            var anchor = new TaughtPoseAnchor(0.6f);
            TaughtPickPlan original = Plan();

            TaughtAnchorResult result = anchor.Anchor(original, "A-1", TaughtGrasp + new Vector3(0f, 0f, 0.26f));

            Assert.That(result.IsUsable, Is.True);
            Assert.That(result.ShiftM, Is.EqualTo(0.26f).Within(1e-5f));
            Assert.That(result.Plan.ArmBaseStand.z, Is.EqualTo(original.ArmBaseStand.z + 0.26f).Within(1e-5f));
            Assert.That(result.Plan.GraspPosition.z, Is.EqualTo(2.36f).Within(1e-5f));
            Assert.That(result.Plan.NearPosition.z, Is.EqualTo(original.NearPosition.z + 0.26f).Within(1e-5f));
            Assert.That(result.Plan.RetreatPosition.z, Is.EqualTo(original.RetreatPosition.z + 0.26f).Within(1e-5f));
            Assert.That(result.Plan.ArmBaseWaypoint.Value.z, Is.EqualTo(original.ArmBaseWaypoint.Value.z + 0.26f).Within(1e-5f));
        }

        [Test]
        public void TheStandKeepsItsDistanceToTheGrasp_SoTheTaughtArmShapeStillReaches()
        {
            var anchor = new TaughtPoseAnchor(0.6f);
            TaughtPickPlan original = Plan();
            float taughtDistance = Vector3.Distance(
                new Vector3(original.ArmBaseStand.x, 0f, original.ArmBaseStand.z), new Vector3(original.GraspPosition.x, 0f, original.GraspPosition.z));

            TaughtPickPlan shifted = anchor.Anchor(original, "A-1", TaughtGrasp + new Vector3(0.1f, 0f, 0.2f)).Plan;

            float distance = Vector3.Distance(
                new Vector3(shifted.ArmBaseStand.x, 0f, shifted.ArmBaseStand.z), new Vector3(shifted.GraspPosition.x, 0f, shifted.GraspPosition.z));
            Assert.That(distance, Is.EqualTo(taughtDistance).Within(1e-5f));
        }

        [Test]
        public void TheHeightIsLeftAlone_BecauseTheBaseCannotChangeIt()
        {
            var anchor = new TaughtPoseAnchor(0.6f);
            TaughtPickPlan original = Plan();

            TaughtPickPlan shifted = anchor.Anchor(original, "A-1", TaughtGrasp + new Vector3(0f, -0.11f, 0.1f)).Plan;

            Assert.That(shifted.GraspPosition.y, Is.EqualTo(original.GraspPosition.y));
            Assert.That(shifted.NearPosition.y, Is.EqualTo(original.NearPosition.y));
            Assert.That(shifted.ArmBaseStand.y, Is.EqualTo(0f));
        }

        [Test]
        public void TheToolRotationAndTheYaw_AreNotChanged()
        {
            var anchor = new TaughtPoseAnchor(0.6f);
            TaughtPickPlan original = Plan();

            TaughtPickPlan shifted = anchor.Anchor(original, "A-1", TaughtGrasp + new Vector3(0.3f, 0f, 0.1f)).Plan;

            Assert.That(shifted.ToolRotation, Is.EqualTo(original.ToolRotation));
            Assert.That(shifted.RootYawDeg, Is.EqualTo(original.RootYawDeg));
        }

        [Test]
        public void AnItemBeyondTheLimit_IsUnusable_AndTheDetailNamesTheSlotAndTheDistances()
        {
            var anchor = new TaughtPoseAnchor(0.3f);

            TaughtAnchorResult result = anchor.Anchor(Plan(), "A-1", TaughtGrasp + new Vector3(0f, 0f, 0.5f));

            Assert.That(result.IsUsable, Is.False);
            Assert.That(result.Detail, Does.Contain("A-1"));
            Assert.That(result.Detail, Does.Contain("50.0cm"));
            Assert.That(result.Detail, Does.Contain("30.0cm"));
            Assert.That(result.Plan.GraspPosition, Is.EqualTo(TaughtGrasp), "쓰지 못할 때는 원래 계획 그대로다");
        }

        [Test]
        public void AnItemRightAtTheTaughtGrasp_ShiftsNothing()
        {
            TaughtAnchorResult result = new TaughtPoseAnchor(0.6f).Anchor(Plan(), "A-1", TaughtGrasp);

            Assert.That(result.IsUsable, Is.True);
            Assert.That(result.ShiftM, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(result.Plan.ArmBaseStand, Is.EqualTo(Plan().ArmBaseStand));
        }

        [Test]
        public void WithoutALimit_TheSavedValuesAreUsedAsTheyAre()
        {
            TaughtAnchorResult result = new TaughtPoseAnchor(0f).Anchor(Plan(), "A-1", TaughtGrasp + new Vector3(5f, 5f, 5f));

            Assert.That(result.IsUsable, Is.True);
            Assert.That(result.ShiftM, Is.EqualTo(0f));
            Assert.That(result.Plan.GraspPosition, Is.EqualTo(TaughtGrasp));
        }

        [Test]
        public void TheDefaultStruct_UsesTheSavedValuesAsTheyAre()
        {
            TaughtAnchorResult result = default(TaughtPoseAnchor).Anchor(Plan(), "A-1", TaughtGrasp + Vector3.one);

            Assert.That(result.IsUsable, Is.True);
            Assert.That(result.Plan.GraspPosition, Is.EqualTo(TaughtGrasp));
        }
    }
}
