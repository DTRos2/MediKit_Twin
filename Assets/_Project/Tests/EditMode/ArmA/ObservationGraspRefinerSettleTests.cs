using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    /// <summary>쥐는 자리에 닿은 뒤 도구 끝점의 실제 오차를 메우는 일(IGraspSettler)과, 쥘 때 손가락 끝을 숙이는 설정을 시험한다.</summary>
    public sealed class ObservationGraspRefinerSettleTests
    {
        private static readonly Vector3 ItemAt = new Vector3(1f, 1f, 1f);

        private readonly List<string> _log = new List<string>();
        private FakeEstimator _estimator;
        private FakeSeededSolver _solver;
        private Vector3 _toolAt;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _estimator = new FakeEstimator();
            _solver = new FakeSeededSolver();
            _toolAt = ItemAt;
            Sees(ItemAt);
        }

        private ObservationGraspRefiner Create(RecognitionSettings settings = null, bool withToolPose = true, bool withRoutes = false)
        {
            return new ObservationGraspRefiner(
                _estimator,
                settings ?? PerceptionTestData.Recognition(false),
                withRoutes ? new SeededRouteBuilder(_solver) : null,
                null,
                _log.Add,
                null,
                withToolPose ? (System.Func<Pose>)(() => new Pose(_toolAt, Quaternion.identity)) : null);
        }

        private void Sees(Vector3 position) =>
            _estimator.Next = ItemObservation.Seen("A-1", new Pose(position, Quaternion.identity), new Vector2(0.5f, 0.5f), 0.2f);

        private static ItemPickRequest Request() => new ItemPickRequest(PerceptionTestData.Item(ItemAt), () => true);

        private static GraspTargets Commanded(TaughtJointRoute route = null) =>
            new GraspTargets(Quaternion.identity, ItemAt + Vector3.back * 0.15f, ItemAt, ItemAt + Vector3.back * 0.1f, route);

        [Test]
        public void WhenTheToolIsShortOfTheAim_TheErrorIsAddedToTheCommand()
        {
            _toolAt = ItemAt - new Vector3(0f, 0f, 0.02f);
            GraspTargets commanded = Commanded();

            GraspRefinement settled = Create().Settle(Request(), commanded);

            Assert.That(settled.IsCorrected, Is.True);
            var error = new Vector3(0f, 0f, 0.02f);
            Assert.That(Vector3.Distance(settled.Targets.Grasp, commanded.Grasp + error), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(settled.Targets.Near, commanded.Near + error), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(settled.Targets.Retreat, commanded.Retreat + error), Is.LessThan(1e-5f));
        }

        [Test]
        public void TheCommandIsNotTheReference_OnlyTheAimIs()
        {
            // 명령(Grasp)이 물건에서 11cm 벗어나 있어도, 도구 끝점이 겨냥에 닿아 있으면 더 움직이지 않는다.
            GraspTargets commandedFarAbove = new GraspTargets(Quaternion.identity, ItemAt, ItemAt + Vector3.up * 0.11f, ItemAt, null);
            _toolAt = ItemAt;

            GraspRefinement settled = Create().Settle(Request(), commandedFarAbove);

            Assert.That(settled.IsCorrected, Is.False);
            Assert.That(settled.Targets.Grasp, Is.EqualTo(commandedFarAbove.Grasp));
        }

        [Test]
        public void AnErrorWithinTheTolerance_IsLeftAlone()
        {
            _toolAt = ItemAt + new Vector3(0.003f, 0f, 0f);

            GraspRefinement settled = Create(PerceptionTestData.Recognition(false, settleTolerance: 0.005f)).Settle(Request(), Commanded());

            Assert.That(settled.IsCorrected, Is.False);
        }

        [Test]
        public void ALargeError_IsClosedOnlyUpToTheLimit()
        {
            _toolAt = ItemAt - new Vector3(0f, 0f, 0.5f);
            GraspTargets commanded = Commanded();

            GraspRefinement settled = Create(PerceptionTestData.Recognition(false, maxSettle: 0.04f)).Settle(Request(), commanded);

            Assert.That(settled.IsCorrected, Is.True);
            Assert.That(settled.Targets.Grasp.z - commanded.Grasp.z, Is.EqualTo(0.04f).Within(1e-5f));
        }

        [Test]
        public void TheAimIncludesTheClearanceAboveTheItemCentre()
        {
            // 손가락 뿌리가 판에 눌리지 않게 물건 중심보다 2.5cm 높게 겨냥한다. 도구 끝점이 거기 있으면 더 움직이지 않는다.
            RecognitionSettings settings = PerceptionTestData.Recognition(false, fingerDrop: 0.025f);
            _toolAt = ItemAt + Vector3.up * 0.025f;

            Assert.That(Create(settings).Settle(Request(), Commanded()).IsCorrected, Is.False);

            _toolAt = ItemAt;
            GraspRefinement low = Create(settings).Settle(Request(), Commanded());
            Assert.That(low.IsCorrected, Is.True);
            Assert.That(low.Targets.Grasp.y - Commanded().Grasp.y, Is.EqualTo(0.025f).Within(1e-5f));
        }

        [Test]
        public void SettlingKeepsTheToolOrientation_AndDoesNotTiltAgain()
        {
            Quaternion tilted = Quaternion.AngleAxis(15f, Vector3.up);
            var commanded = new GraspTargets(tilted, ItemAt + Vector3.back * 0.15f, ItemAt, ItemAt + Vector3.back * 0.1f, null);
            _toolAt = ItemAt - new Vector3(0f, 0f, 0.02f);

            GraspRefinement settled = Create(PerceptionTestData.Recognition(false, tiltDeg: 15f)).Settle(Request(), commanded);

            Assert.That(settled.IsCorrected, Is.True);
            Assert.That(Quaternion.Angle(settled.Targets.Rotation, tilted), Is.LessThan(1e-3f));
        }

        [Test]
        public void WhenTheItemCannotBeSeenAgain_TheCommandIsKept()
        {
            _estimator.Next = ItemObservation.Missed("A-1", ObservationStatus.Occluded);
            _toolAt = ItemAt - new Vector3(0f, 0f, 0.02f);

            GraspRefinement settled = Create().Settle(Request(), Commanded());

            Assert.That(settled.IsCorrected, Is.False);
            Assert.That(settled.IsRejected, Is.False);
        }

        [Test]
        public void WithoutAToolPose_NothingIsSettled()
        {
            ObservationGraspRefiner refiner = Create(withToolPose: false);

            Assert.That(refiner.SettleAttempts, Is.EqualTo(0));
            Assert.That(refiner.Settle(Request(), Commanded()).IsCorrected, Is.False);
        }

        [Test]
        public void TheAttemptsAndWaitComeFromTheSettings()
        {
            ObservationGraspRefiner refiner = Create(PerceptionTestData.Recognition(false, settleAttempts: 3));

            Assert.That(refiner.SettleAttempts, Is.EqualTo(3));
            Assert.That(refiner.SettleWaitSeconds, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void WhenThereIsAJointRoute_ItIsSolvedAgainForTheSettledPosition()
        {
            GraspTargets planned = Commanded();
            Assert.That(
                new SeededRouteBuilder(_solver).TryBuild(new float[] { 0f, 0f }, planned.NearPose, planned.GraspPose, planned.RetreatPose, out TaughtJointRoute route),
                Is.True);
            _solver.Calls = 0;
            _toolAt = ItemAt - new Vector3(0.02f, 0f, 0f);

            GraspRefinement settled = Create(withRoutes: true).Settle(Request(), Commanded(route));

            Assert.That(settled.IsCorrected, Is.True);
            Assert.That(_solver.Calls, Is.GreaterThan(0));
            Assert.That(settled.Targets.Route, Is.Not.SameAs(route));
        }

        [Test]
        public void WhenTheTiltIsSet_TheGraspIsTiltedEvenWithoutAnyShift()
        {
            GraspTargets planned = Commanded();

            GraspRefinement refined = Create(PerceptionTestData.Recognition(false, tiltDeg: 15f)).Refine(Request(), planned);

            Assert.That(refined.IsCorrected, Is.True);
            Assert.That(refined.Targets.Grasp, Is.EqualTo(planned.Grasp));
            Assert.That(Quaternion.Angle(refined.Targets.Rotation, planned.Rotation * Quaternion.AngleAxis(15f, Vector3.up)), Is.LessThan(1e-3f));
        }

        [Test]
        public void WithoutATilt_TheOrientationIsKept()
        {
            GraspTargets planned = Commanded();

            GraspRefinement refined = Create(PerceptionTestData.Recognition(false, tiltDeg: 0f)).Refine(Request(), planned);

            Assert.That(refined.IsCorrected, Is.False);
            Assert.That(refined.Targets.Rotation, Is.EqualTo(planned.Rotation));
        }
    }
}
