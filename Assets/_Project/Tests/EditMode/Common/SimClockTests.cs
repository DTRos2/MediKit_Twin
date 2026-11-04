using NUnit.Framework;

namespace MediKitTwin.Common.Tests
{
    public sealed class SimClockTests
    {
        [Test]
        public void TheAllowedSpeeds_AreHalfOneAndTwo()
        {
            Assert.That(SimClock.AllowedSpeeds, Is.EqualTo(new[] { 0.5f, 1f, 2f }));
        }

        [TestCase(4f)]
        [TestCase(3f)]
        [TestCase(0f)]
        [TestCase(-1f)]
        public void ASpeedThatIsNotAllowed_IsRefused_AndTheSpeedStaysTheSame(float speed)
        {
            float before = SimClock.Speed;

            bool accepted = SimClock.TrySetSpeed(speed);

            Assert.That(accepted, Is.False);
            Assert.That(SimClock.Speed, Is.EqualTo(before));
        }
    }
}
