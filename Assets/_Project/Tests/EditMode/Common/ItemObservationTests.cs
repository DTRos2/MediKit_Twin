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

        private static readonly PinholeCamera Square = new PinholeCamera(90f, 1f, 0.1f, 10f);
        private static readonly Pose AtOriginLookingForward = new Pose(Vector3.zero, Quaternion.identity);

        [Test]
        public void TheLiveViewport_FollowsTheCamera_NotTheMomentTheItemWasFound()
        {
            // 찾아본 순간에는 물건이 화면 가운데(0.5, 0.5)에 있었다. 그 뒤 카메라가 오른쪽으로 1m 옮겨 가면 물건은 화면 왼쪽으로 가야 한다.
            var itemPose = new Pose(new Vector3(0f, 0f, 2f), Quaternion.identity);
            ItemObservation seen = ItemObservation.Seen("A-1", itemPose, new Vector2(0.5f, 0.5f), 2f);
            var movedCamera = new Pose(new Vector3(1f, 0f, 0f), Quaternion.identity);

            bool isVisible = seen.TryViewportFrom(Square, movedCamera, out Vector2 live);

            Assert.That(isVisible, Is.True);
            Assert.That(live.x, Is.EqualTo(0.25f).Within(1e-4f), "카메라가 오른쪽으로 갔으므로 물건은 화면 왼쪽으로 가야 한다");
            Assert.That(live.y, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(live, Is.Not.EqualTo(seen.Viewport), "찾아본 순간의 값을 그대로 돌려주면 안 된다");
        }

        [Test]
        public void TheLiveViewport_IsTheSameAsTheFoundOne_WhenTheCameraHasNotMoved()
        {
            var itemPose = new Pose(new Vector3(1f, 1f, 2f), Quaternion.identity);
            Square.TryProject(AtOriginLookingForward, itemPose.position, out Vector2 atFindTime, out float depth);
            ItemObservation seen = ItemObservation.Seen("A-1", itemPose, atFindTime, depth);

            seen.TryViewportFrom(Square, AtOriginLookingForward, out Vector2 live);

            Assert.That(live.x, Is.EqualTo(seen.Viewport.x).Within(1e-4f));
            Assert.That(live.y, Is.EqualTo(seen.Viewport.y).Within(1e-4f));
        }

        [Test]
        public void TheLiveViewport_IsNotVisible_OnceTheCameraTurnsAwayFromTheItem()
        {
            var itemPose = new Pose(new Vector3(0f, 0f, 2f), Quaternion.identity);
            ItemObservation seen = ItemObservation.Seen("A-1", itemPose, new Vector2(0.5f, 0.5f), 2f);
            var turnedAway = new Pose(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));

            Assert.That(seen.TryViewportFrom(Square, turnedAway, out _), Is.False);
        }

        [Test]
        public void AMissedItem_HasNoLiveViewport()
        {
            ItemObservation missed = ItemObservation.Missed("A-1", ObservationStatus.OutOfView);

            Assert.That(missed.TryViewportFrom(Square, AtOriginLookingForward, out _), Is.False);
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
