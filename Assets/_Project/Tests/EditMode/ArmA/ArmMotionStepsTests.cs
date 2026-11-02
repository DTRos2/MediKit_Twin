using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmMotionStepsTests
    {
        private const string Step = "테스트 단계";
        private const float TravelPositionM = 0.03f;
        private const float TravelRotationDeg = 10f;
        private const float PrecisePositionM = 0.01f;
        private const float PreciseRotationDeg = 5f;
        private const float TravelJointDeg = 4f;
        private const float PreciseJointDeg = 1.5f;
        private const float TravelSolvePositionM = 0.015f;
        private const float TravelSolveRotationDeg = 5f;

        private sealed class FakeTool : IToolPoseTarget, IToolSolveToleranceTarget
        {
            internal bool Arrives = true;
            internal bool SolveSucceeds = true;
            internal Pose? TargetAsked;
            internal float PositionToleranceAsked;
            internal float RotationToleranceAsked;
            internal float SolvePositionAsked = -1f;
            internal float SolveRotationAsked = -1f;

            public bool HasToolTarget => TargetAsked.HasValue;
            public bool LastSolveSucceeded => SolveSucceeds;
            public IkResult LastResult => new IkResult(false, 0.2f, 12f, 5);

            public void SetToolTarget(Pose worldPose) => SetToolTarget(worldPose, 0f, 0f);

            public void SetToolTarget(Pose worldPose, float solvePositionToleranceM, float solveRotationToleranceDeg)
            {
                TargetAsked = worldPose;
                SolvePositionAsked = solvePositionToleranceM;
                SolveRotationAsked = solveRotationToleranceDeg;
            }

            public void ClearToolTarget() { }

            public IkResult Probe(Pose worldPose) => default;

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg)
            {
                PositionToleranceAsked = positionToleranceM;
                RotationToleranceAsked = rotationToleranceDeg;
                return Arrives;
            }
        }

        private sealed class FakeJointTarget : IJointPoseTarget
        {
            internal bool Arrives = true;
            internal IReadOnlyList<float> Goal;
            internal float ToleranceAsked;

            public bool SetJointTarget(IReadOnlyList<float> anglesRad)
            {
                Goal = anglesRad;
                return true;
            }

            public bool HasReachedJointTarget(float toleranceRad)
            {
                ToleranceAsked = toleranceRad;
                return Arrives;
            }
        }

        private sealed class FakeBasePositioner : IArmBasePositioner
        {
            internal bool Arrives = true;
            internal Vector3? Destination;
            internal float? FacingAsked;
            internal int Calls;

            public string LastFailureReason => Arrives ? string.Empty : "바퀴가 걸렸다";

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
            {
                Calls++;
                Destination = worldPosition;
                FacingAsked = null;
                done(Arrives);
                yield break;
            }

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
            {
                Calls++;
                Destination = worldPosition;
                FacingAsked = facingYawDeg;
                done(Arrives);
                yield break;
            }
        }

        private FakeTool _tool;
        private FakeJointTarget _jointTarget;
        private FakeBasePositioner _positioner;
        private float _time;

        [SetUp]
        public void SetUp()
        {
            _tool = new FakeTool();
            _jointTarget = new FakeJointTarget();
            _positioner = new FakeBasePositioner();
            _time = 0f;
        }

        private ArmMotionSteps Create(bool withBase = true, bool withJoints = true)
        {
            var diagnostics = new TransferDiagnostics(_tool, null, null, () => false, PrecisePositionM);
            var tolerances = new MotionTolerances(
                TravelPositionM, TravelRotationDeg, PrecisePositionM, PreciseRotationDeg, TravelJointDeg, PreciseJointDeg,
                TravelSolvePositionM, TravelSolveRotationDeg);
            return new ArmMotionSteps(
                withBase ? _positioner : null,
                new ToolPoseMover(_tool, TravelPositionM, TravelRotationDeg, 5f),
                withJoints ? new JointPoseMover(_jointTarget, () => _time, 5f) : null,
                diagnostics,
                tolerances);
        }

        private static bool Run(Func<Action<bool>, IEnumerator> step, Action onStep = null)
        {
            bool result = false;
            CoroutineRunner.Drain(step(done => result = done), onStep);
            return result;
        }

        [Test]
        public void MoveBase_ArrivesWithoutMoving_WhenThereIsNoBasePositioner()
        {
            var result = new TransferResult();

            bool isOk = Run(done => Create(withBase: false).MoveBase(Vector3.one, 10f, Step, result, done));

            Assert.That(isOk, Is.True);
            Assert.That(_positioner.Calls, Is.EqualTo(0));
        }

        [Test]
        public void MoveBase_ArrivesWithoutMoving_WhenThereIsNoDestination()
        {
            var result = new TransferResult();

            bool isOk = Run(done => Create().MoveBase(null, null, Step, result, done));

            Assert.That(isOk, Is.True);
            Assert.That(_positioner.Calls, Is.EqualTo(0));
        }

        [Test]
        public void MoveBase_FacesTheGivenDirection_WhenOneIsGiven()
        {
            var result = new TransferResult();

            bool isOk = Run(done => Create().MoveBase(new Vector3(1f, 0f, 2f), 90f, Step, result, done));

            Assert.That(isOk, Is.True);
            Assert.That(_positioner.Destination, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(_positioner.FacingAsked, Is.EqualTo(90f));
        }

        [Test]
        public void MoveBase_KeepsTheTravelHeading_WhenNoDirectionIsGiven()
        {
            var result = new TransferResult();

            Run(done => Create().MoveBase(Vector3.right, null, Step, result, done));

            Assert.That(_positioner.FacingAsked, Is.Null);
        }

        [Test]
        public void MoveBase_FailsWithTheStepAndTheReason_WhenTheBaseDoesNotArrive()
        {
            _positioner.Arrives = false;
            var result = new TransferResult();

            bool isOk = Run(done => Create().MoveBase(Vector3.right, null, Step, result, done));

            Assert.That(isOk, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.MotionFailed));
            Assert.That(result.Detail, Does.Contain(Step));
            Assert.That(result.Detail, Does.Contain("바퀴가 걸렸다"));
        }

        [Test]
        public void MoveTool_UsesTheLooseTolerance_AndMoveToolPreciselyUsesTheStrictOne()
        {
            var result = new TransferResult();
            var pose = new Pose(Vector3.one, Quaternion.identity);
            ArmMotionSteps steps = Create();

            Run(done => steps.MoveTool(pose, Step, result, done));
            Assert.That(_tool.PositionToleranceAsked, Is.EqualTo(TravelPositionM));
            Assert.That(_tool.RotationToleranceAsked, Is.EqualTo(TravelRotationDeg));

            Run(done => steps.MoveToolPrecisely(pose, Step, result, done));
            Assert.That(_tool.PositionToleranceAsked, Is.EqualTo(PrecisePositionM));
            Assert.That(_tool.RotationToleranceAsked, Is.EqualTo(PreciseRotationDeg));
            Assert.That(_tool.TargetAsked, Is.EqualTo(pose));
        }

        [Test]
        public void MoveTool_AcceptsALooseIkSolve_AndMoveToolPreciselyRequiresAConvergedOne()
        {
            var result = new TransferResult();
            var pose = new Pose(Vector3.one, Quaternion.identity);
            ArmMotionSteps steps = Create();

            Run(done => steps.MoveTool(pose, Step, result, done));
            Assert.That(_tool.SolvePositionAsked, Is.EqualTo(TravelSolvePositionM));
            Assert.That(_tool.SolveRotationAsked, Is.EqualTo(TravelSolveRotationDeg));

            Run(done => steps.MoveToolPrecisely(pose, Step, result, done));
            Assert.That(_tool.SolvePositionAsked, Is.EqualTo(0f), "쥐고 놓는 자리는 IK가 수렴해야만 움직인다");
            Assert.That(_tool.SolveRotationAsked, Is.EqualTo(0f));
        }

        [Test]
        public void MoveTool_AsksForAStrictIkSolve_WhenNoLooseSolveToleranceIsConfigured()
        {
            var diagnostics = new TransferDiagnostics(_tool, null, null, () => false, PrecisePositionM);
            var tolerances = new MotionTolerances(
                TravelPositionM, TravelRotationDeg, PrecisePositionM, PreciseRotationDeg, TravelJointDeg, PreciseJointDeg);
            var steps = new ArmMotionSteps(
                _positioner, new ToolPoseMover(_tool, TravelPositionM, TravelRotationDeg, 5f), null, diagnostics, tolerances);

            Run(done => steps.MoveTool(new Pose(Vector3.one, Quaternion.identity), Step, new TransferResult(), done));

            Assert.That(_tool.SolvePositionAsked, Is.EqualTo(0f));
            Assert.That(_tool.SolveRotationAsked, Is.EqualTo(0f));
        }

        [Test]
        public void MoveTool_FailsWithTheStepAndTheIkError_WhenTheTargetCannotBeReached()
        {
            _tool.Arrives = false;
            _tool.SolveSucceeds = false;
            var result = new TransferResult();

            bool isOk = Run(done => Create().MoveTool(new Pose(Vector3.one, Quaternion.identity), Step, result, done));

            Assert.That(isOk, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.MotionFailed));
            Assert.That(result.Detail, Does.Contain(Step));
            Assert.That(result.Detail, Does.Contain("0.200"));
        }

        [Test]
        public void MoveTravel_GoesByJointsWithTheLooseTolerance_WhenJointsAreGiven()
        {
            var result = new TransferResult();
            var joints = new[] { 0.1f, 0.2f };

            bool isOk = Run(done => Create().MoveTravel(default, joints, Step, result, done), () => _time += 1f);

            Assert.That(isOk, Is.True);
            Assert.That(_jointTarget.Goal, Is.EqualTo(joints));
            Assert.That(_jointTarget.ToleranceAsked, Is.EqualTo(TravelJointDeg * Mathf.Deg2Rad).Within(1e-6f));
            Assert.That(_tool.TargetAsked, Is.Null);
        }

        [Test]
        public void MoveGrasp_GoesByJointsWithTheStrictTolerance_WhenJointsAreGiven()
        {
            var result = new TransferResult();

            Run(done => Create().MoveGrasp(default, new[] { 0.1f }, Step, result, done), () => _time += 1f);

            Assert.That(_jointTarget.ToleranceAsked, Is.EqualTo(PreciseJointDeg * Mathf.Deg2Rad).Within(1e-6f));
        }

        [Test]
        public void MoveTravelAndMoveGrasp_GoByTheToolPose_WhenNoJointsAreGiven()
        {
            var result = new TransferResult();
            var pose = new Pose(Vector3.one, Quaternion.identity);
            ArmMotionSteps steps = Create();

            Run(done => steps.MoveTravel(pose, null, Step, result, done));
            Assert.That(_tool.PositionToleranceAsked, Is.EqualTo(TravelPositionM));

            Run(done => steps.MoveGrasp(pose, null, Step, result, done));
            Assert.That(_tool.PositionToleranceAsked, Is.EqualTo(PrecisePositionM));
            Assert.That(_jointTarget.Goal, Is.Null);
        }

        [Test]
        public void MoveTravel_GoesByTheToolPose_WhenJointsCannotBeUsed()
        {
            var result = new TransferResult();
            var pose = new Pose(Vector3.one, Quaternion.identity);

            Run(done => Create(withJoints: false).MoveTravel(pose, new[] { 0.1f }, Step, result, done));

            Assert.That(_tool.TargetAsked, Is.EqualTo(pose));
        }

        [Test]
        public void MoveTravel_FailsWithTheStep_WhenTheJointsDoNotArriveInTime()
        {
            _jointTarget.Arrives = false;
            var result = new TransferResult();

            bool isOk = Run(done => Create().MoveTravel(default, new[] { 0.1f }, Step, result, done), () => _time += 1f);

            Assert.That(isOk, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TransferFailure.MotionFailed));
            Assert.That(result.Detail, Does.Contain(Step));
            Assert.That(result.Detail, Does.Contain("관절 각도"));
        }
    }
}
