using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서려던 자리가 막혀 있으면 정해진 방향으로 조금씩 물러나며 처음으로 비어 있는 자리를 찾는다.
    /// 막힘 여부는 주입받으므로 무엇이 막는지(레일, 선반 다리 등)는 모르고, 씬 없이 시험한다.
    /// 끝까지 물러나도 막혀 있으면 자리를 바꾸지 않는다. 엉뚱하게 먼 자리로 보내는 것보다 원래 자리에서 실패하는 것이 원인을 알기 쉽다.
    /// </summary>
    public sealed class StandRetreat
    {
        // 물러나는 방향의 수평 성분이 이보다 짧으면 방향이 없는 것으로 본다(길이 1로 맞출 수 없다).
        private const float MinDirectionSqrMagnitude = 1e-8f;

        // 최대 거리가 한 걸음의 정수배일 때 부동소수 오차로 마지막 걸음이 빠지지 않게 걸음 수 계산에 더하는 여유.
        private const float StepCountEpsilon = 1e-4f;

        private readonly Func<Vector3, bool> _isClearAt;
        private readonly float _stepM;
        private readonly float _maxRetreatM;

        /// <param name="isClearAt">그 자리(바닥 위 지점)가 비어 있으면 true.</param>
        /// <param name="stepM">한 번에 물러나는 거리(m). 작을수록 가장 가까운 빈 자리를 찾는다.</param>
        /// <param name="maxRetreatM">물러나 볼 최대 거리(m).</param>
        public StandRetreat(Func<Vector3, bool> isClearAt, float stepM, float maxRetreatM)
        {
            if (stepM <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stepM), "물러나는 한 걸음은 0보다 커야 한다.");
            }

            _isClearAt = isClearAt ?? throw new ArgumentNullException(nameof(isClearAt));
            _stepM = stepM;
            _maxRetreatM = Mathf.Max(0f, maxRetreatM);
        }

        /// <param name="away">물러나는 방향. 수평 성분만 쓰고 길이를 1로 맞춘다. 영벡터면 물러날 수 없다.</param>
        /// <param name="cleared">비어 있는 자리. 찾지 못했으면 원래 자리.</param>
        /// <param name="retreatedM">물러난 거리(m). 원래 자리가 비어 있거나 찾지 못했으면 0.</param>
        /// <returns>서려던 자리나 물러난 자리가 비어 있으면 true.</returns>
        public bool TryFind(Vector3 stand, Vector3 away, out Vector3 cleared, out float retreatedM)
        {
            cleared = stand;
            retreatedM = 0f;

            if (_isClearAt(stand))
            {
                return true;
            }

            var flat = new Vector3(away.x, 0f, away.z);
            if (flat.sqrMagnitude < MinDirectionSqrMagnitude)
            {
                return false;
            }

            flat.Normalize();
            int steps = Mathf.FloorToInt(_maxRetreatM / _stepM + StepCountEpsilon);
            for (int step = 1; step <= steps; step++)
            {
                Vector3 candidate = stand + flat * (step * _stepM);
                if (_isClearAt(candidate))
                {
                    cleared = candidate;
                    retreatedM = step * _stepM;
                    return true;
                }
            }

            return false;
        }
    }
}
