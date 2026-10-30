using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ScopedArmBasePositionerTests
    {
        private readonly List<string> _calls = new List<string>();

        private sealed class RecordingScope : IMotionScope
        {
            private readonly List<string> _calls;

            internal RecordingScope(List<string> calls) => _calls = calls;

            public void Begin() => _calls.Add("begin");

            public void End() => _calls.Add("end");
        }

        private sealed class FakeInner : IArmBasePositioner
        {
            private readonly List<string> _calls;
            private readonly bool _result;

            public string LastFailureReason => "안쪽 이유";

            internal FakeInner(List<string> calls, bool result)
            {
                _calls = calls;
                _result = result;
            }

            public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
            {
                _calls.Add("move");
                yield return null;
                done(_result);
            }
        }

        private bool Move(FakeInner inner)
        {
            var positioner = new ScopedArmBasePositioner(inner, new RecordingScope(_calls));
            bool result = false;
            CoroutineRunner.Drain(positioner.MoveArmBaseTo(Vector3.zero, done => result = done), null);
            return result;
        }

        [Test]
        public void TurnsTheScopeOnOnlyWhileMoving()
        {
            bool result = Move(new FakeInner(_calls, true));

            Assert.That(result, Is.True);
            Assert.That(_calls, Is.EqualTo(new[] { "begin", "move", "end" }));
        }

        [Test]
        public void WhenTheMoveFails_StillEndsTheScopeAndPassesTheResultThrough()
        {
            bool result = Move(new FakeInner(_calls, false));

            Assert.That(result, Is.False);
            Assert.That(_calls, Is.EqualTo(new[] { "begin", "move", "end" }));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ScopedArmBasePositioner(null, new RecordingScope(_calls)));
            Assert.Throws<ArgumentNullException>(() => new ScopedArmBasePositioner(new FakeInner(_calls, true), null));
        }
    }
}
