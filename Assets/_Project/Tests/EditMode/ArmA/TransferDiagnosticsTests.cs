using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TransferDiagnosticsTests
    {
        private const float PositionToleranceM = 0.01f;

        private sealed class FakeTool : IToolPoseTarget
        {
            internal bool SolveSucceeded;
            internal IkResult Last;
            internal IkResult ProbeResult;

            public bool HasToolTarget => false;
            public bool LastSolveSucceeded => SolveSucceeded;
            public IkResult LastResult => Last;

            public void SetToolTarget(Pose worldPose) { }

            public void ClearToolTarget() { }

            public IkResult Probe(Pose worldPose) => ProbeResult;

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg) => false;
        }

        private sealed class FakeTracking : IToolTrackingDiagnostics
        {
            internal string Tracking = string.Empty;
            internal string Deviation = string.Empty;

            public string DescribeTracking() => Tracking;

            public string DescribeJointDeviation(IReadOnlyList<float> referenceRad) => Deviation;
        }

        private sealed class FakeFrame : IToolReachFrame
        {
            public Vector3 OffsetFromArmBase(Vector3 worldPoint) => new Vector3(0.3f, -0.2f, 0.4f);
        }

        private FakeTool _tool;
        private bool _isHeld;

        [SetUp]
        public void SetUp()
        {
            _tool = new FakeTool();
            _isHeld = false;
        }

        private TransferDiagnostics Create(IToolTrackingDiagnostics tracking = null, IToolReachFrame frame = null)
        {
            return new TransferDiagnostics(_tool, tracking, frame, () => _isHeld, PositionToleranceM);
        }

        [Test]
        public void BlamesTheManualHold_WhenTheArmIsHeld()
        {
            _isHeld = true;
            TransferDiagnostics diagnostics = Create();

            Assert.That(diagnostics.ToolFailure(), Does.Contain("수동 제어"));
            Assert.That(diagnostics.JointFailure(new[] { 0f }), Does.Contain("수동 제어"));
        }

        [Test]
        public void ReportsTheIkError_WhenNoReachingPoseWasFound()
        {
            _tool.SolveSucceeded = false;
            _tool.Last = new IkResult(false, 0.123f, 17f, 10);

            string message = Create().ToolFailure();

            Assert.That(message, Does.Contain("0.123"));
            Assert.That(message, Does.Contain("17.0"));
        }

        [Test]
        public void ReportsATrackingProblem_WhenTheSolveWorkedButTheArmDidNotArrive()
        {
            _tool.SolveSucceeded = true;
            var tracking = new FakeTracking { Tracking = "관절3이 한계에 붙음" };

            string message = Create(tracking).ToolFailure();

            Assert.That(message, Does.Contain("시간 안에"));
            Assert.That(message, Does.Contain("관절3이 한계에 붙음"));
        }

        [Test]
        public void StillExplains_WhenThereIsNoTrackingHelper()
        {
            _tool.SolveSucceeded = true;

            Assert.That(Create().ToolFailure(), Does.Contain("시간 안에"));
        }

        [Test]
        public void NamesTheDeviatingJoint_WhenTheJointTargetWasMissed()
        {
            var tracking = new FakeTracking { Deviation = "J2 +12도" };

            string message = Create(tracking).JointFailure(new[] { 0f });

            Assert.That(message, Does.Contain("J2 +12도"));
        }

        [Test]
        public void DeviationIsEmpty_WithoutATrackingHelper()
        {
            Assert.That(Create().JointDeviation(new[] { 0f }), Is.Empty);
        }

        [Test]
        public void ReachSaysTooFarToTurn_WhenOnlyTheJointMoveLimitBlocksIt()
        {
            _tool.ProbeResult = new IkResult(false, 0.005f, 3f, 10);

            string message = Create().Reach(new[] { Vector3.zero }, Quaternion.identity);

            Assert.That(message, Does.Contain("너무 멀리"));
        }

        [Test]
        public void ReachShowsTheErrorAndPlacement_ForEachPoint()
        {
            _tool.ProbeResult = new IkResult(false, 0.40f, 30f, 10);

            string message = Create(null, new FakeFrame()).Reach(new[] { Vector3.zero, Vector3.one }, Quaternion.identity);

            Assert.That(message, Does.Contain("지점1"));
            Assert.That(message, Does.Contain("지점2"));
            Assert.That(message, Does.Contain("0.40m"));
            Assert.That(message, Does.Contain("수평 0.50m"));
            Assert.That(message, Does.Contain("-0.20m"));
        }
    }
}
