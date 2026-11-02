using System;
using System.Collections.Generic;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 닫는 명령을 끝까지 내린 뒤의 손가락 위치로, 손가락 사이에 물체가 걸렸는지 판정한다.
    /// 손가락마다 열린 값에서 닫힌 값까지 얼마나 닫혔는지(0~1)를 재어, 하나라도 기준 비율에 못 미치면 걸린 것으로 본다.
    /// 아무것도 없으면 모든 손가락이 닫힌 값까지 간다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public sealed class GripClosureJudge
    {
        // 열린 값과 닫힌 값이 이 이하로 가까우면 움직임이 없는 관절이라 판정에서 뺀다.
        private const float MinSpan = 1e-6f;

        private readonly float _fullyClosedRatio;

        /// <param name="fullyClosedRatio">이 비율(0 초과 1 이하) 이상 닫혔으면 끝까지 닫힌 것으로 본다. 예: 0.85.</param>
        public GripClosureJudge(float fullyClosedRatio)
        {
            if (fullyClosedRatio <= 0f || fullyClosedRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fullyClosedRatio), "끝까지 닫힘 비율은 0 초과 1 이하여야 한다.");
            }

            _fullyClosedRatio = fullyClosedRatio;
        }

        /// <param name="positions">손가락 관절의 지금 위치(프로필 값과 같은 단위).</param>
        /// <param name="openValues">손가락 관절의 열린 값.</param>
        /// <param name="closedValues">손가락 관절의 닫힌 값.</param>
        public bool IsBlocked(IReadOnlyList<float> positions, IReadOnlyList<float> openValues, IReadOnlyList<float> closedValues)
        {
            if (positions == null || openValues == null || closedValues == null
                || positions.Count != openValues.Count || positions.Count != closedValues.Count)
            {
                throw new ArgumentException("위치, 열린 값, 닫힌 값의 개수가 같아야 한다.");
            }

            for (int i = 0; i < positions.Count; i++)
            {
                float span = closedValues[i] - openValues[i];
                if (Math.Abs(span) <= MinSpan)
                {
                    continue;
                }

                float closedRatio = (positions[i] - openValues[i]) / span;
                if (closedRatio < _fullyClosedRatio)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
