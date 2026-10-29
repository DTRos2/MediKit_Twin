using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>
    /// 정책 출력(-1~1)을 로봇 명령으로 바꾼다. 앞의 N개는 관절 각도 변화량(최대 각속도 x 결정 주기에 비례),
    /// 그리퍼가 있으면 마지막 1개로 열기(&lt;0)/닫기(&gt;=0)를 정한다. 정책이 관절 한계나 속도 제한을 알 필요가 없다.
    /// </summary>
    public sealed class ArmActionInterpreter
    {
        private readonly IArmState _state;
        private readonly IArmDriver _driver;
        private readonly IGripper _gripper;
        private readonly int _jointCount;
        private readonly float[] _angles;
        private readonly float[] _lower;
        private readonly float[] _upper;

        public ArmActionInterpreter(IArmState state, IArmDriver driver, IGripper gripper)
        {
            _state = state;
            _driver = driver;
            _gripper = gripper;
            _jointCount = state.JointNames.Count;
            _angles = new float[_jointCount];
            _lower = new float[_jointCount];
            _upper = new float[_jointCount];
        }

        public int Size => _jointCount + (_gripper != null ? 1 : 0);

        /// <param name="decisionSeconds">행동 하나가 유지되는 시뮬레이션 시간. 이 시간 동안 최대 각속도로 움직일 때의 변화량이 행동 1이다.</param>
        public void Apply(float[] action, float decisionSeconds)
        {
            _state.GetJointPositions(_angles);
            _state.GetJointLimits(_lower, _upper);

            float maxDelta = _driver.MaxJointSpeedRadPerSec * decisionSeconds;
            for (int i = 0; i < _jointCount; i++)
            {
                float delta = Mathf.Clamp(action[i], -1f, 1f) * maxDelta;
                _angles[i] = Mathf.Clamp(_angles[i] + delta, _lower[i], _upper[i]);
            }

            _driver.SetJointTargets(_angles);

            if (_gripper != null)
            {
                if (action[_jointCount] >= 0f)
                {
                    _gripper.Close();
                }
                else
                {
                    _gripper.Open();
                }
            }
        }

        /// <summary>행동 크기(관절 부분의 평균 절댓값). 보상의 행동 감점에 쓴다.</summary>
        public float Magnitude(float[] action)
        {
            float sum = 0f;
            for (int i = 0; i < _jointCount; i++)
            {
                sum += Mathf.Abs(action[i]);
            }

            return sum / _jointCount;
        }
    }
}
