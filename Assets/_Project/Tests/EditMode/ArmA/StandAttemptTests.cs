using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandAttemptTests
    {
        private static OrientationSearchResult FoundResult()
        {
            var search = new OrientationSearchResult();
            search.Found(Quaternion.identity, 0f, 1);
            return search;
        }

        [Test]
        public void ANewAttempt_IsNeitherStuckNorFound()
        {
            var attempt = new StandAttempt();

            Assert.That(attempt.IsBaseStuck, Is.False);
            Assert.That(attempt.IsFound, Is.False);
        }

        [Test]
        public void AfterASuccessfulSearch_IsFound()
        {
            var attempt = new StandAttempt();

            attempt.RecordSearch(FoundResult());

            Assert.That(attempt.IsFound, Is.True);
            Assert.That(attempt.Search.IsFound, Is.True);
        }

        [Test]
        public void AfterASearchThatFoundNothing_IsNotFound_ButTheBaseWasNotStuck()
        {
            var attempt = new StandAttempt();
            var search = new OrientationSearchResult();
            search.NotFound(75);

            attempt.RecordSearch(search);

            Assert.That(attempt.IsFound, Is.False);
            Assert.That(attempt.IsBaseStuck, Is.False);
            Assert.That(attempt.Search.TriedCount, Is.EqualTo(75));
        }

        [Test]
        public void WhenTheBaseIsStuck_NothingIsFound()
        {
            var attempt = new StandAttempt();

            attempt.MarkBaseStuck();

            Assert.That(attempt.IsBaseStuck, Is.True);
            Assert.That(attempt.IsFound, Is.False);
            Assert.That(attempt.Search.IsFound, Is.False);
        }
    }
}
