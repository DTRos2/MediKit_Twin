using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class SlipPointsTests
    {
        [Test]
        public void TheFirstOccurrenceKeepsThePlainName()
        {
            Assert.That(SlipPoints.Label(SlipPoints.Stowed, 1), Is.EqualTo(SlipPoints.Stowed));
        }

        [Test]
        public void LaterOccurrencesCarryTheirNumber()
        {
            Assert.That(SlipPoints.Label(SlipPoints.BaseArrived, 2), Is.EqualTo($"{SlipPoints.BaseArrived} 2번째"));
            Assert.That(SlipPoints.Label(SlipPoints.BaseArrived, 3), Is.EqualTo($"{SlipPoints.BaseArrived} 3번째"));
        }

        [Test]
        public void ANonPositiveOccurrenceKeepsThePlainName()
        {
            Assert.That(SlipPoints.Label(SlipPoints.Stowed, 0), Is.EqualTo(SlipPoints.Stowed));
        }

        [Test]
        public void EachArmPhaseHasItsOwnPoint()
        {
            Assert.That(SlipPoints.For(ArmMotionPhase.Stowed), Is.EqualTo(SlipPoints.Stowed));
            Assert.That(SlipPoints.For(ArmMotionPhase.BaseArrived), Is.EqualTo(SlipPoints.BaseArrived));
        }

        [Test]
        public void TheBaseArrivedPointIsNotConfusedWithTheBoxSidePoint()
        {
            // 로그를 읽는 쪽이 "박스 곁 도착"으로 구간 1의 끝을 찾으므로 이름이 겹치면 안 된다.
            Assert.That(SlipPoints.BaseArrived, Is.Not.EqualTo(SlipPoints.BoxSide));
            Assert.That(SlipPoints.BaseArrived, Does.Not.Contain(SlipPoints.BoxSide));
            Assert.That(SlipPoints.BoxSide, Does.Not.Contain(SlipPoints.BaseArrived));
        }
    }
}
