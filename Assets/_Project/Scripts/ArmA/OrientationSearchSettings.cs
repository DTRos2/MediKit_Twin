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
        [Tooltip("비틀기 1도를 기울이기·좌우 몇 도로 칠지. 비틀기는 칸으로 들어가는 방향을 바꾸지 않아 칸 틀에 걸릴 위험이 거의 없으므로 작게 둔다. 1이면 똑같이 센다.")]
        [SerializeField, Range(0f, 1f)] private float _rollCostWeight = DefaultRollCostWeight;

        /// <summary>비틀기를 기울이기·좌우의 1/10로 센다.</summary>
        public const float DefaultRollCostWeight = 0.1f;

        public OrientationSearchSettings()
        {
        }

        public OrientationSearchSettings(
            float maxPitchDownDeg, float maxPitchUpDeg, float pitchStepDeg,
            float maxYawDeg, float yawStepDeg,
            float maxRollDeg, float rollStepDeg,
            float rollCostWeight = DefaultRollCostWeight)
        {
            _maxPitchDownDeg = maxPitchDownDeg;
            _maxPitchUpDeg = maxPitchUpDeg;
            _pitchStepDeg = pitchStepDeg;
            _maxYawDeg = maxYawDeg;
            _yawStepDeg = yawStepDeg;
            _maxRollDeg = maxRollDeg;
            _rollStepDeg = rollStepDeg;
            _rollCostWeight = rollCostWeight;
        }

        /// <summary>기본 방향 하나만 보는 설정. 이미 알고 있는 방향이 지금도 닿는지만 확인할 때 쓴다(간격이 0이면 그 방향은 바꾸지 않는다).</summary>
        public static OrientationSearchSettings NominalOnly()
        {
            return new OrientationSearchSettings(0f, 0f, 0f, 0f, 0f, 0f, 0f);
        }

        /// <summary>손목 비틀기 범위만 바꾼 복사본. 나머지(기울이기, 좌우 돌리기, 비틀기 비중)는 그대로다.</summary>
        public OrientationSearchSettings WithRoll(float maxRollDeg, float rollStepDeg)
        {
            return new OrientationSearchSettings(
                _maxPitchDownDeg, _maxPitchUpDeg, _pitchStepDeg, _maxYawDeg, _yawStepDeg, maxRollDeg, rollStepDeg, _rollCostWeight);
        }

        public float MaxPitchDownDeg => _maxPitchDownDeg;

        public float MaxPitchUpDeg => _maxPitchUpDeg;

        public float PitchStepDeg => _pitchStepDeg;

        public float MaxYawDeg => _maxYawDeg;

        public float YawStepDeg => _yawStepDeg;

        public float MaxRollDeg => _maxRollDeg;

        public float RollStepDeg => _rollStepDeg;

        public float RollCostWeight => _rollCostWeight;
    }
}
