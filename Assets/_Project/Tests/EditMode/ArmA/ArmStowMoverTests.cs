using System;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ArmStowMoverTests
    {
        private const float Tolerance = 0.05f;
        private const float TimeoutSeconds = 10f;

        private float _time;
        private FakeTool _tool;
        private FakeArm _arm;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _tool = new FakeTool();
            _arm = new FakeArm();
        }

        private sealed class FakeTool : IToolPoseTarget
        {
            internal int ClearCount;

            public bool HasToolTarget => false;
            public bool LastSolveSucceeded => true;
            public IkResult LastResult => default;

            public void SetToolTarget(Pose worldPose) { }

            public void ClearToolTarget() => ClearCount++;

            public IkResult Probe(Pose worldPose) => default;

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg) => false;
        }

        private sealed class FakeArm : IArmDriver
        {
            internal float[] Goal;
            internal int StepsUntilAtTarget = 3;
            internal bool NeverArrives;
            internal int ClearCountWhenGoalSet = -1;
            internal FakeTool Tool;

            public float MaxJointSpeedRadPerSec => 1f;

            public float[] HomePoseRad { get; } = { 0.1f, -0.5f, 1.2f };

            public void SetJointTargets(float[] anglesRad)
            {
                Goal = anglesRad;
                ClearCountWhenGoalSet = Tool != null ? Tool.ClearCount : -1;
            }

            public bool IsAtTarget(float toleranceRad) => !NeverArrives && StepsUntilAtTarget <= 0;

            public void ResetToPose(float[] anglesRad) { }

            internal void Step() => StepsUntilAtTarget--;
        }

        private bool Stow()
        {
            _arm.Tool = _tool;
            var mover = new ArmStowMover(_tool, _arm, () => _time, Tolerance, TimeoutSeconds);
            bool result = false;

            CoroutineRunner.Drain(mover.Stow(done => result = done), () =>
            {
                _time += 1f;
                _arm.Step();
            });
            return result;
        }

        [Test]
        public void SendsTheArmToItsHomePose_AndSucceedsWhenItGetsThere()
        {
            bool result = Stow();

            Assert.That(result, Is.True);
            Assert.That(_arm.Goal, Is.EqualTo(_arm.HomePoseRad));
        }

        [Test]
        public void ClearsTheToolTargetFirst_SoIkDoesNotOverrideTheJointGoal()
        {
            Stow();

            Assert.That(_tool.ClearCount, Is.EqualTo(1));
            Assert.That(_arm.ClearCountWhenGoalSet, Is.EqualTo(1), "관절 목표를 주기 전에 도구 목표를 지워야 한다");
        }

        [Test]
        public void WhenTheArmNeverGetsThere_TimesOutAndFails()
        {
            _arm.NeverArrives = true;

            bool result = Stow();

            Assert.That(result, Is.False);
            Assert.That(_time, Is.GreaterThan(TimeoutSeconds));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ArmStowMover(null, _arm, () => 0f, Tolerance, 1f));
            Assert.Throws<ArgumentNullException>(() => new ArmStowMover(_tool, null, () => 0f, Tolerance, 1f));
            Assert.Throws<ArgumentNullException>(() => new ArmStowMover(_tool, _arm, null, Tolerance, 1f));
        }
    }
}
