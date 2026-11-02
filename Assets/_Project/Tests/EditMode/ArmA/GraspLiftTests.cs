using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspLiftTests
    {
        [Test]
        public void AThinItem_IsAimedHigherByWhatTheFingersDropBelowTheTool()
        {
            // 손가락이 2.5cm 내려오고 물건 높이가 1.8cm(절반 0.9cm)이면 중심에서 1.6cm 높게 겨냥한다.
            Assert.That(GraspLift.Compute(0.025f, 0.009f), Is.EqualTo(0.016f).Within(1e-6f));
        }

        [Test]
        public void ATallItem_IsAimedAtItsCentre()
        {
            Assert.That(GraspLift.Compute(0.025f, 0.05f), Is.EqualTo(0f));
        }

        [Test]
        public void WithoutAFingerDrop_NothingIsLifted()
        {
            Assert.That(GraspLift.Compute(0f, 0.009f), Is.EqualTo(0f));
        }

        [Test]
        public void AnUnknownHeight_LiftsByTheWholeFingerDrop()
        {
            Assert.That(GraspLift.Compute(0.025f, 0f), Is.EqualTo(0.025f).Within(1e-6f));
        }

        [Test]
        public void ANegativeHeight_IsTreatedAsUnknown()
        {
            Assert.That(GraspLift.Compute(0.025f, -1f), Is.EqualTo(0.025f).Within(1e-6f));
        }
    }
}
