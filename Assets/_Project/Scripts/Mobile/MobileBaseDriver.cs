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
    public class MobileBaseDriver : MonoBehaviour, IMobileBase, IManualPlacement
    {
        [Tooltip("전진 속도(m/s).")]
        [SerializeField, Min(0.05f)] private float _moveSpeed = 1.2f;

        [Tooltip("제자리 회전 속도(도/초).")]
        [SerializeField, Min(10f)] private float _turnSpeedDegPerSec = 180f;

        [Tooltip("목표에서 이 거리 안이면 도착으로 본다.")]
        [SerializeField, Min(0.001f)] private float _arriveToleranceM = 0.02f;

        [Tooltip("목표 방향과 이 각도 안으로 맞으면 회전을 멈추고 전진한다.")]
        [SerializeField, Min(0.5f)] private float _alignToleranceDeg = 3f;

        [Tooltip("전진할 때 속도를 올리고 내리는 가속도(m/s²). 0이면 가감속 없이 바로 전진 속도로 달린다. " +
                 "속도가 한 프레임에 계단처럼 바뀌면 베이스 위의 팔이 흔들리고 쥔 물건이 도구에서 출렁인다.")]
        [SerializeField, Min(0f)] private float _moveAccelerationMps2 = 2f;

        [Tooltip("제자리에서 돌 때 속도를 올리고 내리는 가속도(도/초²). 0이면 가감속 없이 바로 회전 속도로 돈다.")]
        [SerializeField, Min(0f)] private float _turnAccelerationDegPerSec2 = 720f;

        [Tooltip("도착한 뒤 최종 방향으로 돌아설 때 이 각도 안이면 맞은 것으로 보고 정확한 방향으로 맞춘다. " +
                 "작을수록 마지막에 한 번에 틀어지는 각도가 작아 팔이 덜 튄다.")]
        [SerializeField, Min(0.05f)] private float _finalYawToleranceDeg = 0.5f;

        // 허용 오차의 이 비율만큼 안쪽을 멈출 자리로 삼아 감속한다. 허용 오차 경계에서 속도가 0이 되면 경계에 닿기 전에
        // 멈춰 서 버릴 수 있고, 경계에서 빠르면 도착 처리 때 속도가 한꺼번에 사라진다. 그 사이에서 천천히 들어가게 한 값이다.
        private const float StopMargin = 0.9f;

        private SpeedRamp _moveRamp;
        private SpeedRamp _turnRamp;
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
            ResetRamps();
        }

        // 진행 중이던 이동은 취소하고(도착을 알리지 않는다) 곧바로 이 자리와 방향에 놓는다. 높이는 그대로 둔다.
        public void PlaceAt(Vector3 worldPosition, float yawDeg)
        {
            _hasTarget = false;
            ResetRamps();
            transform.SetPositionAndRotation(
                new Vector3(worldPosition.x, transform.position.y, worldPosition.z),
                Quaternion.Euler(0f, yawDeg, 0f));
        }

        // 인스펙터에서 바꾼 속도와 가속도로 만든다. 실행 중에 값을 바꿔도 다음 실행부터 반영된다.
        private void Awake()
        {
            _moveRamp = new SpeedRamp(_moveSpeed, _moveAccelerationMps2);
            _turnRamp = new SpeedRamp(_turnSpeedDegPerSec, _turnAccelerationDegPerSec2);
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
                TurnInPlace(currentYaw, yawError, _alignToleranceDeg);
                return;
            }

            // 도착 허용 오차 안쪽에서 멈추도록 남은 거리를 잡는다. 서는 자리가 프레임 간격에 따라 달라지지 않는다.
            _turnRamp.Reset();
            float step = _moveRamp.Advance(distance - _arriveToleranceM * StopMargin, Time.deltaTime);
            transform.position += toTarget / distance * step;
        }

        // 위치에 닿았다. 최종 방향이 있으면 그 방향으로 돌아서고 정확히 맞춘 뒤에 도착을 알린다.
        private void FinishAtTarget()
        {
            if (_hasFinalYaw)
            {
                float currentYaw = transform.eulerAngles.y;
                float yawError = Mathf.DeltaAngle(currentYaw, _finalYawDeg);

                if (Mathf.Abs(yawError) > _finalYawToleranceDeg)
                {
                    TurnInPlace(currentYaw, yawError, _finalYawToleranceDeg);
                    return;
                }

                // 남은 각도만큼의 어긋남이 팔 베이스의 위치 오차가 되므로 정확한 방향으로 맞춘다.
                transform.rotation = Quaternion.Euler(0f, _finalYawDeg, 0f);
            }

            _hasTarget = false;
            ResetRamps();
            Arrived?.Invoke();
        }

        // 허용 오차 안쪽에서 멈추도록 감속하며 돈다. 돌고 있는 동안에는 전진 속도를 0으로 둔다.
        private void TurnInPlace(float currentYaw, float yawError, float toleranceDeg)
        {
            _moveRamp.Reset();
            float turn = _turnRamp.Advance(Mathf.Abs(yawError) - toleranceDeg * StopMargin, Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, currentYaw + Mathf.Sign(yawError) * turn, 0f);
        }

        private void ResetRamps()
        {
            _moveRamp?.Reset();
            _turnRamp?.Reset();
        }
    }
}
