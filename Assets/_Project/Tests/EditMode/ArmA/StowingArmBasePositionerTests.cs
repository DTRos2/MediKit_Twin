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

            internal FakeInner(List<string> calls) => _calls = calls;

            public string LastFailureReason => "안쪽 이유";

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
            {
                _calls.Add("move");
                Target = worldPosition;
                done(true);
                yield break;
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
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StowingArmBasePositioner(null, new FakeStower(_calls, true)));
            Assert.Throws<ArgumentNullException>(() => new StowingArmBasePositioner(new FakeInner(_calls), null));
        }
    }
}
