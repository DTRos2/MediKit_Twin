using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 방향을 기본 자세에서 얼마나, 얼마 간격으로 바꿔 가며 닿는 자세를 찾을지 정한 값. 각도는 모두 도(°)다.
    /// 기울이기(pitch)는 도구 끝이 아래로 향하는 쪽이 양수, 좌우 돌리기(yaw)는 도구 위쪽 축 기준, 손목 비틀기(roll)는 접근 축 기준이다.
    /// 간격이 0이면 그 방향은 바꾸지 않는다.
    /// </summary>
    [Serializable]
    public sealed class OrientationSearchSettings
    {
        [Tooltip("도구 끝이 아래로 기울어질 수 있는 최대 각도.")]
        [SerializeField, Min(0f)] private float _maxPitchDownDeg = 45f;
        [Tooltip("도구 끝이 위로 들릴 수 있는 최대 각도. 선반 칸은 위가 막혀 있으니 작게 둔다.")]
        [SerializeField, Min(0f)] private float _maxPitchUpDeg = 15f;
        [Tooltip("기울이기를 바꾸는 간격. 0이면 기울이지 않는다.")]
        [SerializeField, Min(0f)] private float _pitchStepDeg = 15f;

        [Tooltip("좌우로 돌릴 수 있는 최대 각도(한쪽).")]
        [SerializeField, Min(0f)] private float _maxYawDeg = 30f;
        [Tooltip("좌우 돌리기를 바꾸는 간격. 0이면 돌리지 않는다.")]
        [SerializeField, Min(0f)] private float _yawStepDeg = 15f;

        [Tooltip("손목을 비틀 수 있는 최대 각도(한쪽).")]
        [SerializeField, Min(0f)] private float _maxRollDeg = 90f;
        [Tooltip("손목 비틀기를 바꾸는 간격. 0이면 비틀지 않는다.")]
        [SerializeField, Min(0f)] private float _rollStepDeg = 90f;

        public OrientationSearchSettings()
        {
        }

        public OrientationSearchSettings(
            float maxPitchDownDeg, float maxPitchUpDeg, float pitchStepDeg,
            float maxYawDeg, float yawStepDeg,
            float maxRollDeg, float rollStepDeg)
        {
            _maxPitchDownDeg = maxPitchDownDeg;
            _maxPitchUpDeg = maxPitchUpDeg;
            _pitchStepDeg = pitchStepDeg;
            _maxYawDeg = maxYawDeg;
            _yawStepDeg = yawStepDeg;
            _maxRollDeg = maxRollDeg;
            _rollStepDeg = rollStepDeg;
        }

        public float MaxPitchDownDeg => _maxPitchDownDeg;

        public float MaxPitchUpDeg => _maxPitchUpDeg;

        public float PitchStepDeg => _pitchStepDeg;

        public float MaxYawDeg => _maxYawDeg;

        public float YawStepDeg => _yawStepDeg;

        public float MaxRollDeg => _maxRollDeg;

        public float RollStepDeg => _rollStepDeg;
    }
}
