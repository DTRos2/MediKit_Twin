using System;
using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>도구 끝점을 목표 위치로 보내는 보상. 가까워지면 +, 멀어지면 -, 도착하면 보너스.</summary>
    [Serializable]
    public class ReachReward : IRewardFunction
    {
        [Tooltip("목표까지 거리 1m당 감점.")]
        [SerializeField] private float _distanceWeight = 1f;

        [Tooltip("이전 스텝보다 가까워진 거리 1m당 보상. 거리 감점만 쓰면 학습 신호가 약하다.")]
        [SerializeField] private float _progressWeight = 10f;

        [SerializeField] private float _successBonus = 10f;
        [SerializeField] private float _stepPenalty = 0.01f;

        [Tooltip("행동 크기 감점. 불필요하게 큰 움직임을 줄인다.")]
        [SerializeField] private float _actionPenaltyWeight = 0.005f;

        [SerializeField, Min(0.001f)] private float _successDistanceM = 0.02f;

        public float Evaluate(in RlContext context)
        {
            float reward = -_distanceWeight * context.Distance;
            reward += _progressWeight * (context.PreviousDistance - context.Distance);
            reward -= _stepPenalty;
            reward -= _actionPenaltyWeight * context.ActionMagnitude;

            if (IsSuccess(context))
            {
                reward += _successBonus;
            }

            return reward;
        }

        public bool IsSuccess(in RlContext context)
        {
            return context.Distance <= _successDistanceM;
        }
    }
}
