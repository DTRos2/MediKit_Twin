using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 이송 순서가 쓰는 팔 동작 한 단계씩: 베이스 옮기기, 도구 끝점 보내기, 관절 각도로 보내기.
    /// 단계마다 지나가는 자리는 느슨한 오차로, 쥐고 놓는 자리는 엄격한 오차로 판정한다. 닿지 못하면 단계 이름과 이유를
    /// 결과(TransferResult)에 남기고 done(false)를 부른다. 어떤 단계를 어떤 순서로 할지는 모르고(ArmItemTransfer 등의 몫),
    /// 이유를 어떻게 설명할지도 모른다(TransferDiagnostics의 몫).
    /// 베이스 이동 모듈이 없으면(고정 팔) 베이스는 이동 없이 도착한 것으로 본다.
    /// </summary>
    public sealed class ArmMotionSteps
    {
        private readonly IArmBasePositioner _basePositioner;
        private readonly ToolPoseMover _toolMover;
        private readonly JointPoseMover _jointMover;
        private readonly TransferDiagnostics _diagnostics;
        private readonly MotionTolerances _tolerances;

        /// <param name="basePositioner">팔 베이스를 옮기는 쪽. 고정 팔이면 null.</param>
        /// <param name="jointMover">관절 각도로 보내는 쪽. 쓸 수 없으면 null이고, 그러면 관절 각도 목표도 도구 끝점으로 간다.</param>
        public ArmMotionSteps(
            IArmBasePositioner basePositioner,
            ToolPoseMover toolMover,
            JointPoseMover jointMover,
            TransferDiagnostics diagnostics,
            MotionTolerances tolerances)
        {
            _basePositioner = basePositioner;
            _toolMover = toolMover ?? throw new ArgumentNullException(nameof(toolMover));
            _jointMover = jointMover;
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _tolerances = tolerances;
        }

        /// <summary>팔 베이스를 standPoint로 옮긴다. 방향이 주어지면 도착한 뒤 그 방향으로 선다. 옮길 곳이 없으면 이동 없이 도착한 것으로 본다.</summary>
        public IEnumerator MoveBase(Vector3? standPoint, float? facingYawDeg, string step, TransferResult result, Action<bool> done)
        {
            if (_basePositioner == null || !standPoint.HasValue)
            {
                done(true);
                yield break;
            }

            bool isArrived = false;
            yield return facingYawDeg.HasValue
                ? _basePositioner.MoveArmBaseTo(standPoint.Value, facingYawDeg.Value, arrived => isArrived = arrived)
                : _basePositioner.MoveArmBaseTo(standPoint.Value, arrived => isArrived = arrived);
            if (!isArrived)
            {
                result.Fail(TransferFailure.MotionFailed, $"{step}: 베이스가 서는 자리에 도착하지 못했다 ({_basePositioner.LastFailureReason})");
            }

            done(isArrived);
        }

        /// <summary>지나가는 자리(접근, 후퇴, 들어올리기): 도구 끝점을 느슨한 오차로 보낸다. IK 풀이도 느슨한 오차까지 받아 준다.</summary>
        public IEnumerator MoveTool(Pose target, string step, TransferResult result, Action<bool> done)
        {
            return MoveToolWithin(
                target, _tolerances.TravelPositionM, _tolerances.TravelRotationDeg,
                _tolerances.TravelSolvePositionM, _tolerances.TravelSolveRotationDeg, step, result, done);
        }

        /// <summary>쥐고 놓는 자리: 도구 끝점을 엄격한 오차로 보낸다. IK도 수렴해야 한다.</summary>
        public IEnumerator MoveToolPrecisely(Pose target, string step, TransferResult result, Action<bool> done)
        {
            return MoveToolWithin(target, _tolerances.PrecisePositionM, _tolerances.PreciseRotationDeg, 0f, 0f, step, result, done);
        }

        /// <summary>지나가는 자리: 관절 각도가 있으면 그 각도로, 없으면 도구 끝점 자세로 간다.</summary>
        public IEnumerator MoveTravel(Pose target, float[] jointsRad, string step, TransferResult result, Action<bool> done)
        {
            return jointsRad != null && _jointMover != null
                ? MoveJoints(jointsRad, _tolerances.TravelJointDeg, step, result, done)
                : MoveTool(target, step, result, done);
        }

        /// <summary>쥐는 자리: 관절 각도가 있으면 그 각도로, 없으면 도구 끝점 자세로 간다.</summary>
        public IEnumerator MoveGrasp(Pose target, float[] jointsRad, string step, TransferResult result, Action<bool> done)
        {
            return jointsRad != null && _jointMover != null
                ? MoveJoints(jointsRad, _tolerances.PreciseJointDeg, step, result, done)
                : MoveToolPrecisely(target, step, result, done);
        }

        // 도구 끝점을 목표로 보낸다. 방향은 미리 닿는 것을 확인했으므로, 여기서 실패하면 무언가에 걸렸거나 시간이 모자란 것이다.
        private IEnumerator MoveToolWithin(
            Pose target, float positionToleranceM, float rotationToleranceDeg,
            float solvePositionToleranceM, float solveRotationToleranceDeg, string step, TransferResult result, Action<bool> done)
        {
            bool isReached = false;
            yield return _toolMover.MoveTo(
                target, positionToleranceM, rotationToleranceDeg, solvePositionToleranceM, solveRotationToleranceDeg,
                reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed, $"{step}: {_diagnostics.ToolFailure()}");
            }

            done(isReached);
        }

        // 관절 각도로 곧바로 보낸다.
        private IEnumerator MoveJoints(float[] anglesRad, float toleranceDeg, string step, TransferResult result, Action<bool> done)
        {
            bool isReached = false;
            yield return _jointMover.MoveTo(anglesRad, toleranceDeg * Mathf.Deg2Rad, reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed, $"{step}: {_diagnostics.JointFailure(anglesRad)}");
            }

            done(isReached);
        }
    }
}
