using System;
using System.Collections;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class ToolPoseMoverTests
    {
        private const float TolerancePositionM = 0.03f;
        private const float ToleranceRotationDeg = 10f;
        private const float SolvePositionM = 0.015f;
        private const float SolveRotationDeg = 5f;
        private const float TimeoutSeconds = 5f;

        // 풀이 기준을 받지 못하는 도구.
        private class PlainTool : IToolPoseTarget
        {
            internal bool Arrives = true;
            internal bool SolveSucceeds = true;
            internal Pose? TargetAsked;
            internal int PlainCalls;

            public bool HasToolTarget => TargetAsked.HasValue;
            public bool LastSolveSucceeded => SolveSucceeds;
            public IkResult LastResult => default;

            public void SetToolTarget(Pose worldPose)
            {
                PlainCalls++;
                TargetAsked = worldPose;
            }

            public void ClearToolTarget() { }

            public IkResult Probe(Pose worldPose) => default;

            public bool HasReachedTarget(float positionToleranceM, float rotationToleranceDeg) => Arrives;
        }

        // 풀이 기준을 목표와 함께 받는 도구.
        private sealed class LooseTool : PlainTool, IToolSolveToleranceTarget
        {
            internal int LooseCalls;
            internal float SolvePositionAsked = -1f;
            internal float SolveRotationAsked = -1f;

            public void SetToolTarget(Pose worldPose, float solvePositionToleranceM, float solveRotationToleranceDeg)
            {
                LooseCalls++;
                TargetAsked = worldPose;
                SolvePositionAsked = solvePositionToleranceM;
                SolveRotationAsked = solveRotationToleranceDeg;
            }
        }

        // MoveTo는 결과를 콜백으로 알린다. 코루틴을 끝까지 돌려 그 값을 돌려준다.
        private static bool Move(Func<Action<bool>, IEnumerator> step)
        {
            bool result = false;
            CoroutineRunner.Drain(step(done => result = done), null);
            return result;
        }

        private static ToolPoseMover Create(IToolPoseTarget tool)
        {
            return new ToolPoseMover(tool, TolerancePositionM, ToleranceRotationDeg, TimeoutSeconds);
        }

        [Test]
        public void MoveTo_GivesTheSolveToleranceWithTheTarget_WhenTheToolTakesOne()
        {
            var tool = new LooseTool();
            var pose = new Pose(Vector3.one, Quaternion.identity);
            ToolPoseMover mover = Create(tool);

            bool isReached = Move(done => mover.MoveTo(
                pose, TolerancePositionM, ToleranceRotationDeg, SolvePositionM, SolveRotationDeg, done));

            Assert.That(isReached, Is.True);
            Assert.That(tool.LooseCalls, Is.EqualTo(1));
            Assert.That(tool.PlainCalls, Is.EqualTo(0));
            Assert.That(tool.TargetAsked, Is.EqualTo(pose));
            Assert.That(tool.SolvePositionAsked, Is.EqualTo(SolvePositionM));
            Assert.That(tool.SolveRotationAsked, Is.EqualTo(SolveRotationDeg));
        }

        [Test]
        public void MoveTo_AsksForAStrictSolve_WhenNoSolveToleranceIsGiven()
        {
            var tool = new LooseTool();
            ToolPoseMover mover = Create(tool);

            Move(done => mover.MoveTo(Pose.identity, TolerancePositionM, ToleranceRotationDeg, done));
            Assert.That(tool.SolvePositionAsked, Is.EqualTo(0f));
            Assert.That(tool.SolveRotationAsked, Is.EqualTo(0f));

            tool.SolvePositionAsked = -1f;
            Move(done => mover.MoveTo(Pose.identity, done));
            Assert.That(tool.SolvePositionAsked, Is.EqualTo(0f), "허용 오차를 따로 주지 않는 이동도 엄격하다");
        }

        [Test]
        public void MoveTo_GivesThePlainTarget_WhenTheToolCannotTakeASolveTolerance()
        {
            var tool = new PlainTool();
            var pose = new Pose(Vector3.one, Quaternion.identity);
            ToolPoseMover mover = Create(tool);

            bool isReached = Move(done => mover.MoveTo(
                pose, TolerancePositionM, ToleranceRotationDeg, SolvePositionM, SolveRotationDeg, done));

            Assert.That(isReached, Is.True);
            Assert.That(tool.PlainCalls, Is.EqualTo(1));
            Assert.That(tool.TargetAsked, Is.EqualTo(pose));
        }

        [Test]
        public void MoveTo_Fails_WhenTheSolveIsNotAcceptedAndTheToolDoesNotArrive()
        {
            var tool = new LooseTool { Arrives = false, SolveSucceeds = false };
            ToolPoseMover mover = Create(tool);

            bool isReached = Move(done => mover.MoveTo(
                Pose.identity, TolerancePositionM, ToleranceRotationDeg, SolvePositionM, SolveRotationDeg, done));

            Assert.That(isReached, Is.False);
        }
    }
}
