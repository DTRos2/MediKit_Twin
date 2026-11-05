using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ObservationGraspRefinerTests
    {
        private static readonly Vector3 ItemAt = new Vector3(1f, 1f, 1f);

        private readonly List<string> _log = new List<string>();
        private FakeEstimator _estimator;
        private FakeSeededSolver _solver;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _estimator = new FakeEstimator();
            _solver = new FakeSeededSolver();
        }

        private ObservationGraspRefiner Create(
            bool requireRecognition = false, float max = 0.03f, bool withRoutes = true, Func<GraspTargets, bool> isClear = null,
            ShiftBackoff backoff = null, float fingerDrop = 0f, float tiltDeg = 0f)
        {
            return new ObservationGraspRefiner(
                _estimator,
                PerceptionTestData.Recognition(requireRecognition, 0.004f, max, fingerDrop, tiltDeg),
                withRoutes ? new SeededRouteBuilder(_solver) : null,
                isClear,
                _log.Add,
                backoff);
        }

        private static ItemPickRequest Request() => new ItemPickRequest(PerceptionTestData.Item(ItemAt), () => true);

        [Test]
        public void WhenTheItemIsGrasped_TheGraspedCallbackIsCalledOnce()
        {
            int told = 0;
            var refiner = new ObservationGraspRefiner(
                _estimator, PerceptionTestData.Recognition(false, 0.004f, 0.03f), null, null, _log.Add, onItemGrasped: () => told++);

            refiner.ItemGrasped(Request());

            Assert.That(told, Is.EqualTo(1));
        }

        [Test]
        public void WhenNoCallbackIsGiven_GraspingTheItemDoesNothingAndDoesNotThrow()
        {
            Assert.DoesNotThrow(() => Create().ItemGrasped(Request()));
        }

        private static GraspTargets Planned(TaughtJointRoute route = null) =>
            new GraspTargets(Quaternion.identity, ItemAt + Vector3.back * 0.15f, ItemAt, ItemAt + Vector3.back * 0.1f, route);

        private static TaughtJointRoute RouteSolvedBy(FakeSeededSolver solver, GraspTargets targets)
        {
            Assert.That(new SeededRouteBuilder(solver).TryBuild(new float[] { 0f, 0f }, targets.NearPose, targets.GraspPose, targets.RetreatPose, out TaughtJointRoute route), Is.True);
            return route;
        }

        private void Sees(Vector3 position) =>
            _estimator.Next = ItemObservation.Seen("A-1", new Pose(position, Quaternion.identity), new Vector2(0.5f, 0.5f), 0.2f);

        [Test]
        public void WhenTheItemIsWhereExpected_ThePlannedPositionsAreKept()
        {
            Sees(ItemAt);
            GraspTargets planned = Planned();

            GraspRefinement refinement = Create().Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.IsRejected, Is.False);
            Assert.That(refinement.Targets.Grasp, Is.EqualTo(planned.Grasp));
        }

        [Test]
        public void AJitterBelowTheMinimum_DoesNotMoveTheArm()
        {
            Sees(ItemAt + new Vector3(0.002f, 0f, 0f));

            GraspRefinement refinement = Create().Refine(Request(), Planned());

            Assert.That(refinement.IsCorrected, Is.False);
        }

        [Test]
        public void WhenTheItemIsSeenElsewhere_AllThreePositionsMoveByTheSameAmount()
        {
            Sees(ItemAt + new Vector3(0.01f, -0.01f, 0f));
            GraspTargets planned = Planned();

            GraspRefinement refinement = Create().Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.True);
            var shift = new Vector3(0.01f, -0.01f, 0f);
            Assert.That(Vector3.Distance(refinement.Targets.Grasp, planned.Grasp + shift), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(refinement.Targets.Near, planned.Near + shift), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(refinement.Targets.Retreat, planned.Retreat + shift), Is.LessThan(1e-5f));
            Assert.That(refinement.Targets.Rotation, Is.EqualTo(planned.Rotation));
        }

        [Test]
        public void ABigDifference_IsCorrectedOnlyUpToTheLimit()
        {
            Sees(ItemAt + new Vector3(0.5f, 0f, 0f));

            GraspRefinement refinement = Create(max: 0.03f).Refine(Request(), Planned());

            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(refinement.Targets.Grasp.x - ItemAt.x, Is.EqualTo(0.03f).Within(1e-5f));
        }

        // 알려 준 자세처럼 쥐는 자리가 요청 시점의 물건 위치와 따로 정해진 계획. 세 자리 모두 offset만큼 벗어나 있다.
        private static GraspTargets PlannedOffTheItem(Vector3 offset) =>
            new GraspTargets(Quaternion.identity, ItemAt + offset + Vector3.back * 0.15f, ItemAt + offset, ItemAt + offset + Vector3.back * 0.1f, null);

        [Test]
        public void WhenThePlannedGraspIsOffTheItem_TheGraspMovesOntoTheSeenItem()
        {
            // 물건은 요청한 자리 그대로인데 알려 준 쥐는 자리가 5cm 벗어나 있다. 예전에는 둘을 견주지 않아 고치지 않았다.
            Sees(ItemAt);
            var offset = new Vector3(0.05f, 0f, 0f);
            GraspTargets planned = PlannedOffTheItem(offset);

            GraspRefinement refinement = Create(max: 0.08f).Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(Vector3.Distance(refinement.Targets.Grasp, ItemAt), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(refinement.Targets.Near, planned.Near - offset), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(refinement.Targets.Retreat, planned.Retreat - offset), Is.LessThan(1e-5f));
        }

        [Test]
        public void WhenThePlannedGraspIsFarOffTheItem_TheGraspMovesTowardItOnlyUpToTheLimit()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0.2f, 0f, 0f));

            GraspRefinement refinement = Create(max: 0.08f).Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(refinement.Targets.Grasp.x - ItemAt.x, Is.EqualTo(0.12f).Within(1e-5f), "20cm 벗어난 것을 8cm만 고쳐 12cm가 남는다");
        }

        [Test]
        public void WhenTheItemMovedAfterTheRequest_ThePlannedGraspStillFollowsTheSeenItem()
        {
            // 스스로 찾은 자세는 쥐는 자리가 요청 시점의 물건 위치라서, 예전 방식과 같은 결과가 나와야 한다.
            Sees(ItemAt + new Vector3(0f, 0f, 0.02f));

            GraspRefinement refinement = Create().Refine(Request(), Planned());

            Assert.That(refinement.Targets.Grasp.z, Is.EqualTo(ItemAt.z + 0.02f).Within(1e-5f));
        }

        [Test]
        public void WhenTheItemIsNotFound_ByDefault_TheItemIsStillGraspedWhereItWasPlanned()
        {
            _estimator.Next = ItemObservation.Missed("A-1", ObservationStatus.Occluded);

            GraspRefinement refinement = Create().Refine(Request(), Planned());

            Assert.That(refinement.IsRejected, Is.False);
            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(_log.Count, Is.EqualTo(1), "찾지 못했다는 경고가 남는다");
            Assert.That(_log[0], Does.Contain("가려"));
        }

        [Test]
        public void WhenTheItemIsNotFound_AndRecognitionIsRequired_TheGraspIsRefused()
        {
            _estimator.Next = ItemObservation.Missed("A-1", ObservationStatus.OutOfView);

            GraspRefinement refinement = Create(requireRecognition: true).Refine(Request(), Planned());

            Assert.That(refinement.IsRejected, Is.True);
            Assert.That(refinement.Note, Does.Contain("A-1"));
        }

        [Test]
        public void OnAJointRoute_TheShiftedPositionsAreSolvedAgainFromTheGraspJoints()
        {
            GraspTargets planned = Planned();
            TaughtJointRoute route = RouteSolvedBy(_solver, planned);
            _solver.Calls = 0;
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create().Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(refinement.Targets.Route, Is.Not.Null);
            Assert.That(refinement.Targets.Route, Is.Not.SameAs(route));
            // 가짜 풀이는 도구 자세의 x를 첫 관절 각도로 삼는다. 쥐는 자리의 x가 0.01 늘었으니 관절 각도도 그만큼이다.
            Assert.That(refinement.Targets.Route.Grasp[0], Is.EqualTo(ItemAt.x + 0.01f).Within(1e-5f));
            Assert.That(_solver.LastSeed, Is.Not.Null);
        }

        [Test]
        public void OnAJointRoute_WhenOnlyTheTiltedPoseCannotBeSolved_TheItemIsStillReachedWithoutTilting()
        {
            // 숙인 자세를 못 푼다고 옮기는 것까지 포기하면 손가락이 물건에 닿지 못한다. 숙이지 않고 옮겨야 한다.
            GraspTargets planned = Planned();
            TaughtJointRoute route = RouteSolvedBy(_solver, planned);
            _solver.RefusesTilted = true;
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create(tiltDeg: 15f).Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.True, "숙이지 않고라도 물건 쪽으로 옮겨야 한다");
            Assert.That(refinement.Targets.Grasp.x, Is.EqualTo(ItemAt.x + 0.01f).Within(1e-5f));
            Assert.That(Quaternion.Angle(refinement.Targets.Rotation, planned.Rotation), Is.LessThan(1e-3f), "숙이지 않았다면 도구 방향은 그대로다");
            Assert.That(_log.Count, Is.EqualTo(1));
            StringAssert.Contains("숙이지 않고", _log[0]);
        }

        [Test]
        public void OnAJointRoute_WhenTheTiltedPoseIsSolved_TheTiltIsKept()
        {
            GraspTargets planned = Planned();
            TaughtJointRoute route = RouteSolvedBy(_solver, planned);
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create(tiltDeg: 15f).Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(Quaternion.Angle(refinement.Targets.Rotation, planned.Rotation), Is.EqualTo(15f).Within(0.1f));
            StringAssert.Contains("숙여", _log[0]);
        }

        [Test]
        public void OnAJointRoute_WhenNothingCanBeSolvedEvenWithoutTilting_ThePlannedRouteIsKept()
        {
            GraspTargets planned = Planned();
            TaughtJointRoute route = RouteSolvedBy(_solver, planned);
            _solver.Solves = false;
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create(tiltDeg: 15f).Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.Targets.Route, Is.SameAs(route));
        }

        [Test]
        public void OnAJointRoute_WhenTheShiftedPositionCannotBeSolved_ThePlannedRouteIsKept()
        {
            GraspTargets planned = Planned();
            TaughtJointRoute route = RouteSolvedBy(_solver, planned);
            _solver.Solves = false;
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create().Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.Targets.Route, Is.SameAs(route));
            Assert.That(refinement.Targets.Grasp, Is.EqualTo(ItemAt));
        }

        [Test]
        public void OnAJointRoute_WithoutASolver_ThePlannedRouteIsKept()
        {
            TaughtJointRoute route = RouteSolvedBy(_solver, Planned());
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create(withRoutes: false).Refine(Request(), Planned(route));

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.Targets.Route, Is.SameAs(route));
        }

        [Test]
        public void ACorrectionThatWouldHitSomething_IsDropped_WhenThePlannedPositionWasFine()
        {
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));
            GraspTargets planned = Planned();

            // 계획한 자리는 겹치지 않고, 옮긴 자리는 겹친다.
            GraspRefinement refinement = Create(isClear: targets => Mathf.Approximately(targets.Grasp.x, ItemAt.x)).Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.Targets.Grasp, Is.EqualTo(planned.Grasp));
            Assert.That(_log[_log.Count - 1], Does.Contain("겹쳐"));
        }

        [Test]
        public void ACorrectionIsStillMade_WhenThePlannedPositionAlreadyOverlapsToo()
        {
            Sees(ItemAt + new Vector3(0.01f, 0f, 0f));

            GraspRefinement refinement = Create(isClear: targets => false).Refine(Request(), Planned());

            Assert.That(refinement.IsCorrected, Is.True, "원래도 겹치던 자리라면 옮겨서 더 나빠지는 것이 아니다");
        }

        [Test]
        public void WithAFingerDrop_AnItemOfUnknownHeight_IsAimedAboveItsCentre()
        {
            Sees(ItemAt);

            GraspRefinement refinement = Create(max: 0.08f, fingerDrop: 0.025f).Refine(Request(), Planned());

            Assert.That(refinement.IsCorrected, Is.True, "물건이 제자리여도 손가락이 판에 닿지 않게 높여야 한다");
            Assert.That(refinement.Targets.Grasp.y - ItemAt.y, Is.EqualTo(0.025f).Within(1e-5f));
            Assert.That(refinement.Targets.Grasp.x, Is.EqualTo(ItemAt.x).Within(1e-5f));
            Assert.That(_log[_log.Count - 1], Does.Contain("높게 겨냥"));
        }

        [Test]
        public void WithAFingerDrop_ATallItem_IsAimedAtItsCentre()
        {
            Sees(ItemAt);
            GameObject tall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                tall.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
                var item = new ShelfPickItem(new Pose(ItemAt, Quaternion.identity), Vector3.back, tall, "A-1");

                GraspRefinement refinement = Create(max: 0.08f, fingerDrop: 0.025f).Refine(new ItemPickRequest(item, () => true), Planned());

                Assert.That(refinement.IsCorrected, Is.False, "키가 큰 물건은 중심이 이미 판에서 충분히 떠 있다");
                Assert.That(refinement.Targets.Grasp, Is.EqualTo(ItemAt));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tall);
            }
        }

        [Test]
        public void WithAFingerDrop_AThinItem_IsAimedAboveItsCentreByTheRest()
        {
            Sees(ItemAt);
            GameObject thin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                thin.transform.localScale = new Vector3(0.06f, 0.018f, 0.06f);
                var item = new ShelfPickItem(new Pose(ItemAt, Quaternion.identity), Vector3.back, thin, "A-1");

                GraspRefinement refinement = Create(max: 0.08f, fingerDrop: 0.025f).Refine(new ItemPickRequest(item, () => true), Planned());

                // 손가락이 2.5cm 내려오고 물건 높이의 절반이 0.9cm이므로 1.6cm 높게 겨냥한다.
                Assert.That(refinement.Targets.Grasp.y - ItemAt.y, Is.EqualTo(0.016f).Within(1e-4f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(thin);
            }
        }

        // 쥐는 자리가 물건보다 8cm 위에 있고, 물건 바로 밑에 바닥판이 있어 물건 위 3cm보다 낮게는 못 내려간다고 본다.
        private static bool AboveTheShelfBoard(GraspTargets targets) => targets.Grasp.y >= ItemAt.y + 0.03f - 1e-5f;

        [Test]
        public void ACorrectionThatWouldHitTheBoard_IsCutBackToTheLargestPartThatDoesNot()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0f, 0.08f, 0f));

            GraspRefinement refinement = Create(max: 0.1f, isClear: AboveTheShelfBoard).Refine(Request(), planned);

            // 전부(8cm)와 3/4(6cm)는 3cm보다 낮아 닿고, 1/2(4cm)부터 닿지 않는다.
            Assert.That(refinement.IsCorrected, Is.True);
            Assert.That(refinement.Targets.Grasp.y - ItemAt.y, Is.EqualTo(0.04f).Within(1e-5f));
            Assert.That(_log[_log.Count - 1], Does.Contain("50%"));
        }

        [Test]
        public void ACorrectionThatIsCutBack_MovesAllThreePositionsByTheSameAmount()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0f, 0.08f, 0f));

            GraspTargets corrected = Create(max: 0.1f, isClear: AboveTheShelfBoard).Refine(Request(), planned).Targets;

            Assert.That(corrected.Near.y - planned.Near.y, Is.EqualTo(corrected.Grasp.y - planned.Grasp.y).Within(1e-5f));
            Assert.That(corrected.Retreat.y - planned.Retreat.y, Is.EqualTo(corrected.Grasp.y - planned.Grasp.y).Within(1e-5f));
        }

        [Test]
        public void WhenEvenTheSmallestPartHitsTheBoard_TheCorrectionIsDropped()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0f, 0.08f, 0f));

            // 7cm보다 낮으면 닿는다고 본다. 줄인 가장 작은 양(1/4, 2cm 내림)은 6cm라 닿는다.
            GraspRefinement refinement = Create(max: 0.1f, isClear: targets => targets.Grasp.y >= ItemAt.y + 0.07f).Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.False);
            Assert.That(refinement.Targets.Grasp, Is.EqualTo(planned.Grasp));
            Assert.That(_log[_log.Count - 1], Does.Contain("겹쳐"));
        }

        [Test]
        public void WithoutABackoff_OnlyTheWholeCorrectionIsTried()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0f, 0.08f, 0f));

            GraspRefinement refinement = Create(max: 0.1f, isClear: AboveTheShelfBoard, backoff: ShiftBackoff.None).Refine(Request(), planned);

            Assert.That(refinement.IsCorrected, Is.False);
        }

        [Test]
        public void AWholeCorrectionThatIsFine_IsNotCutBack()
        {
            Sees(ItemAt);
            GraspTargets planned = PlannedOffTheItem(new Vector3(0f, 0.08f, 0f));

            GraspRefinement refinement = Create(max: 0.1f, isClear: targets => true).Refine(Request(), planned);

            Assert.That(refinement.Targets.Grasp.y, Is.EqualTo(ItemAt.y).Within(1e-5f));
            Assert.That(_log[_log.Count - 1], Does.Not.Contain("%만"));
        }
    }
}
