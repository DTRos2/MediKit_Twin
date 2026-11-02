using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandSearchRoutineTests
    {
        private const string ReachStep = "물건 앞쪽으로 팔 뻗기";

        // 도달 판정은 주입하므로, 팔이 닿는 방향의 수를 시험이 정한다.
        private sealed class FakeTool : IToolPoseTarget
        {
            public bool HasToolTarget => false;
            public bool LastSolveSucceeded => true;
            public IkResult LastResult => default;

            public void SetToolTarget(Pose worldPose) { }

            public void ClearToolTarget() { }

            public IkResult Probe(Pose worldPose) => new IkResult(false, 0.4f, 20f, 5);

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg) => true;
        }

        private sealed class FakeBasePositioner : IArmBasePositioner
        {
            internal readonly List<Vector3> Destinations = new List<Vector3>();
            internal bool Arrives = true;

            public string LastFailureReason => Arrives ? string.Empty : "막힘";

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
            {
                Destinations.Add(worldPosition);
                done(Arrives);
                yield break;
            }

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
            {
                Destinations.Add(worldPosition);
                done(Arrives);
                yield break;
            }
        }

        private sealed class FakeBaseState : IArmBaseStateSource
        {
            public bool TryGetState(out Pose armBaseWorldPose, out float rootYawDeg)
            {
                armBaseWorldPose = new Pose(Vector3.zero, Quaternion.identity);
                rootYawDeg = 0f;
                return true;
            }
        }

        private readonly List<string> _logs = new List<string>();
        private FakeBasePositioner _positioner;
        private float _time;

        [SetUp]
        public void SetUp()
        {
            _logs.Clear();
            _positioner = new FakeBasePositioner();
            _time = 0f;
        }

        private StandSearchRoutine Create(Func<Pose, bool> canReach, StandSearchSettings settings, bool withBaseState)
        {
            var tool = new FakeTool();
            var diagnostics = new TransferDiagnostics(tool, null, null, () => false, 0.01f);
            var tolerances = new MotionTolerances(0.03f, 10f, 0.01f, 5f, 4f, 1.5f);
            var steps = new ArmMotionSteps(_positioner, new ToolPoseMover(tool, 0.03f, 10f, 5f), null, diagnostics, tolerances);

            return new StandSearchRoutine(
                steps,
                new GraspOrientationSearcher(canReach),
                new StandPlacementCost(1f, 100f),
                canReach,
                withBaseState ? new FakeBaseState() : null,
                diagnostics,
                settings,
                () => _time,
                _logs.Add);
        }

        private static ReachPlan PlanOf(IReadOnlyList<float> standoffs, IReadOnlyList<float> laterals)
        {
            return new ReachPlan(
                standoffs,
                laterals,
                null,
                (extra, lateral) => new Vector3(lateral, 0f, extra),
                Quaternion.identity,
                new[] { new Vector3(0f, 1f, 2f) },
                new OrientationSearchSettings(),
                string.Empty,
                "서는 자리로 이동",
                ReachStep,
                0f);
        }

        private OrientationSearchResult Run(StandSearchRoutine routine, ReachPlan plan, TransferResult result)
        {
            var found = new OrientationSearchResult();
            CoroutineRunner.Drain(routine.ApproachAndSearch(plan, result, search => found = search), () => _time += 0.02f);
            return found;
        }

        [Test]
        public void StandsAtTheBasePlace_AndKeepsTheNominalOrientation_WhenTheArmReachesIt()
        {
            StandSearchRoutine routine = Create(pose => true, new StandSearchSettings(false, 3), withBaseState: true);
            var result = new TransferResult();

            OrientationSearchResult search = Run(routine, PlanOf(new[] { 0f, 0.1f }, new[] { 0f }), result);

            Assert.That(search.IsFound, Is.True);
            Assert.That(_positioner.Destinations, Is.EqualTo(new[] { new Vector3(0f, 0f, 0f) }));
        }

        [Test]
        public void StepsBack_WhenTheFirstPlaceIsOutOfReach_AndPlanningIsOff()
        {
            // 기본 자리(서는 거리 보정 0)에서는 닿는 방향이 없고, 0.1m 물러난 자리에서만 닿는다.
            bool isAtFarStand = false;
            StandSearchRoutine routine = Create(pose => isAtFarStand, new StandSearchSettings(false, 3), withBaseState: true);
            var result = new TransferResult();
            var plan = PlanOf(new[] { 0f, 0.1f }, new[] { 0f });

            var found = new OrientationSearchResult();
            CoroutineRunner.Drain(
                routine.ApproachAndSearch(plan, result, search => found = search),
                () => isAtFarStand = _positioner.Destinations.Count >= 2);

            Assert.That(found.IsFound, Is.True);
            Assert.That(_positioner.Destinations.Count, Is.EqualTo(2));
            Assert.That(_positioner.Destinations[1].z, Is.EqualTo(0.1f).Within(1e-5f));
        }

        [Test]
        public void FailsWithTheReachDescription_WhenNoPlaceReaches()
        {
            StandSearchRoutine routine = Create(pose => false, new StandSearchSettings(false, 3), withBaseState: true);
            var result = new TransferResult();

            OrientationSearchResult search = Run(routine, PlanOf(new[] { 0f }, new[] { 0f }), result);

            Assert.That(search.IsFound, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.MotionFailed));
            Assert.That(result.Detail, Does.Contain(ReachStep));
            Assert.That(result.Detail, Does.Contain("지점1"));
        }

        [Test]
        public void StopsAtOnce_WhenTheBaseCannotGetThere()
        {
            _positioner.Arrives = false;
            StandSearchRoutine routine = Create(pose => true, new StandSearchSettings(false, 3), withBaseState: true);
            var result = new TransferResult();

            OrientationSearchResult search = Run(routine, PlanOf(new[] { 0f, 0.1f, 0.2f }, new[] { 0f }), result);

            Assert.That(search.IsFound, Is.False);
            Assert.That(_positioner.Destinations.Count, Is.EqualTo(1));
            Assert.That(result.Detail, Does.Contain("막힘"));
        }

        [Test]
        public void PlansInAdvance_AndTellsWhatItIsDoing_WhenThereAreSeveralPlaces()
        {
            StandSearchRoutine routine = Create(pose => true, new StandSearchSettings(true, 3), withBaseState: true);
            var result = new TransferResult();

            OrientationSearchResult search = Run(routine, PlanOf(new[] { 0f, 0.1f }, new[] { 0f, 0.1f }), result);

            Assert.That(search.IsFound, Is.True);
            Assert.That(_logs, Has.Some.Contains("미리 가늠한다"));
            Assert.That(_logs, Has.Some.Contains("가늠 끝"));
            // 기본 자리가 이미 닿으므로 더 먼 자리는 따지지 않고, 기본 자리로 간다.
            Assert.That(_positioner.Destinations, Is.EqualTo(new[] { new Vector3(0f, 0f, 0f) }));
        }

        [Test]
        public void DoesNotPlanInAdvance_WhenTheBaseStateIsUnknown()
        {
            StandSearchRoutine routine = Create(pose => true, new StandSearchSettings(true, 3), withBaseState: false);
            var result = new TransferResult();

            OrientationSearchResult search = Run(routine, PlanOf(new[] { 0f, 0.1f }, new[] { 0f, 0.1f }), result);

            Assert.That(search.IsFound, Is.True);
            Assert.That(_logs, Has.None.Contains("미리 가늠한다"));
        }

        [Test]
        public void DoesNotPlanInAdvance_WhenThereIsOnlyOnePlaceToTry()
        {
            StandSearchRoutine routine = Create(pose => true, new StandSearchSettings(true, 3), withBaseState: true);
            var result = new TransferResult();

            Run(routine, PlanOf(new[] { 0f }, new[] { 0f }), result);

            Assert.That(_logs, Has.None.Contains("미리 가늠한다"));
        }
    }
}
