using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StowingArmBasePositionerTests
    {
        private readonly List<string> _calls = new List<string>();

        // NUnit은 한 클래스의 테스트들이 같은 인스턴스를 쓰므로, 앞 테스트가 남긴 호출 기록을 비워야 한다.
        [SetUp]
        public void ClearCalls()
        {
            _calls.Clear();
        }

        private sealed class FakeStower : IArmStower
        {
            private readonly List<string> _calls;
            private readonly bool _result;

            internal FakeStower(List<string> calls, bool result)
            {
                _calls = calls;
                _result = result;
            }

            public IEnumerator Stow(Action<bool> done)
            {
                _calls.Add("stow");
                done(_result);
                yield break;
            }
        }

        private sealed class FakeInner : IArmBasePositioner
        {
            private readonly List<string> _calls;

            internal Vector3 Target;
            internal bool Arrives = true;

            internal FakeInner(List<string> calls) => _calls = calls;

            public string LastFailureReason => "안쪽 이유";

            internal float? FacingYawDeg;

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
            {
                _calls.Add("move");
                Target = worldPosition;
                done(Arrives);
                yield break;
            }

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
            {
                FacingYawDeg = facingYawDeg;
                return MoveArmBaseTo(worldPosition, done);
            }
        }

        private bool Move(IArmStower stower, FakeInner inner, Vector3 target)
        {
            var positioner = new StowingArmBasePositioner(inner, stower);
            bool result = false;
            CoroutineRunner.Drain(positioner.MoveArmBaseTo(target, done => result = done), null);
            return result;
        }

        [Test]
        public void StowsFirst_ThenMovesTheBaseToTheTarget()
        {
            var inner = new FakeInner(_calls);
            var target = new Vector3(3f, 0f, 2f);

            bool result = Move(new FakeStower(_calls, true), inner, target);

            Assert.That(result, Is.True);
            Assert.That(_calls, Is.EqualTo(new[] { "stow", "move" }));
            Assert.That(inner.Target, Is.EqualTo(target));
        }

        [Test]
        public void MovingWithAFacing_StowsFirst_ThenPassesTheFacingToTheInnerPositioner()
        {
            var inner = new FakeInner(_calls);
            var positioner = new StowingArmBasePositioner(inner, new FakeStower(_calls, true));
            bool result = false;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.one, 45f, done => result = done), null);

            Assert.That(result, Is.True);
            Assert.That(_calls, Is.EqualTo(new[] { "stow", "move" }));
            Assert.That(inner.FacingYawDeg, Is.EqualTo(45f));
        }

        [Test]
        public void WhenStowingFails_DoesNotMoveTheBaseAndFails()
        {
            var inner = new FakeInner(_calls);

            bool result = Move(new FakeStower(_calls, false), inner, Vector3.one);

            Assert.That(result, Is.False);
            Assert.That(_calls, Is.EqualTo(new[] { "stow" }));
        }

        [Test]
        public void LastFailureReason_NamesTheStowFailure_ThenFallsBackToTheInnerReason()
        {
            var inner = new FakeInner(_calls);

            var failing = new StowingArmBasePositioner(inner, new FakeStower(_calls, false));
            CoroutineRunner.Drain(failing.MoveArmBaseTo(Vector3.zero, _ => { }), null);
            Assert.That(failing.LastFailureReason, Does.Contain("팔을 접지"));

            var passing = new StowingArmBasePositioner(inner, new FakeStower(_calls, true));
            CoroutineRunner.Drain(passing.MoveArmBaseTo(Vector3.zero, _ => { }), null);
            Assert.That(passing.LastFailureReason, Is.EqualTo("안쪽 이유"));
        }

        [Test]
        public void ReportsStowedThenBaseArrived_InThatOrder_WhenTheMoveSucceeds()
        {
            var phases = new List<ArmMotionPhase>();
            var positioner = new StowingArmBasePositioner(new FakeInner(_calls), new FakeStower(_calls, true));
            positioner.PhaseReached += phases.Add;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.one, _ => { }), null);

            Assert.That(phases, Is.EqualTo(new[] { ArmMotionPhase.Stowed, ArmMotionPhase.BaseArrived }));
        }

        [Test]
        public void ReportsNothing_WhenStowingFails()
        {
            var phases = new List<ArmMotionPhase>();
            var positioner = new StowingArmBasePositioner(new FakeInner(_calls), new FakeStower(_calls, false));
            positioner.PhaseReached += phases.Add;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.one, _ => { }), null);

            Assert.That(phases, Is.Empty);
        }

        [Test]
        public void ReportsStowedButNotBaseArrived_WhenTheBaseDoesNotArrive()
        {
            var phases = new List<ArmMotionPhase>();
            bool result = true;
            var positioner = new StowingArmBasePositioner(new FakeInner(_calls) { Arrives = false }, new FakeStower(_calls, true));
            positioner.PhaseReached += phases.Add;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.one, done => result = done), null);

            Assert.That(result, Is.False);
            Assert.That(phases, Is.EqualTo(new[] { ArmMotionPhase.Stowed }));
        }

        [Test]
        public void TheFacingOverloadReportsTheSamePhases()
        {
            var phases = new List<ArmMotionPhase>();
            var positioner = new StowingArmBasePositioner(new FakeInner(_calls), new FakeStower(_calls, true));
            positioner.PhaseReached += phases.Add;

            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.one, 90f, _ => { }), null);

            Assert.That(phases, Is.EqualTo(new[] { ArmMotionPhase.Stowed, ArmMotionPhase.BaseArrived }));
        }

        [Test]
        public void MovingWithoutAnyListener_StillWorks()
        {
            bool result = Move(new FakeStower(_calls, true), new FakeInner(_calls), Vector3.one);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StowingArmBasePositioner(null, new FakeStower(_calls, true)));
            Assert.Throws<ArgumentNullException>(() => new StowingArmBasePositioner(new FakeInner(_calls), null));
        }
    }
}
