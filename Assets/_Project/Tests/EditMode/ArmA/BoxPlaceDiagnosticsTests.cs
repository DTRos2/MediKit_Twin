using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BoxPlaceDiagnosticsTests
    {
        private readonly List<Pose> _probed = new List<Pose>();
        private Func<Pose, IkResult> _answer;

        [SetUp]
        public void SetUp()
        {
            _probed.Clear();
            _answer = pose => new IkResult(true, 0.0005f, 0.2f, 3);
        }

        private BoxPlaceDiagnostics Create()
        {
            return new BoxPlaceDiagnostics(pose =>
            {
                _probed.Add(pose);
                return _answer(pose);
            });
        }

        // 도구 앞(+z)으로 4cm 떨어진 곳에 물건 중심이 있는 상태.
        private static HeldItemFrame HeldFourCmAhead()
        {
            return HeldItemFrame.Measure(new Pose(Vector3.zero, Quaternion.identity), new Vector3(0f, 0f, 0.04f), Quaternion.identity);
        }

        [Test]
        public void TheHeldFrameReportsHowFarTheItemCenterIsFromTheTool()
        {
            string text = Create().DescribeHeldFrame(HeldFourCmAhead());

            Assert.That(text, Does.Contain("(0.0, 0.0, 4.0)cm"));
            Assert.That(text, Does.Contain("거리는 4.0cm"));
        }

        [Test]
        public void ThePlanReportsNoShiftWhenTheToolPointsTheSameWay()
        {
            HeldItemFrame frame = HeldFourCmAhead();
            PlacePath path = PlacePath.For(frame, Vector3.zero, Quaternion.identity, 0.1f, 0.1f);

            string text = Create().DescribePlan(path, path, Quaternion.identity);

            Assert.That(text, Does.Contain("슬롯 위 0.0cm"));
            Assert.That(text, Does.Contain("슬롯 0.0cm"));
            Assert.That(text, Does.Contain("들어올림 0.0cm"));
        }

        [Test]
        public void TurningTheToolMovesTheToolSpotByTheHeldOffset()
        {
            HeldItemFrame frame = HeldFourCmAhead();
            PlacePath tested = PlacePath.For(frame, Vector3.zero, Quaternion.identity, 0.1f, 0.1f);
            Quaternion turned = Quaternion.Euler(0f, 90f, 0f);
            PlacePath executed = PlacePath.For(frame, Vector3.zero, turned, 0.1f, 0.1f);

            string text = Create().DescribePlan(tested, executed, turned);

            // 4cm 앞이던 물건 중심이 90도 돌면 4cm 옆이 되므로, 도구 자리는 (4, 0, -4)cm 차이 = 5.7cm 달라진다.
            Assert.That(text, Does.Contain("슬롯 위 5.7cm"));
            Assert.That(text, Does.Contain("슬롯 5.7cm"));
            Assert.That(text, Does.Contain("들어올림 5.7cm"));
        }

        [Test]
        public void ThePlanSolvesBothSetsOfSpotsWithTheFoundRotation()
        {
            HeldItemFrame frame = HeldFourCmAhead();
            Quaternion turned = Quaternion.Euler(0f, 90f, 0f);
            PlacePath tested = PlacePath.For(frame, Vector3.zero, Quaternion.identity, 0.1f, 0.1f);
            PlacePath executed = PlacePath.For(frame, Vector3.zero, turned, 0.1f, 0.1f);

            Create().DescribePlan(tested, executed, turned);

            // 가늠한 자리 3곳 + 실행할 자리 3곳을 모두 찾은 방향으로 푼다.
            Assert.That(_probed.Count, Is.EqualTo(6));
            Assert.That(_probed.TrueForAll(pose => Quaternion.Angle(pose.rotation, turned) < 1e-3f), Is.True);
            Assert.That(Vector3.Distance(_probed[0].position, tested.Above), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(_probed[3].position, executed.Above), Is.LessThan(1e-5f));
        }

        [Test]
        public void AnUnconvergedSolveIsReportedInMillimetersSoANearMissIsVisible()
        {
            _answer = pose => new IkResult(false, 0.0023f, 0.1f, 60);
            PlacePath path = PlacePath.For(HeldFourCmAhead(), Vector3.zero, Quaternion.identity, 0.1f, 0.1f);

            string text = Create().DescribePlan(path, path, Quaternion.identity);

            Assert.That(text, Does.Contain("수렴 못함 위치 2.3mm/회전 0.1도"));
        }

        [Test]
        public void AConvergedSolveIsReportedAsConverged()
        {
            PlacePath path = PlacePath.For(HeldFourCmAhead(), Vector3.zero, Quaternion.identity, 0.1f, 0.1f);

            string text = Create().DescribePlan(path, path, Quaternion.identity);

            Assert.That(text, Does.Contain("슬롯 위: 수렴 위치 0.5mm/회전 0.2도"));
        }

        [Test]
        public void ASteadyGripReportsNoSlipAtThePoint()
        {
            var trail = new HeldSlipTrail(HeldFourCmAhead());

            string text = Create().DescribeSlipMark(SlipPoints.BoxSide, trail.Mark(HeldFourCmAhead()));

            Assert.That(text, Does.Contain($"[{SlipPoints.BoxSide}]"));
            Assert.That(text, Does.Contain("직전 지점 뒤로 0.0cm 0.0도"));
            Assert.That(text, Does.Contain("(0.0cm) 0.0도"));
        }

        [Test]
        public void ASlippingItemReportsTheStepAndTheTotalSeparately()
        {
            var tool = new Pose(Vector3.zero, Quaternion.identity);
            var trail = new HeldSlipTrail(HeldItemFrame.Measure(tool, new Vector3(0f, 0f, 0.04f), Quaternion.identity));
            trail.Mark(HeldItemFrame.Measure(tool, new Vector3(0.01f, 0f, 0.04f), Quaternion.identity));

            SlipMark second = trail.Mark(HeldItemFrame.Measure(tool, new Vector3(0.03f, 0f, 0.04f), Quaternion.Euler(0f, 0f, 20f)));
            string text = Create().DescribeSlipMark(SlipPoints.AboveSlot, second);

            Assert.That(text, Does.Contain("직전 지점 뒤로 2.0cm 20.0도"), "앞 지점(1cm)에서 2cm 더 밀렸다");
            Assert.That(text, Does.Contain("(3.0, 0.0, 0.0)cm(3.0cm) 20.0도"), "처음에서는 3cm 밀렸다");
        }

        [Test]
        public void TheReleaseReportsHowFarTheItemAndTheToolAreFromTheirTargets()
        {
            string text = Create().DescribeRelease(
                new Vector3(0f, 1f, 0f), new Vector3(-0.046f, 1.004f, -0.077f), new Vector3(0f, 1f, 0f), new Vector3(0.03f, 1f, 0.04f));

            Assert.That(text, Does.Contain("(-4.6, 0.4, -7.7)cm"));
            Assert.That(text, Does.Contain("도구는 목표에서 5.0cm"));
        }

        [Test]
        public void WithoutAProbe_NothingCanBeReported()
        {
            Assert.Throws<ArgumentNullException>(() => new BoxPlaceDiagnostics(null));
        }
    }
}
