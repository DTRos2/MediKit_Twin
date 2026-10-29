using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>
    /// 로봇암 도달(reach) 강화학습 환경. 관측 만들기, 행동 해석, 목표 뽑기, 보상 계산을 각각 다른 객체에 맡기고
    /// 이 클래스는 에피소드와 결정 주기를 엮는 일만 한다. 로봇은 인터페이스(IArmState, IArmDriver, IGripper)로만 만나서
    /// FR5와 UR3에 같은 코드를 쓴다. 정책(IPolicy)은 무작위, 학습된 모델, ROS 2 브리지 어느 쪽이든 끼울 수 있다.
    /// </summary>
    public class ArmReachEnvironment : MonoBehaviour
    {
        [Header("로봇 (ArmController 하나로 세 인터페이스를 모두 충족)")]
        [SerializeField] private MonoBehaviour _arm;
        [Tooltip("비우면 그리퍼 없이 팔만 학습한다.")]
        [SerializeField] private MonoBehaviour _gripper;

        [Header("정책과 목표")]
        [SerializeField] private MonoBehaviour _policy;
        [SerializeField] private MonoBehaviour _goalSampler;
        [Tooltip("목표 위치를 눈으로 보여 줄 표시물(선택).")]
        [SerializeField] private Transform _goalMarker;

        [Header("에피소드")]
        [Tooltip("행동 하나를 유지하는 FixedUpdate 횟수.")]
        [SerializeField, Min(1)] private int _decisionPeriodFixedSteps = 5;
        [SerializeField, Min(1)] private int _maxStepsPerEpisode = 200;
        [Tooltip("에피소드 시작 자세(rad). 비우면 모든 관절 0.")]
        [SerializeField] private float[] _homePose;

        [SerializeReference] private IRewardFunction _reward = new ReachReward();

        private IArmState _state;
        private IArmDriver _driver;
        private IGripper _grip;
        private IPolicy _policyImpl;
        private IRlFeedbackListener _feedback;
        private IGoalSampler _sampler;
        private ArmObservationBuilder _observationBuilder;
        private ArmActionInterpreter _actionInterpreter;

        private float[] _observation;
        private float[] _action;
        private float[] _home;
        private Vector3 _goal;
        private float _previousDistance;
        private float _episodeReward;
        private int _episodeStep;
        private int _fixedStepCounter;
        private bool _ready;

        public int ObservationSize { get; private set; }
        public int ActionSize { get; private set; }
        public int EpisodeCount { get; private set; }

        /// <summary>에피소드가 끝날 때 발생. 인자는 (누적 보상, 성공 여부).</summary>
        public event Action<float, bool> EpisodeFinished;

        /// <summary>결정 한 번이 끝날 때마다 발생. 인자는 (이번 스텝 보상). 학습기나 ROS 브리지가 구독한다.</summary>
        public event Action<float> StepCompleted;

        private void Start()
        {
            _state = _arm as IArmState;
            _driver = _arm as IArmDriver;
            _grip = _gripper as IGripper;
            _policyImpl = _policy as IPolicy;
            _feedback = _policy as IRlFeedbackListener;
            _sampler = _goalSampler as IGoalSampler;

            if (_state == null || _driver == null || _policyImpl == null || _sampler == null || _reward == null)
            {
                Debug.LogError($"{name}: Arm(IArmState+IArmDriver), Policy, Goal Sampler, Reward를 모두 지정해야 한다.", this);
                enabled = false;
                return;
            }

            int jointCount = _state.JointNames.Count;
            _observationBuilder = new ArmObservationBuilder(_state, _driver.MaxJointSpeedRadPerSec);
            _actionInterpreter = new ArmActionInterpreter(_state, _driver, _grip);

            ObservationSize = _observationBuilder.Size;
            ActionSize = _actionInterpreter.Size;
            _observation = new float[ObservationSize];
            _action = new float[ActionSize];

            _home = new float[jointCount];
            if (_homePose != null && _homePose.Length == jointCount)
            {
                Array.Copy(_homePose, _home, jointCount);
            }

            _ready = true;
            ResetEpisode();
        }

        private void FixedUpdate()
        {
            if (!_ready)
            {
                return;
            }

            _fixedStepCounter++;
            if (_fixedStepCounter < _decisionPeriodFixedSteps)
            {
                return;
            }

            _fixedStepCounter = 0;
            DecideAndEvaluate();
        }

        public void ResetEpisode()
        {
            _driver.ResetToPose(_home);
            _grip?.Open();

            _goal = _sampler.Sample();
            if (_goalMarker != null)
            {
                _goalMarker.position = _goal;
            }

            _previousDistance = Vector3.Distance(_state.ToolPose.position, _goal);
            _episodeReward = 0f;
            _episodeStep = 0;
            _fixedStepCounter = 0;
            EpisodeCount++;
        }

        // 결정 시점마다 지난 행동의 결과(보상)를 계산하고, 다음 행동을 정책에서 받아 적용한다.
        private void DecideAndEvaluate()
        {
            if (_episodeStep > 0)
            {
                Evaluate();
            }

            _observationBuilder.Build(_goal, _grip, _observation);
            _policyImpl.SelectAction(_observation, _action);

            float decisionSeconds = _decisionPeriodFixedSteps * Time.fixedDeltaTime;
            _actionInterpreter.Apply(_action, decisionSeconds);
            _episodeStep++;
        }

        private void Evaluate()
        {
            var context = new RlContext(
                _state.ToolPose.position,
                _goal,
                _previousDistance,
                _actionInterpreter.Magnitude(_action),
                _grip != null && _grip.IsHolding,
                _episodeStep);

            float reward = _reward.Evaluate(context);
            _episodeReward += reward;
            _previousDistance = context.Distance;
            StepCompleted?.Invoke(reward);
            _feedback?.OnStepReward(reward);

            bool success = _reward.IsSuccess(context);
            bool timeout = _episodeStep >= _maxStepsPerEpisode;
            if (success || timeout)
            {
                EpisodeFinished?.Invoke(_episodeReward, success);
                _feedback?.OnEpisodeFinished(_episodeReward, success);
                ResetEpisode();
            }
        }
    }
}
