using NUnit.Framework;

namespace MediKitTwin.Common.Tests
{
    public sealed class ObservationShowTimerTests
    {
        [Test]
        public void NothingIsShown_BeforeAnythingWasObserved()
        {
            var timer = new ObservationShowTimer(5f);
            timer.Observe(0, 0f);

            Assert.That(timer.IsVisible(0f), Is.False);
            Assert.That(timer.IsVisible(100f), Is.False);
        }

        [Test]
        public void AfterAnObservation_ItIsShownForTheGivenSeconds_ThenHidden()
        {
            var timer = new ObservationShowTimer(5f);
            timer.Observe(1, 10f);

            Assert.That(timer.IsVisible(14.9f), Is.True);
            Assert.That(timer.IsVisible(15.1f), Is.False);
        }

        [Test]
        public void TheSameCountPassedEveryFrame_DoesNotRestartTheTimer()
        {
            var timer = new ObservationShowTimer(5f);
            timer.Observe(1, 10f);

            timer.Observe(1, 13f);

            Assert.That(timer.IsVisible(15.1f), Is.False);
        }

        [Test]
        public void AnotherObservation_RestartsTheTimer_EvenIfTheResultWasTheSame()
        {
            var timer = new ObservationShowTimer(5f);
            timer.Observe(1, 10f);

            timer.Observe(2, 14f);

            Assert.That(timer.IsVisible(18.9f), Is.True);
        }
    }
}
