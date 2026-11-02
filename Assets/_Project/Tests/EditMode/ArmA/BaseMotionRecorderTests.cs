using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BaseMotionRecorderTests
    {
        private const float Step = 0.02f;
        private static readonly Pose Tool = new Pose(Vector3.zero, Quaternion.identity);

        private sealed class FakeSteps : IPhysicsStepSource
        {
            public event Action Stepped;

            public int ListenerCount => Stepped == null ? 0 : Stepped.GetInvocationList().Length;

            public void Step()
            {
                Stepped?.Invoke();
            }
        }

        private sealed class FakeMobile : IMobileBase
        {
            public Vector3 Position { get; set; }

            public float YawDeg { get; set; }

            public bool IsMoving => false;

            public event Action Arrived;

            public void DriveTo(Vector3 worldPosition)
            {
            }

            public void DriveTo(Vector3 worldPosition, float finalYawDeg)
            {
            }

            public void Stop()
            {
            }

            internal void Arrive()
            {
                Arrived?.Invoke();
            }
        }

        private readonly FakeSteps _steps = new FakeSteps();
        private readonly FakeMobile _mobile = new FakeMobile();
        private readonly List<string> _log = new List<string>();
        private float _now;
        private float _heldX;
        private bool _canMeasure;

        [SetUp]
        public void SetUp()
        {
            _log.Clear();
            _now = 0f;
            _heldX = 0f;
            _canMeasure = true;
            _mobile.Position = Vector3.zero;
            _mobile.YawDeg = 0f;
        }

        private BaseMotionRecorder Create(float onsetSlipM = 0.02f)
        {
            var probe = new BaseMotionProbe(_steps, _mobile, () => _now, onsetSlipM);
            var diagnostics = new BoxPlaceDiagnostics(pose => new IkResult(true, 0f, 0f, 1));
            return new BaseMotionRecorder(probe, MeasureHeld, diagnostics, _log.Add);
        }

        private HeldItemFrame? MeasureHeld()
        {
            return _canMeasure
                ? HeldItemFrame.Measure(Tool, new Vector3(_heldX, 0f, 0.04f), Quaternion.identity)
                : (HeldItemFrame?)null;
        }

        // 한 스텝 흐른다: 시간이 가고 베이스가 dx만큼 가고 물건이 heldX로 옮겨진 뒤 스텝 알림이 나간다.
        private void Advance(float baseDx, float heldX)
        {
            _now += Step;
            _mobile.Position += new Vector3(baseDx, 0f, 0f);
            _heldX = heldX;
            _steps.Step();
        }

        [Test]
        public void ADriveThatSlipsWhileMoving_LogsASummaryAndATrace()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Advance(0.024f, 0f);
            Advance(0.024f, -0.015f);
            Advance(0.024f, -0.035f);
            recorder.End(arrived: true);

            Assert.That(_log.Count, Is.EqualTo(2));
            Assert.That(_log[0], Does.StartWith("박스 진단: 베이스 이동 [1번째] 도착"));
            Assert.That(_log[0], Does.Contain("7.2cm 이동"));
            Assert.That(_log[0], Does.Contain("밀림 최대 3.5cm"));
            Assert.That(_log[0], Does.Contain("베이스가 움직이는 중"));
            Assert.That(_log[1], Does.StartWith("박스 진단: 베이스 이동 [1번째] 추이"));
            Assert.That(_log[1], Does.Contain("밀림(cm) [0.0 0.0 1.5 3.5"));
        }

        [Test]
        public void ASlipThatGrowsAfterTheBaseStops_IsDescribedAsAfterTheStop()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Advance(0.024f, 0f);
            Advance(0f, -0.01f);
            Advance(0f, -0.03f);
            recorder.End(arrived: true);

            Assert.That(_log[0], Does.Contain("베이스가 멈추고"));
            Assert.That(_log[0], Does.Not.Contain("움직이는 중"));
        }

        [Test]
        public void AMoveWhereNothingMovesAndNothingSlips_LogsOnlyTheSummary()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Advance(0f, 0f);
            Advance(0f, 0f);
            recorder.End(arrived: true);

            Assert.That(_log.Count, Is.EqualTo(1));
            Assert.That(_log[0], Does.Contain("베이스는 움직이지 않았다"));
            Assert.That(_log[0], Does.Contain("넘지 않았다"));
        }

        [Test]
        public void TheSlipThresholdIsConfigurable()
        {
            BaseMotionRecorder recorder = Create(onsetSlipM: 0.05f);

            recorder.Begin();
            Advance(0.024f, -0.03f);
            recorder.End(arrived: true);

            Assert.That(_log[0], Does.Contain("5.0cm를 넘지 않았다"));
        }

        [Test]
        public void EndWithoutBegin_DoesNothing()
        {
            BaseMotionRecorder recorder = Create();

            recorder.End(arrived: true);

            Assert.That(_log, Is.Empty);
            Assert.That(recorder.IsRecording, Is.False);
        }

        [Test]
        public void BeginningAgainBeforeEnding_ClosesTheFirstAsNotArrived_AndCountsTheSecond()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Advance(0.024f, 0f);
            recorder.Begin();
            Advance(0.024f, 0f);
            recorder.End(arrived: true);

            Assert.That(_log.Exists(line => line.Contains("[1번째] 도착하지 못하고 끝남")), Is.True);
            Assert.That(_log.Exists(line => line.Contains("[2번째] 도착,")), Is.True);
        }

        [Test]
        public void AfterEnding_StopsListeningToSteps()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Assert.That(_steps.ListenerCount, Is.EqualTo(1));
            recorder.End(arrived: true);

            Assert.That(_steps.ListenerCount, Is.EqualTo(0));
            int logged = _log.Count;
            Advance(0.1f, -0.1f);
            Assert.That(_log.Count, Is.EqualTo(logged));
            Assert.That(recorder.IsRecording, Is.False);
        }

        [Test]
        public void BeginningAgain_DoesNotListenTwice()
        {
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            recorder.Begin();

            Assert.That(_steps.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void WhenTheHeldFrameCannotBeMeasured_StillLogsTheBaseMotionWithoutSlip()
        {
            _canMeasure = false;
            BaseMotionRecorder recorder = Create();

            recorder.Begin();
            Advance(0.024f, 0f);
            recorder.End(arrived: true);

            Assert.That(_log[0], Does.Contain("2.4cm 이동"));
            Assert.That(_log[0], Does.Contain("밀림 최대 0.0cm"));
        }

        [Test]
        public void TheNumberingRestartsWithANewRecorder()
        {
            Create().Begin();
            _steps.Step();
            var second = Create();
            second.Begin();
            second.End(arrived: true);

            Assert.That(_log.Exists(line => line.Contains("[1번째]")), Is.True);
            Assert.That(_log.Exists(line => line.Contains("[2번째]")), Is.False);
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            var probe = new BaseMotionProbe(_steps, _mobile, () => _now, 0.02f);
            var diagnostics = new BoxPlaceDiagnostics(pose => default);

            Assert.Throws<ArgumentNullException>(() => new BaseMotionRecorder(null, MeasureHeld, diagnostics, _log.Add));
            Assert.Throws<ArgumentNullException>(() => new BaseMotionRecorder(probe, null, diagnostics, _log.Add));
            Assert.Throws<ArgumentNullException>(() => new BaseMotionRecorder(probe, MeasureHeld, null, _log.Add));
            Assert.Throws<ArgumentNullException>(() => new BaseMotionRecorder(probe, MeasureHeld, diagnostics, null));
        }

        [Test]
        public void Probe_NullDependency_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new BaseMotionProbe(null, _mobile, () => _now, 0.02f));
            Assert.Throws<ArgumentNullException>(() => new BaseMotionProbe(_steps, null, () => _now, 0.02f));
            Assert.Throws<ArgumentNullException>(() => new BaseMotionProbe(_steps, _mobile, null, 0.02f));
        }
    }
}
