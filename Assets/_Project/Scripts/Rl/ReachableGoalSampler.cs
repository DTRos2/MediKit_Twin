using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>
    /// 무작위 관절 각도를 순기구학으로 바꿔서 목표를 만든다. 목표가 반드시 로봇이 닿을 수 있는 위치가 되므로
    /// 닿지 않는 목표 때문에 학습 신호가 오염되지 않는다. 시드를 받는 System.Random을 써서 같은 시드면 같은 목표열이 나온다.
    /// </summary>
    public class ReachableGoalSampler : MonoBehaviour, IGoalSampler
    {
        [Tooltip("IArmState와 IForwardKinematics를 구현하는 로봇(ArmController).")]
        [SerializeField] private MonoBehaviour _arm;

        [SerializeField] private int _seed = 12345;

        [Tooltip("관절 한계의 이 비율 안쪽에서만 뽑는다. 1이면 한계 전체.")]
        [SerializeField, Range(0.1f, 1f)] private float _rangeFraction = 0.6f;

        [Tooltip("목표가 바닥 아래로 가지 않게 하는 월드 최소 높이.")]
        [SerializeField] private float _minWorldHeight = 0.05f;

        private System.Random _random;
        private IArmState _state;
        private IForwardKinematics _kinematics;
        private float[] _angles;
        private float[] _lower;
        private float[] _upper;

        private void Start()
        {
            _state = _arm as IArmState;
            _kinematics = _arm as IForwardKinematics;
            if (_state == null || _kinematics == null)
            {
                Debug.LogError($"{name}: Arm이 IArmState와 IForwardKinematics를 구현해야 한다.", this);
                enabled = false;
                return;
            }

            int count = _state.JointNames.Count;
            _angles = new float[count];
            _lower = new float[count];
            _upper = new float[count];
            _random = new System.Random(_seed);
        }

        public Vector3 Sample()
        {
            _state.GetJointLimits(_lower, _upper);

            Vector3 position = Vector3.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                for (int i = 0; i < _angles.Length; i++)
                {
                    float center = 0.5f * (_lower[i] + _upper[i]);
                    float half = 0.5f * (_upper[i] - _lower[i]) * _rangeFraction;
                    _angles[i] = center + (float)(_random.NextDouble() * 2.0 - 1.0) * half;
                }

                position = _kinematics.ComputeToolPose(_angles).position;
                if (position.y >= _minWorldHeight)
                {
                    break;
                }
            }

            return position;
        }
    }
}
