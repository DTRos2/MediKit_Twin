using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PhysicalGraspVerdictTests
    {
        [Test]
        public void Succeeds_WhenHeldAndTheFingersStoppedOnTheItem()
        {
            Assert.That(PhysicalGraspVerdict.FailureReason(true, true, true), Is.Empty);
        }

        [Test]
        public void Fails_WhenNothingWasGrabbed()
        {
            Assert.That(PhysicalGraspVerdict.FailureReason(false, true, false), Does.Contain("쥘 물건이 없다"));
            Assert.That(PhysicalGraspVerdict.FailureReason(false, false, false), Does.Contain("쥘 물건이 없다"));
        }

        [Test]
        public void Fails_WhenHeldButTheFingersClosedAllTheWay()
        {
            // 도구 끝점 반경 안에 물건이 있어 붙잡혔지만 손가락이 물건에 닿지 않았다: 손가락 사이에 있는 게 아니다.
            Assert.That(PhysicalGraspVerdict.FailureReason(true, true, false), Does.Contain("끝까지 닫혔다"));
        }

        [Test]
        public void TrustsTheAttachment_WhenContactCannotBeSensed()
        {
            Assert.That(PhysicalGraspVerdict.FailureReason(true, false, false), Is.Empty);
        }
    }
}
