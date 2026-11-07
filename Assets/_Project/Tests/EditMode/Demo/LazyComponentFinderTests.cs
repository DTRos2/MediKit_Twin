using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class LazyComponentFinderTests
    {
        private sealed class Thing
        {
        }

        private int _findCalls;
        private Thing _inScene;

        private Thing Find()
        {
            _findCalls++;
            return _inScene;
        }

        [SetUp]
        public void SetUp()
        {
            _findCalls = 0;
            _inScene = null;
        }

        [Test]
        public void APresetIsUsedAtOnce_WithoutSearchingTheScene()
        {
            var preset = new Thing();
            var finder = new LazyComponentFinder<Thing>(preset, 1f, Find);

            Assert.That(finder.Refresh(0f), Is.True);
            Assert.That(finder.Found, Is.SameAs(preset));
            Assert.That(_findCalls, Is.EqualTo(0));
        }

        [Test]
        public void WhileNothingIsFound_ItSearchesOnlyOncePerInterval()
        {
            var finder = new LazyComponentFinder<Thing>(null, 1f, Find);

            Assert.That(finder.Refresh(0f), Is.False);
            Assert.That(finder.Refresh(0.5f), Is.False);
            Assert.That(_findCalls, Is.EqualTo(1));

            Assert.That(finder.Refresh(1.1f), Is.False);
            Assert.That(_findCalls, Is.EqualTo(2));
        }

        [Test]
        public void OnceFound_ItStopsSearching()
        {
            var finder = new LazyComponentFinder<Thing>(null, 1f, Find);
            finder.Refresh(0f);
            _inScene = new Thing();

            Assert.That(finder.Refresh(1.1f), Is.True);
            Assert.That(finder.Found, Is.SameAs(_inScene));

            finder.Refresh(5f);
            Assert.That(_findCalls, Is.EqualTo(2));
        }
    }
}
