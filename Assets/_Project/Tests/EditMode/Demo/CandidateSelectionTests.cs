using System;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class CandidateSelectionTests
    {
        private static readonly string[] Robots = { "fr5_robot", "ur3_with_gripper" };

        [Test]
        public void WithoutOnly_StartsAtTheFirstCandidate()
        {
            var selection = new CandidateSelection<string>(Robots);

            Assert.That(selection.Count, Is.EqualTo(2));
            Assert.That(selection.Current, Is.EqualTo("fr5_robot"));
            Assert.That(selection.HasChoice, Is.True);
            Assert.That(selection.IsOnlyMissing, Is.False);
        }

        [Test]
        public void SelectNext_MovesOnAndWrapsAround()
        {
            var selection = new CandidateSelection<string>(Robots);

            Assert.That(selection.SelectNext(), Is.EqualTo("ur3_with_gripper"));
            Assert.That(selection.SelectNext(), Is.EqualTo("fr5_robot"));
            Assert.That(selection.Current, Is.EqualTo("fr5_robot"));
        }

        [Test]
        public void WithOnly_KeepsJustThatCandidate()
        {
            var selection = new CandidateSelection<string>(Robots, "ur3_with_gripper");

            Assert.That(selection.Count, Is.EqualTo(1));
            Assert.That(selection.Current, Is.EqualTo("ur3_with_gripper"));
            Assert.That(selection.HasChoice, Is.False);
            Assert.That(selection.IsOnlyMissing, Is.False);
        }

        [Test]
        public void WithOnly_SelectNext_StaysOnThatCandidate()
        {
            var selection = new CandidateSelection<string>(Robots, "fr5_robot");

            Assert.That(selection.SelectNext(), Is.EqualTo("fr5_robot"));
        }

        [Test]
        public void WithOnlyNotInTheList_KeepsEveryCandidateAndReportsIt()
        {
            var selection = new CandidateSelection<string>(Robots, "missing_robot");

            Assert.That(selection.IsOnlyMissing, Is.True);
            Assert.That(selection.Count, Is.EqualTo(2));
            Assert.That(selection.Current, Is.EqualTo("fr5_robot"));
        }

        [Test]
        public void WithoutCandidates_IsEmptyAndHasNoChoice()
        {
            var selection = new CandidateSelection<string>(Array.Empty<string>());

            Assert.That(selection.IsEmpty, Is.True);
            Assert.That(selection.HasChoice, Is.False);
        }

        [Test]
        public void WithoutCandidates_CurrentAndSelectNext_Throw()
        {
            var selection = new CandidateSelection<string>(Array.Empty<string>());

            Assert.Throws<InvalidOperationException>(() => _ = selection.Current);
            Assert.Throws<InvalidOperationException>(() => selection.SelectNext());
        }

        [Test]
        public void NullCandidates_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new CandidateSelection<string>(null));
        }
    }
}
