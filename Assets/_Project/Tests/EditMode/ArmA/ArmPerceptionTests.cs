using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmPerceptionTests
    {
        private sealed class FixedRefiner : IGraspRefiner
        {
            public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
            {
                return GraspRefinement.Corrected(planned.ShiftedBy(Vector3.up, null), "고침");
            }
        }

        private static GraspTargets Planned() => new GraspTargets(Quaternion.identity, Vector3.zero, Vector3.one, Vector3.right, null);

        private static ItemPickRequest Request() => new ItemPickRequest(PerceptionTestData.Item(Vector3.one), () => true);

        [Test]
        public void BeforeItIsAttached_NothingIsCorrected_AndEveryPoseCountsAsClear()
        {
            var perception = new ArmPerception();

            GraspRefinement refinement = perception.Refine(Request(), Planned());

            Assert.That(perception.IsAttached, Is.False);
            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.IsRejected, Is.False);
            Assert.That(refinement.Targets.Grasp, Is.EqualTo(Vector3.one));
            Assert.That(perception.IsClear(new Pose(Vector3.zero, Quaternion.identity)), Is.True);
            Assert.That(perception.IsClearAtBase(new Pose(Vector3.zero, Quaternion.identity), new Pose(Vector3.zero, Quaternion.identity)), Is.True);
        }

        [Test]
        public void AfterItIsAttached_TheRefinerAndTheClearanceQuestionsAreUsed()
        {
            var perception = new ArmPerception();
            perception.Attach(new FixedRefiner(), pose => pose.position.x > 0f, (target, armBase) => armBase.position.y > 0f);

            Assert.That(perception.IsAttached, Is.True);
            Assert.That(perception.Refine(Request(), Planned()).Targets.Grasp, Is.EqualTo(Vector3.one + Vector3.up));
            Assert.That(perception.IsClear(new Pose(Vector3.right, Quaternion.identity)), Is.True);
            Assert.That(perception.IsClear(new Pose(Vector3.left, Quaternion.identity)), Is.False);
            Assert.That(perception.IsClearAtBase(default, new Pose(Vector3.up, Quaternion.identity)), Is.True);
            Assert.That(perception.IsClearAtBase(default, new Pose(Vector3.down, Quaternion.identity)), Is.False);
        }

        [Test]
        public void AttachingOnlyTheAvoidance_LeavesTheGraspPositionsAlone()
        {
            var perception = new ArmPerception();
            perception.Attach(null, pose => false, null);

            Assert.That(perception.Refine(Request(), Planned()).IsCorrected, Is.False);
            Assert.That(perception.IsClear(new Pose(Vector3.zero, Quaternion.identity)), Is.False);
            Assert.That(perception.IsClearAtBase(default, default), Is.True, "가늠용 판정은 채우지 않았으니 겹치지 않는 것으로 답한다");
        }
    }
}
