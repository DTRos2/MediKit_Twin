using NUnit.Framework;

namespace MediKitTwin.Conveyor.Tests
{
    public sealed class ConveyorSpeedRangeTests
    {
        [Test]
        public void Clamp_KeepsASpeedInsideTheRange()
        {
            var range = new ConveyorSpeedRange(0.2f, 2f);

            Assert.That(range.Clamp(0.8f), Is.EqualTo(0.8f));
        }

        [TestCase(0.1f, 0.2f)]
        [TestCase(0f, 0.2f)]
        [TestCase(-3f, 0.2f)]
        [TestCase(2.5f, 2f)]
        [TestCase(100f, 2f)]
        public void Clamp_PullsAnOutOfRangeSpeedToTheNearestEnd(float given, float expected)
        {
            var range = new ConveyorSpeedRange(0.2f, 2f);

            Assert.That(range.Clamp(given), Is.EqualTo(expected));
        }

        [Test]
        public void Clamp_TreatsNotANumberAsTheSlowestSpeed_NeverAsZero()
        {
            var range = new ConveyorSpeedRange(0.2f, 2f);

            Assert.That(range.Clamp(float.NaN), Is.EqualTo(0.2f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void AMinimumThatWouldStopTheBoxes_IsRaisedToTheSmallestAllowed(float minimum)
        {
            var range = new ConveyorSpeedRange(minimum, 2f);

            Assert.That(range.Min, Is.EqualTo(ConveyorSpeedRange.SmallestMinMps));
            Assert.That(range.Clamp(0f), Is.GreaterThan(0f));
        }

        [Test]
        public void AMaximumBelowTheMinimum_BecomesTheMinimum_SoTheRangeIsNeverEmptyOrInverted()
        {
            var range = new ConveyorSpeedRange(1f, 0.5f);

            Assert.That(range.Max, Is.EqualTo(range.Min));
            Assert.That(range.Clamp(3f), Is.EqualTo(1f));
        }
    }
}
