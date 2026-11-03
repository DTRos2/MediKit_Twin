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

        [Tooltip("허용 오차의 이 비율만큼 안쪽을 멈출 자리로 삼아 감속한다. 허용 오차 경계에서 속도가 0이 되면 경계에 닿기 전에 멈춰 서 버릴 수 있고, " +
                 "경계에서 빠르면 도착 처리 때 속도가 한꺼번에 사라진다. 1이면 경계에서 멈추고, 작을수록 안쪽에서 멈춘다.")]
        [SerializeField, Range(0.1f, 1f)] private float _stopMarginRatio = 0.9f;

        private DifferentialDrive _drive;
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
            _drive?.Reset();
        }

        // 진행 중이던 이동은 취소하고(도착을 알리지 않는다) 곧바로 이 자리와 방향에 놓는다. 높이는 그대로 둔다.
        public void PlaceAt(Vector3 worldPosition, float yawDeg)
        {
            _hasTarget = false;
            _drive?.Reset();
            transform.SetPositionAndRotation(
                new Vector3(worldPosition.x, transform.position.y, worldPosition.z),
                Quaternion.Euler(0f, yawDeg, 0f));
        }

        // 인스펙터에서 바꾼 속도, 가속도, 허용 오차로 만든다. 실행 중에 값을 바꿔도 다음 실행부터 반영된다.
        private void Awake()
        {
            _drive = new DifferentialDrive(
                new SpeedRamp(_moveSpeed, _moveAccelerationMps2),
                new SpeedRamp(_turnSpeedDegPerSec, _turnAccelerationDegPerSec2),
                new DriveTolerances(_arriveToleranceM, _alignToleranceDeg, _finalYawToleranceDeg, _stopMarginRatio));
        }

        // 무엇을 할지는 DifferentialDrive가 정하고, 이 컴포넌트는 정한 대로 Transform을 옮기고 도착을 알리기만 한다.
        private void Update()
        {
            if (!_hasTarget)
            {
                return;
            }

            float? finalYaw = _hasFinalYaw ? _finalYawDeg : (float?)null;
            DriveStep step = _drive.Step(transform.position, transform.eulerAngles.y, _target, finalYaw, Time.deltaTime);

            switch (step.Action)
            {
                case DriveAction.Turn:
                    transform.rotation = Quaternion.Euler(0f, step.YawDeg, 0f);
                    break;
                case DriveAction.Move:
                    transform.position += step.Translation;
                    break;
                case DriveAction.Arrive:
                    FinishAtTarget(step);
                    break;
            }
        }

        private void FinishAtTarget(DriveStep step)
        {
            if (step.SnapsYaw)
            {
                transform.rotation = Quaternion.Euler(0f, step.YawDeg, 0f);
            }

            _hasTarget = false;
            Arrived?.Invoke();
        }
    }
}
