using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class SimulatedItemPoseEstimatorTests
    {
        private FakeCameraView _camera;

        [SetUp]
        public void SetUp()
        {
            // 원점에서 +Z를 보는 카메라: 시야각 90도, 0.05~3m.
            _camera = new FakeCameraView();
        }

        private SimulatedItemPoseEstimator Create(System.Func<Vector3, Vector3, bool> isBlocked = null, ObservationNoise noise = null)
        {
            return new SimulatedItemPoseEstimator(_camera, item => item.Pose, isBlocked, noise);
        }

        [Test]
        public void AnItemInFrontOfTheCamera_IsDetected_WhereItReallyIs()
        {
            ItemObservation seen = Create().Observe(PerceptionTestData.Item(new Vector3(0.1f, 0f, 0.5f)));

            Assert.That(seen.IsDetected, Is.True);
            Assert.That(seen.EstimatedPose.position, Is.EqualTo(new Vector3(0.1f, 0f, 0.5f)));
            Assert.That(seen.DistanceM, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(seen.SlotId, Is.EqualTo("A-1"));
        }

        [Test]
        public void AnItemBehindTheCamera_IsOutOfView()
        {
            ItemObservation seen = Create().Observe(PerceptionTestData.Item(new Vector3(0f, 0f, -0.5f)));

            Assert.That(seen.Status, Is.EqualTo(ObservationStatus.OutOfView));
        }

        [Test]
        public void AnItemFartherThanTheCameraRange_IsOutOfView()
        {
            Assert.That(Create().Observe(PerceptionTestData.Item(new Vector3(0f, 0f, 5f))).Status, Is.EqualTo(ObservationStatus.OutOfView));
        }

        [Test]
        public void AnItemInViewButHiddenBehindSomething_IsOccluded()
        {
            ItemObservation seen = Create((from, to) => true).Observe(PerceptionTestData.Item(new Vector3(0f, 0f, 0.5f)));

            Assert.That(seen.Status, Is.EqualTo(ObservationStatus.Occluded));
        }

        [Test]
        public void TheLineOfSightIsCheckedFromTheCameraToTheItem()
        {
            Vector3 askedFrom = Vector3.one * 99f;
            Vector3 askedTo = Vector3.one * 99f;
            _camera.At = new Pose(new Vector3(0f, 0.1f, 0f), Quaternion.identity);

            Create((from, to) =>
            {
                askedFrom = from;
                askedTo = to;
                return false;
            }).Observe(PerceptionTestData.Item(new Vector3(0f, 0.1f, 0.4f)));

            Assert.That(askedFrom, Is.EqualTo(new Vector3(0f, 0.1f, 0f)));
            Assert.That(askedTo, Is.EqualTo(new Vector3(0f, 0.1f, 0.4f)));
        }

        // 화면 카메라는 흔들림을 거르려고 손을 늦게 따라온다. 손이 막 도착했을 때 화면 자세가 아직 옛 자리에 있어도,
        // 물건이 보이는지는 손의 실제 자세로 가려야 한다.
        [Test]
        public void WhatTheCameraSees_FollowsTheHandPose_NotTheLaggingDisplayPose()
        {
            _camera.At = new Pose(new Vector3(2f, 0f, 0f), Quaternion.identity);
            _camera.SensorAt = new Pose(Vector3.zero, Quaternion.identity);

            ItemObservation seen = Create().Observe(PerceptionTestData.Item(new Vector3(0.1f, 0f, 0.5f)));

            Assert.That(seen.IsDetected, Is.True);
            Assert.That(seen.DistanceM, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void AnItemOutsideTheHandsView_IsOutOfView_EvenIfTheDisplayPoseWouldSeeIt()
        {
            _camera.At = new Pose(Vector3.zero, Quaternion.identity);
            _camera.SensorAt = new Pose(new Vector3(2f, 0f, 0f), Quaternion.identity);

            ItemObservation seen = Create().Observe(PerceptionTestData.Item(new Vector3(0.1f, 0f, 0.5f)));

            Assert.That(seen.Status, Is.EqualTo(ObservationStatus.OutOfView));
        }

        [Test]
        public void TheLineOfSightStartsAtTheHand_NotAtTheLaggingDisplayPose()
        {
            Vector3 askedFrom = Vector3.one * 99f;
            _camera.At = new Pose(new Vector3(2f, 0f, 0f), Quaternion.identity);
            _camera.SensorAt = new Pose(new Vector3(0f, 0.1f, 0f), Quaternion.identity);

            Create((from, to) =>
            {
                askedFrom = from;
                return false;
            }).Observe(PerceptionTestData.Item(new Vector3(0f, 0.1f, 0.4f)));

            Assert.That(askedFrom, Is.EqualTo(new Vector3(0f, 0.1f, 0f)));
        }

        [Test]
        public void WhenTheCameraIsNotReady_NothingIsAttempted()
        {
            _camera.Ready = false;

            ItemObservation seen = Create().Observe(PerceptionTestData.Item(new Vector3(0f, 0f, 0.5f)));

            Assert.That(seen.Status, Is.EqualTo(ObservationStatus.NotAttempted));
        }

        [Test]
        public void TheEstimateFollowsWhereTheItemIsNow_NotWhereTheRequestSaidItWas()
        {
            var estimator = new SimulatedItemPoseEstimator(
                _camera, item => new Pose(item.Pose.position + new Vector3(0.02f, 0f, 0f), Quaternion.identity), null, null);

            ItemObservation seen = estimator.Observe(PerceptionTestData.Item(new Vector3(0f, 0f, 0.5f)));

            Assert.That(seen.EstimatedPose.position.x, Is.EqualTo(0.02f).Within(1e-5f));
        }

        [Test]
        public void NoiseIsAddedToThePosition_ButNotToTheOrientation()
        {
            var noise = new ObservationNoise(0.01f, 9);
            var item = new ShelfPickItem(new Pose(new Vector3(0f, 0f, 0.5f), Quaternion.Euler(0f, 30f, 0f)), Vector3.back, null, "B-1");

            ItemObservation seen = Create(null, noise).Observe(item);

            Assert.That(seen.EstimatedPose.position, Is.Not.EqualTo(item.Pose.position));
            Assert.That(Vector3.Distance(seen.EstimatedPose.position, item.Pose.position), Is.LessThan(0.06f));
            Assert.That(Quaternion.Angle(seen.EstimatedPose.rotation, item.Pose.rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void TheRecorder_PassesEveryResultToTheListener_AndReturnsItUnchanged()
        {
            var estimator = new FakeEstimator { Next = ItemObservation.Missed("C-3", ObservationStatus.Occluded) };
            var recorder = new ObservationRecorder(estimator, _camera.Record);

            ItemObservation returned = recorder.Observe(PerceptionTestData.Item(Vector3.zero));

            Assert.That(returned.Status, Is.EqualTo(ObservationStatus.Occluded));
            Assert.That(_camera.LastObservation.Status, Is.EqualTo(ObservationStatus.Occluded));
            Assert.That(_camera.LastObservation.SlotId, Is.EqualTo("C-3"));
        }
    }
}
