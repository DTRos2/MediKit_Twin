using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>
    /// 관측 벡터를 만든다. 로봇 종류를 모르고 IArmState만 읽으므로 FR5, UR3 어느 쪽에도 같은 정책 입력 형식을 쓴다.
    /// 구성: 관절 각도(한계로 -1~1 정규화) N, 관절 속도(최대 속도로 정규화) N, 도구→목표 벡터 3, 거리 1, 그리퍼 개폐 1, 쥠 1.
    /// </summary>
    public sealed class ArmObservationBuilder
    {
        private readonly IArmState _state;
        private readonly float _maxJointSpeedRadPerSec;
        private readonly int _jointCount;
        private readonly float[] _positions;
        private readonly float[] _velocities;
        private readonly float[] _lower;
        private readonly float[] _upper;

        public ArmObservationBuilder(IArmState state, float maxJointSpeedRadPerSec)
        {
            _state = state;
            _maxJointSpeedRadPerSec = Mathf.Max(maxJointSpeedRadPerSec, 1e-3f);
            _jointCount = state.JointNames.Count;
            _positions = new float[_jointCount];
            _velocities = new float[_jointCount];
            _lower = new float[_jointCount];
            _upper = new float[_jointCount];
        }

        public int Size => 2 * _jointCount + 3 + 1 + 2;

        public void Build(Vector3 goalPosition, IGripper gripper, float[] observation)
        {
            _state.GetJointPositions(_positions);
            _state.GetJointVelocities(_velocities);
            _state.GetJointLimits(_lower, _upper);

            int index = 0;
            for (int i = 0; i < _jointCount; i++)
            {
                float range = Mathf.Max(_upper[i] - _lower[i], 1e-3f);
                observation[index++] = 2f * (_positions[i] - _lower[i]) / range - 1f;
            }

            for (int i = 0; i < _jointCount; i++)
            {
                observation[index++] = Mathf.Clamp(_velocities[i] / _maxJointSpeedRadPerSec, -1f, 1f);
            }

            Vector3 toGoal = goalPosition - _state.ToolPose.position;
            observation[index++] = toGoal.x;
            observation[index++] = toGoal.y;
            observation[index++] = toGoal.z;
            observation[index++] = toGoal.magnitude;
            observation[index++] = gripper != null ? gripper.Opening : 1f;
            observation[index] = gripper != null && gripper.IsHolding ? 1f : 0f;
        }
    }
}
