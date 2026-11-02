using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Common.Tests
{
    public sealed class RecognitionMarkTrackerTests
    {
        private static ItemObservation Seen(string slot, float x)
        {
            return ItemObservation.Seen(slot, new Pose(new Vector3(x, 1f, 0f), Quaternion.identity), new Vector2(0.5f, 0.5f), 0.3f);
        }

        [Test]
        public void NothingIsShown_BeforeAnythingIsFound()
        {
            var tracker = new RecognitionMarkTracker(3f);
            tracker.Observe(ItemObservation.None, 0f);

            Assert.That(tracker.TryGetMark(0f, out _), Is.False);
        }

        [Test]
        public void AFoundItem_IsShownForTheGivenSeconds_ThenHidden()
        {
            var tracker = new RecognitionMarkTracker(3f);
            tracker.Observe(Seen("A-1", 1f), 10f);

            Assert.That(tracker.TryGetMark(12.9f, out ItemObservation mark), Is.True);
            Assert.That(mark.SlotId, Is.EqualTo("A-1"));
            Assert.That(tracker.TryGetMark(13.1f, out _), Is.False);
        }

        [Test]
        public void TheSameResultPassedEveryFrame_DoesNotRestartTheTimer()
        {
            var tracker = new RecognitionMarkTracker(3f);
            ItemObservation seen = Seen("A-1", 1f);

            tracker.Observe(seen, 10f);
            tracker.Observe(seen, 12.5f);

            Assert.That(tracker.TryGetMark(13.1f, out _), Is.False);
        }

        [Test]
        public void ANewResult_RestartsTheTimer_AndMovesTheMark()
        {
            var tracker = new RecognitionMarkTracker(3f);
            tracker.Observe(Seen("A-1", 1f), 10f);
            tracker.Observe(Seen("A-1", 1.02f), 12.5f);

            Assert.That(tracker.TryGetMark(14f, out ItemObservation mark), Is.True);
            Assert.That(mark.EstimatedPose.position.x, Is.EqualTo(1.02f).Within(1e-5f));
        }

        [Test]
        public void AMissedResult_ClearsTheMarkAtOnce()
        {
            var tracker = new RecognitionMarkTracker(3f);
            tracker.Observe(Seen("A-1", 1f), 10f);
            tracker.Observe(ItemObservation.Missed("A-1", ObservationStatus.Occluded), 10.5f);

            Assert.That(tracker.TryGetMark(10.6f, out _), Is.False);
        }
    }
}
