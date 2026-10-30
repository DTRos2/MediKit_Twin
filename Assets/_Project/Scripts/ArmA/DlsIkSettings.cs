using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>감쇠 최소제곱(DLS) 역기구학 설정.</summary>
    [Serializable]
    public class DlsIkSettings
    {
        [SerializeField, Min(1)] private int _maxIterations = 60;
        [SerializeField, Min(0f)] private float _positionToleranceM = 0.002f;
        [SerializeField, Min(0f)] private float _rotationToleranceDeg = 2f;

        [Tooltip("클수록 특이점 근처에서 안정적이지만 수렴이 느리다.")]
        [SerializeField, Min(0.0001f)] private float _damping = 0.05f;

        [Tooltip("자세 오차 반영 비율. 0이면 위치만 맞춘다.")]
        [SerializeField, Range(0f, 1f)] private float _rotationWeight = 0.5f;

        [Tooltip("한 번 반복에서 관절이 움직일 수 있는 최대 각도.")]
        [SerializeField, Min(0.1f)] private float _maxStepDeg = 10f;

        [Tooltip("수렴하지 못하면 무작위 자세에서 다시 푸는 횟수. 0이면 한 번만 푼다.")]
        [SerializeField, Min(0)] private int _restarts = 10;

        [Tooltip("재시도 시작 자세를 뽑는 난수 시드. 같은 시드면 같은 결과가 나온다.")]
        [SerializeField] private int _restartSeed = 2024;

        [Tooltip("관절별 움직임 비용(관절 순서). 클수록 그 관절은 덜 움직이고 나머지 관절이 더 맡는다. " +
                 "손목 비용을 키우면 어깨와 팔꿈치까지 함께 움직인다. 비우거나 관절 수와 다르면 모두 같은 비용(1)으로 푼다.")]
        [SerializeField] private float[] _jointCosts = Array.Empty<float>();


        public int Restarts => _restarts;
        public int RestartSeed => _restartSeed;
        public int MaxIterations => _maxIterations;
        public float PositionToleranceM => _positionToleranceM;
        public float RotationToleranceDeg => _rotationToleranceDeg;
        public float Damping => _damping;
        public float RotationWeight => _rotationWeight;
        public float MaxStepDeg => _maxStepDeg;

        /// <summary>관절 index의 움직임 비용. 설정이 없거나 관절 수와 맞지 않으면 1이다. 0 이하 값은 풀이가 깨지므로 아주 작은 양수로 막는다.</summary>
        public float JointCost(int index, int jointCount)
        {
            if (_jointCosts == null || _jointCosts.Length != jointCount)
            {
                return 1f;
            }

            return Mathf.Max(_jointCosts[index], MinJointCost);
        }

        private const float MinJointCost = 0.01f;
    }
}
