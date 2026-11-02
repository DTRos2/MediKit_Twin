using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class JointPoseMoverTests
    {
        private const float Tolerance = 0.03f;
        private const float TimeoutSeconds = 10f;

        private float _time;
        private FakeJointTarget _target;

        [SetUp]
        public void SetUp()
        {
            _time = 0f;
            _target = new FakeJointTarget();
        }

        private sealed class FakeJointTarget : IJointPoseTarget
        {
            internal IReadOnlyList<float> Goal;
            internal float ToleranceAsked;
            internal int StepsUntilReached = 3;
            internal bool NeverReaches;
            internal bool RefusesTheGoal;

            public bool SetJointTarget(IReadOnlyList<float> anglesRad)
            {
                if (RefusesTheGoal)
                {
                    return false;
                }

                Goal = anglesRad;
                return true;
            }

            public bool HasReachedJointTarget(float toleranceRad)
            {
                ToleranceAsked = toleranceRad;
                return !NeverReaches && StepsUntilReached <= 0;
            }

            internal void Step() => StepsUntilReached--;
        }

        private bool Move(float[] goal)
        {
            var mover = new JointPoseMover(_target, () => _time, TimeoutSeconds);
            bool result = false;

            CoroutineRunner.Drain(mover.MoveTo(goal, Tolerance, done => result = done), () =>
            {
                _time += 1f;
                _target.Step();
            });
            return result;
        }

        [Test]
        public void SendsTheGoal_AndSucceedsWhenTheJointsGetThere()
        {
            var goal = new[] { 0.1f, -0.5f, 1.2f };

            bool result = Move(goal);

            Assert.That(result, Is.True);
            Assert.That(_target.Goal, Is.EqualTo(goal));
        }

        [Test]
        public void AsksWhetherTheJointsAreWithinTheGivenTolerance()
        {
            Move(new[] { 0f });

            Assert.That(_target.ToleranceAsked, Is.EqualTo(Tolerance));
        }

        [Test]
        public void WhenAlreadyThere_SucceedsWithoutWaiting()
        {
            _target.StepsUntilReached = 0;

            bool result = Move(new[] { 0f });

            Assert.That(result, Is.True);
            Assert.That(_time, Is.EqualTo(0f));
        }

        [Test]
        public void WhenTheJointsNeverGetThere_TimesOutAndFails()
        {
            _target.NeverReaches = true;

            bool result = Move(new[] { 0f });

            Assert.That(result, Is.False);
            Assert.That(_time, Is.GreaterThan(TimeoutSeconds));
        }

        [Test]
        public void WhenTheGoalIsRefused_FailsAtOnceWithoutWaiting()
        {
            _target.RefusesTheGoal = true;

            bool result = Move(new[] { 0f });

            Assert.That(result, Is.False);
            Assert.That(_time, Is.EqualTo(0f), "받을 수 없는 목표는 기다려도 소용없다");
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new JointPoseMover(null, () => 0f, 1f));
            Assert.Throws<ArgumentNullException>(() => new JointPoseMover(_target, null, 1f));
        }
    }
}
