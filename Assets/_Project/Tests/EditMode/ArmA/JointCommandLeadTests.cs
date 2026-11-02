using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class JointCommandLeadTests
    {
        private const float MaxStep = 0.02f;
        private const float MaxLead = 0.1f;

        [Test]
        public void WhenTheJointFollows_TheCommandMovesTowardTheGoalByTheFullStep()
        {
            float command = JointCommandLead.Advance(0.5f, 1.5f, 0.49f, MaxStep, MaxLead);

            Assert.That(command, Is.EqualTo(0.52f).Within(1e-5f));
        }

        [Test]
        public void WhenTheJointIsBlocked_TheCommandStopsAtTheLeadLimit_NotAtTheGoal()
        {
            // 관절이 0.5에서 막혀 움직이지 않는다. 명령은 목표(1.5)가 아니라 실제보다 0.1 앞에서 멈춘다.
            float command = 0.5f;
            for (int i = 0; i < 1000; i++)
            {
                command = JointCommandLead.Advance(command, 1.5f, 0.5f, MaxStep, MaxLead);
            }

            Assert.That(command, Is.EqualTo(0.6f).Within(1e-5f));
        }

        [Test]
        public void OnceTheBlockIsGone_TheCommandResumesFromTheLeadLimit_WithoutAJump()
        {
            float blocked = 0.5f;
            float command = 0.5f;
            for (int i = 0; i < 1000; i++)
            {
                command = JointCommandLead.Advance(command, 1.5f, blocked, MaxStep, MaxLead);
            }

            // 막힘이 풀려 관절이 명령 위치까지 따라온 뒤 한 번 더 옮기면, 한 걸음만 나아가고 목표로 튀지 않는다.
            float freed = JointCommandLead.Advance(command, 1.5f, command, MaxStep, MaxLead);

            Assert.That(freed - command, Is.EqualTo(MaxStep).Within(1e-5f));
            Assert.That(command, Is.LessThan(1.5f), "막혀 있는 동안 목표까지 가 있지 않았다");
        }

        [Test]
        public void TheLeadLimitAppliesInBothDirections()
        {
            float command = 0.5f;
            for (int i = 0; i < 1000; i++)
            {
                command = JointCommandLead.Advance(command, -1.5f, 0.5f, MaxStep, MaxLead);
            }

            Assert.That(command, Is.EqualTo(0.4f).Within(1e-5f));
        }

        [Test]
        public void ACommandThatIsAlreadyFarAhead_IsPulledBackToTheLeadLimit()
        {
            float command = JointCommandLead.Advance(2f, 3f, 0.5f, MaxStep, MaxLead);

            Assert.That(command, Is.EqualTo(0.6f).Within(1e-5f));
        }

        [Test]
        public void WhenTheGoalIsWithinTheLead_TheCommandReachesTheGoal()
        {
            float command = 0.5f;
            for (int i = 0; i < 10; i++)
            {
                command = JointCommandLead.Advance(command, 0.55f, 0.5f, MaxStep, MaxLead);
            }

            Assert.That(command, Is.EqualTo(0.55f).Within(1e-5f));
        }
    }
}
