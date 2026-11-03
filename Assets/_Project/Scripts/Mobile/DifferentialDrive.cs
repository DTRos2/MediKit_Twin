using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Mobile
{
    /// <summary>
    /// 차동 구동처럼 목표 쪽으로 제자리에서 돌고 나서 전진하고, 도착하면 최종 방향으로 돌아서는 판단. 위치와 방향을 받아 이번 프레임에 할 일(DriveStep)을
    /// 돌려줄 뿐 아무것도 옮기지 않는다. 속도는 SpeedRamp가 가속도 한계 안에서 올리고 내리며, 돌 때는 전진 속도를, 나아갈 때는 회전 속도를 0으로 둔다.
    /// 바닥 평면(x, z)만 본다. 씬 없이 시험한다.
    /// </summary>
    public sealed class DifferentialDrive
    {
        private readonly SpeedRamp _moveRamp;
        private readonly SpeedRamp _turnRamp;
        private readonly DriveTolerances _tolerances;

        public DifferentialDrive(SpeedRamp moveRamp, SpeedRamp turnRamp, DriveTolerances tolerances)
        {
            _moveRamp = moveRamp ?? throw new ArgumentNullException(nameof(moveRamp));
            _turnRamp = turnRamp ?? throw new ArgumentNullException(nameof(turnRamp));
            _tolerances = tolerances;
        }

        /// <param name="position">지금 월드 위치. 높이(y)는 보지 않는다.</param>
        /// <param name="yawDeg">지금 바라보는 방향(도).</param>
        /// <param name="target">가려는 월드 위치.</param>
        /// <param name="finalYawDeg">도착한 뒤 맞출 방향(도). 없으면 도착한 방향 그대로 둔다.</param>
        /// <param name="deltaTime">지난 시간(초).</param>
        public DriveStep Step(Vector3 position, float yawDeg, Vector3 target, float? finalYawDeg, float deltaTime)
        {
            Vector3 toTarget = target - position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= _tolerances.ArriveM)
            {
                return FinishAtTarget(yawDeg, finalYawDeg, deltaTime);
            }

            float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float yawError = Mathf.DeltaAngle(yawDeg, desiredYaw);

            if (Mathf.Abs(yawError) > _tolerances.AlignDeg)
            {
                return TurnInPlace(yawDeg, yawError, _tolerances.AlignDeg, deltaTime);
            }

            // 도착 허용 오차 안쪽에서 멈추도록 남은 거리를 잡는다. 서는 자리가 프레임 간격에 따라 달라지지 않는다.
            _turnRamp.Reset();
            float remaining = distance - _tolerances.ArriveM * _tolerances.StopMarginRatio;
            float travel = _moveRamp.Advance(remaining, deltaTime);
            return DriveStep.Move(toTarget / distance * travel);
        }

        /// <summary>두 속도를 멈춘 것으로 되돌린다. 이동을 멈추거나 취소하거나 다른 자리에 놓을 때 부른다.</summary>
        public void Reset()
        {
            _moveRamp.Reset();
            _turnRamp.Reset();
        }

        // 위치에 닿았다. 최종 방향이 있으면 그 방향으로 돌아서고, 허용 오차 안으로 들어오면 정확히 맞추며 도착을 알린다.
        private DriveStep FinishAtTarget(float yawDeg, float? finalYawDeg, float deltaTime)
        {
            if (!finalYawDeg.HasValue)
            {
                Reset();
                return DriveStep.Arrive();
            }

            float yawError = Mathf.DeltaAngle(yawDeg, finalYawDeg.Value);
            if (Mathf.Abs(yawError) > _tolerances.FinalYawDeg)
            {
                return TurnInPlace(yawDeg, yawError, _tolerances.FinalYawDeg, deltaTime);
            }

            // 남은 각도만큼의 어긋남이 팔 베이스의 위치 오차가 되므로 정확한 방향으로 맞춘다.
            Reset();
            return DriveStep.ArriveAndSnapYaw(finalYawDeg.Value);
        }

        // 허용 오차 안쪽에서 멈추도록 감속하며 돈다. 돌고 있는 동안에는 전진 속도를 0으로 둔다.
        private DriveStep TurnInPlace(float yawDeg, float yawError, float toleranceDeg, float deltaTime)
        {
            _moveRamp.Reset();
            float remaining = Mathf.Abs(yawError) - toleranceDeg * _tolerances.StopMarginRatio;
            float turn = _turnRamp.Advance(remaining, deltaTime);
            return DriveStep.Turn(yawDeg + Mathf.Sign(yawError) * turn);
        }
    }
}
