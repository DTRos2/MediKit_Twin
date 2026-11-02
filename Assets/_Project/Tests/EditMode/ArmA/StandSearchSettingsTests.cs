using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandSearchSettingsTests
    {
        [Test]
        public void KeepsTheGivenValues()
        {
            var settings = new StandSearchSettings(true, 4);

            Assert.That(settings.PlanInAdvance, Is.True);
            Assert.That(settings.MaxStandAttempts, Is.EqualTo(4));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void TriesAtLeastOnePlace(int requested)
        {
            Assert.That(new StandSearchSettings(false, requested).MaxStandAttempts, Is.EqualTo(1));
        }
    }
}
