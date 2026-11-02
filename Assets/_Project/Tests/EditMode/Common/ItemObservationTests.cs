using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Common.Tests
{
    public sealed class ItemObservationTests
    {
        [Test]
        public void ASeenItem_IsDetected_AndKeepsWhatWasMeasured()
        {
            ItemObservation seen = ItemObservation.Seen("A-1", new Pose(Vector3.one, Quaternion.identity), new Vector2(0.4f, 0.6f), 0.25f);

            Assert.That(seen.IsDetected, Is.True);
            Assert.That(seen.SlotId, Is.EqualTo("A-1"));
            Assert.That(seen.EstimatedPose.position, Is.EqualTo(Vector3.one));
            Assert.That(seen.Viewport, Is.EqualTo(new Vector2(0.4f, 0.6f)));
            Assert.That(seen.DistanceM, Is.EqualTo(0.25f));
        }

        [TestCase(ObservationStatus.OutOfView)]
        [TestCase(ObservationStatus.Occluded)]
        [TestCase(ObservationStatus.NotAttempted)]
        public void AMissedItem_IsNotDetected(ObservationStatus status)
        {
            Assert.That(ItemObservation.Missed("B-2", status).IsDetected, Is.False);
        }

        [Test]
        public void NothingObservedYet_IsNotDetected()
        {
            Assert.That(ItemObservation.None.IsDetected, Is.False);
            Assert.That(ItemObservation.None.Status, Is.EqualTo(ObservationStatus.NotAttempted));
        }

        [Test]
        public void TheDescription_SaysWhyAnItemWasMissed()
        {
            Assert.That(ItemObservation.Missed("A-1", ObservationStatus.Occluded).Describe(), Does.Contain("가려"));
            Assert.That(ItemObservation.Missed("A-1", ObservationStatus.OutOfView).Describe(), Does.Contain("시야"));
            Assert.That(ItemObservation.Seen("A-1", default, Vector2.zero, 0.3f).Describe(), Does.Contain("A-1"));
        }
    }
}
