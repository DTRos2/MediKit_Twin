using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Mobile
{
    /// <summary>
    /// 로봇 루트를 바닥 위에서 목표 지점으로 굴린다. 차동 구동처럼 목표 쪽으로 제자리에서 돌고 나서 전진한다.
    /// 최종 방향을 함께 받으면 도착한 뒤 제자리에서 그 방향으로 돌아선다(루트에서 떨어진 팔 베이스를 정확히 놓기 위해서다).
    /// 하역 로봇(robot_boxy)의 궤도 애니메이션은 이 컴포넌트가 직접 다루지 않는다. 하역 로봇의 GridRobotController가
    /// 월드 위치 변화량으로 궤도를 돌리므로, 이 루트를 움직이면 바퀴와 궤도가 따라서 돈다.
    /// 시간은 Time.deltaTime이라 SimClock 배속이 그대로 적용된다.
    /// </summary>
    public class MobileBaseDriver : MonoBehaviour, IMobileBase
    {
        [Tooltip("전진 속도(m/s).")]
        [SerializeField, Min(0.05f)] private float _moveSpeed = 1.2f;

        [Tooltip("제자리 회전 속도(도/초).")]
        [SerializeField, Min(10f)] private float _turnSpeedDegPerSec = 180f;

        [Tooltip("목표에서 이 거리 안이면 도착으로 본다.")]
        [SerializeField, Min(0.001f)] private float _arriveToleranceM = 0.02f;

        [Tooltip("목표 방향과 이 각도 안으로 맞으면 회전을 멈추고 전진한다.")]
        [SerializeField, Min(0.5f)] private float _alignToleranceDeg = 3f;

        private Vector3 _target;
        private bool _hasTarget;
        private float _finalYawDeg;
        private bool _hasFinalYaw;

        public event Action Arrived;

        public bool IsMoving => _hasTarget;

        public Vector3 Position => transform.position;

        public float YawDeg => transform.eulerAngles.y;

        public void DriveTo(Vector3 worldPosition)
        {
            _target = worldPosition;
            _hasFinalYaw = false;
            _hasTarget = true;
        }

        public void DriveTo(Vector3 worldPosition, float finalYawDeg)
        {
            _target = worldPosition;
            _finalYawDeg = finalYawDeg;
            _hasFinalYaw = true;
            _hasTarget = true;
        }

        public void Stop()
        {
            _hasTarget = false;
        }

        private void Update()
        {
            if (!_hasTarget)
            {
                return;
            }

            Vector3 toTarget = _target - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= _arriveToleranceM)
            {
                FinishAtTarget();
                return;
            }

            float currentYaw = transform.eulerAngles.y;
            float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float yawError = Mathf.DeltaAngle(currentYaw, desiredYaw);

            if (Mathf.Abs(yawError) > _alignToleranceDeg)
            {
                TurnInPlace(currentYaw, yawError);
                return;
            }

            float step = Mathf.Min(_moveSpeed * Time.deltaTime, distance);
            transform.position += toTarget / distance * step;
        }

        // 위치에 닿았다. 최종 방향이 있으면 그 방향으로 돌아서고 정확히 맞춘 뒤에 도착을 알린다.
        private void FinishAtTarget()
        {
            if (_hasFinalYaw)
            {
                float currentYaw = transform.eulerAngles.y;
                float yawError = Mathf.DeltaAngle(currentYaw, _finalYawDeg);

                if (Mathf.Abs(yawError) > _alignToleranceDeg)
                {
                    TurnInPlace(currentYaw, yawError);
                    return;
                }

                // 남은 각도만큼의 어긋남이 팔 베이스의 위치 오차가 되므로 정확한 방향으로 맞춘다.
                transform.rotation = Quaternion.Euler(0f, _finalYawDeg, 0f);
            }

            _hasTarget = false;
            Arrived?.Invoke();
        }

        private void TurnInPlace(float currentYaw, float yawError)
        {
            float maxTurn = _turnSpeedDegPerSec * Time.deltaTime;
            float turn = Mathf.Clamp(yawError, -maxTurn, maxTurn);
            transform.rotation = Quaternion.Euler(0f, currentYaw + turn, 0f);
        }
    }
}
