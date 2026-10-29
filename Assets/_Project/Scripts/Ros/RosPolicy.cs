using MediKitTwin.Common;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace MediKitTwin.Ros
{
    /// <summary>
    /// 정책을 ROS 2 노드(aurix_rl의 rl_policy_node 또는 Python 학습기)에 맡긴다. 관측을 발행하고 받은 행동을 돌려준다.
    /// 통신은 비동기라 SelectAction은 가장 최근에 받은 행동을 돌려주며, 아직 못 받았으면 0(정지)을 돌려준다.
    /// 보상과 에피소드 결과도 함께 발행해서 외부 학습기가 학습할 수 있게 한다.
    /// 강화학습 환경의 Policy 필드에 지정한다. ROS 제어 모드이므로 배속은 1배로 고정한다(FR-17과 같은 규칙).
    /// </summary>
    public class RosPolicy : MonoBehaviour, IPolicy, IRlFeedbackListener
    {
        [SerializeField] private string _observationTopic = "/rl/observation";
        [SerializeField] private string _actionTopic = "/rl/action";
        [SerializeField] private string _rewardTopic = "/rl/step_reward";
        [SerializeField] private string _episodeTopic = "/rl/episode_result";

        private ROSConnection _ros;
        private float[] _latestAction = System.Array.Empty<float>();

        private void OnEnable()
        {
            SimClock.SetSpeedLocked(true);
            _ros = ROSConnection.GetOrCreateInstance();
            _ros.RegisterPublisher<Float32MultiArrayMsg>(_observationTopic);
            _ros.RegisterPublisher<Float32Msg>(_rewardTopic);
            _ros.RegisterPublisher<Float32MultiArrayMsg>(_episodeTopic);
            _ros.Subscribe<Float32MultiArrayMsg>(_actionTopic, OnAction);
        }

        private void OnDisable()
        {
            _ros?.Unsubscribe(_actionTopic);
            SimClock.SetSpeedLocked(false);
        }

        public void SelectAction(float[] observation, float[] action)
        {
            _ros.Publish(_observationTopic, new Float32MultiArrayMsg(new MultiArrayLayoutMsg(), (float[])observation.Clone()));

            // 길이가 맞는 행동만 쓴다. 학습기와 환경의 행동 크기가 다르면 정지로 두어 엉뚱한 관절이 움직이는 것을 막는다.
            if (_latestAction.Length == action.Length)
            {
                System.Array.Copy(_latestAction, action, action.Length);
            }
            else
            {
                System.Array.Clear(action, 0, action.Length);
            }
        }

        public void OnStepReward(float reward)
        {
            _ros.Publish(_rewardTopic, new Float32Msg(reward));
        }

        public void OnEpisodeFinished(float totalReward, bool success)
        {
            _ros.Publish(_episodeTopic, new Float32MultiArrayMsg(new MultiArrayLayoutMsg(), new[] { totalReward, success ? 1f : 0f }));
        }

        private void OnAction(Float32MultiArrayMsg message)
        {
            _latestAction = message.data;
        }
    }
}
