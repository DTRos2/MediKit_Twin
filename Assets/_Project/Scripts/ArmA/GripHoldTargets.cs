using System;
using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 쥔 뒤 손가락이 어디에 멈춰 서서 버틸지 정한다. 닫는 명령은 끝까지 닫힌 값을 향하므로, 그대로 두면 손가락이 물건에 막힌 채
    /// 구동 한계 힘으로 계속 밀어 얇거나 가벼운 물건을 뚫고 지나간다. 쥔 순간의 손가락 위치에서 조금만 더 닫히는 값으로 목표를 바꿔
    /// 손가락이 물건에 닿은 자리를 지키며 일정한 힘으로만 누르게 한다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public sealed class GripHoldTargets
    {
        // 열린 값과 닫힌 값이 이 이하로 가까우면 움직임이 없는 관절이라 닫힌 값을 그대로 쓴다.
        private const float MinSpan = 1e-6f;

        private readonly float _preloadFraction;

        /// <param name="preloadFraction">쥔 자리에서 닫히는 쪽으로 더 가는 양. 열린 값에서 닫힌 값까지 거리의 비율(0 이상 1 이하)이며, 클수록 세게 누른다.</param>
        public GripHoldTargets(float preloadFraction)
        {
            if (preloadFraction < 0f || preloadFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(preloadFraction), "눌림 비율은 0 이상 1 이하여야 한다.");
            }

            _preloadFraction = preloadFraction;
        }

        /// <param name="positions">손가락 관절의 지금 위치(프로필 값과 같은 단위).</param>
        /// <param name="openValues">손가락 관절의 열린 값.</param>
        /// <param name="closedValues">손가락 관절의 닫힌 값.</param>
        /// <returns>관절마다 쥐고 있는 동안 구동할 목표. 닫힌 값을 넘지 않는다.</returns>
        public float[] Compute(IReadOnlyList<float> positions, IReadOnlyList<float> openValues, IReadOnlyList<float> closedValues)
        {
            RequireSameCount(positions, openValues, closedValues);

            var targets = new float[positions.Count];
            for (int i = 0; i < targets.Length; i++)
            {
                float span = closedValues[i] - openValues[i];
                if (Math.Abs(span) <= MinSpan)
                {
                    targets[i] = closedValues[i];
                    continue;
                }

                float preloaded = positions[i] + span * _preloadFraction;
                float low = Math.Min(openValues[i], closedValues[i]);
                float high = Math.Max(openValues[i], closedValues[i]);
                targets[i] = Math.Max(low, Math.Min(high, preloaded));
            }

            return targets;
        }

        /// <summary>
        /// 손가락들이 지금 얼마나 열려 있는지(0 닫힘, 1 열림). 움직이는 관절의 평균이다.
        /// 놓을 때 여는 동작을 지금 손가락 위치에서 시작시키는 데 쓴다. 움직이는 관절이 없으면 1을 돌려준다.
        /// </summary>
        public static float CurrentOpening(IReadOnlyList<float> positions, IReadOnlyList<float> openValues, IReadOnlyList<float> closedValues)
        {
            RequireSameCount(positions, openValues, closedValues);

            float sum = 0f;
            int moving = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                float span = closedValues[i] - openValues[i];
                if (Math.Abs(span) <= MinSpan)
                {
                    continue;
                }

                float closedRatio = (positions[i] - openValues[i]) / span;
                sum += 1f - Math.Max(0f, Math.Min(1f, closedRatio));
                moving++;
            }

            return moving == 0 ? 1f : sum / moving;
        }

        private static void RequireSameCount(IReadOnlyList<float> positions, IReadOnlyList<float> openValues, IReadOnlyList<float> closedValues)
        {
            if (positions == null || openValues == null || closedValues == null
                || positions.Count != openValues.Count || positions.Count != closedValues.Count)
            {
                throw new ArgumentException("위치, 열린 값, 닫힌 값의 개수가 같아야 한다.");
            }
        }
    }
}
