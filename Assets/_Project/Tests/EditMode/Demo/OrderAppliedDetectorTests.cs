using MediKitTwin.Orders;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class OrderAppliedDetectorTests
    {
        private OrderAppliedDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _detector = new OrderAppliedDetector();
        }

        // 접수 결과는 주문을 불러올 때마다 새 객체다. 내용이 같아도 다른 객체이면 새로 적용된 것이다.
        private static OrderIntakeResult Intake() => new OrderIntakeResult(null, null, null);

        [Test]
        public void TheFirstValueSeen_OnlySetsTheBaseline()
        {
            Assert.That(_detector.HasChanged(Intake()), Is.False, "패널이 켜지기 전에 적용된 주문은 새로 적용된 것이 아니다");
        }

        [Test]
        public void TheFirstValueSeenBeingNull_ThenAnOrderArrives_IsAChange()
        {
            Assert.That(_detector.HasChanged(null), Is.False);

            Assert.That(_detector.HasChanged(Intake()), Is.True);
        }

        [Test]
        public void TheSameResultAgain_IsNotAChange()
        {
            OrderIntakeResult intake = Intake();
            _detector.HasChanged(intake);

            Assert.That(_detector.HasChanged(intake), Is.False);
            Assert.That(_detector.HasChanged(intake), Is.False);
        }

        [Test]
        public void ANewResultWithTheSameContent_IsAChangeOnce()
        {
            _detector.HasChanged(Intake());

            OrderIntakeResult applied = Intake();
            Assert.That(_detector.HasChanged(applied), Is.True);
            Assert.That(_detector.HasChanged(applied), Is.False, "같은 적용을 두 번 알리지 않는다");
        }

        [Test]
        public void EveryReapply_IsAChange()
        {
            _detector.HasChanged(Intake());

            Assert.That(_detector.HasChanged(Intake()), Is.True);
            Assert.That(_detector.HasChanged(Intake()), Is.True);
        }
    }
}
