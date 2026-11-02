using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서는 자리와 도구 방향 조합의 점수(작을수록 좋다). 도구 방향이 기본 방향에서 벌어질수록, 기본 자리에서 멀리 설수록 커진다.
    /// 두 값의 가중치는 설정에서 받으므로 이 클래스에는 숫자가 없다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public sealed class StandPlacementCost
    {
        private readonly float _deviationCostPerDeg;
        private readonly float _shiftCostPerM;

        /// <param name="deviationCostPerDeg">도구 방향이 기본 방향에서 1도 벌어질 때 더하는 점수.</param>
        /// <param name="shiftCostPerM">기본 자리에서 1m 벗어날 때 더하는 점수.</param>
        public StandPlacementCost(float deviationCostPerDeg, float shiftCostPerM)
        {
            if (deviationCostPerDeg < 0f || shiftCostPerM < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deviationCostPerDeg), "점수 가중치는 0 이상이어야 한다.");
            }

            _deviationCostPerDeg = deviationCostPerDeg;
            _shiftCostPerM = shiftCostPerM;
        }

        public float Of(float deviationDeg, float shiftM)
        {
            return deviationDeg * _deviationCostPerDeg + shiftM * _shiftCostPerM;
        }
    }
}
