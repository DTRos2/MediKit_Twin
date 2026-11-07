using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BlockerTallyTests
    {
        [Test]
        public void NothingRecorded_DescribesAsEmpty()
        {
            var tally = new BlockerTally();

            Assert.That(tally.KindCount, Is.EqualTo(0));
            Assert.That(tally.Describe(4), Is.Empty);
        }

        [Test]
        public void EmptyDescriptions_AreNotCounted()
        {
            var tally = new BlockerTally();

            tally.Record(string.Empty);
            tally.Record(null);

            Assert.That(tally.KindCount, Is.EqualTo(0));
        }

        [Test]
        public void TheSameBlockerIsCountedTogether_EvenAtDifferentPointsOnThePath()
        {
            var tally = new BlockerTally();

            tally.Record("link_5 ↔ KitBox/Wall(경로 20%)");
            tally.Record("link_5 ↔ KitBox/Wall(경로 80%)");
            tally.Record("link_5 ↔ KitBox/Wall");

            Assert.That(tally.KindCount, Is.EqualTo(1));
            Assert.That(tally.Describe(4), Is.EqualTo("link_5 ↔ KitBox/Wall 3번"));
        }

        [Test]
        public void TheMostFrequentBlockerComesFirst()
        {
            var tally = new BlockerTally();
            tally.Record("link_6 ↔ Jig/Plate");
            tally.Record("link_5 ↔ KitBox/Wall");
            tally.Record("link_5 ↔ KitBox/Wall");

            Assert.That(tally.Describe(4), Is.EqualTo("link_5 ↔ KitBox/Wall 2번, link_6 ↔ Jig/Plate 1번"));
        }

        [Test]
        public void WhenCountsAreEqual_TheOrderFollowsTheName()
        {
            var tally = new BlockerTally();
            tally.Record("b ↔ x");
            tally.Record("a ↔ x");

            Assert.That(tally.Describe(4), Is.EqualTo("a ↔ x 1번, b ↔ x 1번"));
        }

        [Test]
        public void OnlyTheRequestedNumberOfKindsAreShown_AndTheRestAreCounted()
        {
            var tally = new BlockerTally();
            tally.Record("a ↔ x");
            tally.Record("a ↔ x");
            tally.Record("b ↔ x");
            tally.Record("c ↔ x");
            tally.Record("d ↔ x");

            Assert.That(tally.Describe(2), Is.EqualTo("a ↔ x 2번, b ↔ x 1번, 그 밖에 2곳"));
        }
    }
}
