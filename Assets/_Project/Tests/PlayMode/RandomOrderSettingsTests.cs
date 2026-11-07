using NUnit.Framework;

namespace MediKitTwin.PlayMode.Tests
{
    public sealed class RandomOrderSettingsTests
    {
        [Test]
        public void WithNothingGiven_TheDefaultsApply()
        {
            RandomOrderSettings settings = RandomOrderSettings.Parse(null, null);

            Assert.That(settings.Seed, Is.EqualTo(RandomOrderSettings.DefaultSeed));
            Assert.That(settings.Kits, Is.EqualTo(RandomOrderSettings.DefaultKits));
        }

        [Test]
        public void GivenNumbersAreUsed()
        {
            RandomOrderSettings settings = RandomOrderSettings.Parse("42", "3");

            Assert.That(settings.Seed, Is.EqualTo(42));
            Assert.That(settings.Kits, Is.EqualTo(3));
        }

        [Test]
        public void AnythingThatIsNotANumberFallsBackToTheDefaults()
        {
            RandomOrderSettings settings = RandomOrderSettings.Parse("abc", "");

            Assert.That(settings.Seed, Is.EqualTo(RandomOrderSettings.DefaultSeed));
            Assert.That(settings.Kits, Is.EqualTo(RandomOrderSettings.DefaultKits));
        }

        [Test]
        public void ANegativeSeedIsStillASeed()
        {
            Assert.That(RandomOrderSettings.Parse("-5", null).Seed, Is.EqualTo(-5));
        }

        [Test]
        public void TheKitCountIsKeptWithinOneAndTheMaximum()
        {
            Assert.That(RandomOrderSettings.Parse(null, "0").Kits, Is.EqualTo(1));
            Assert.That(RandomOrderSettings.Parse(null, "-3").Kits, Is.EqualTo(1));
            Assert.That(RandomOrderSettings.Parse(null, "9999").Kits, Is.EqualTo(RandomOrderSettings.MaxKits));
        }
    }
}
