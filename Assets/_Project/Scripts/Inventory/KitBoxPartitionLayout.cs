using System;
using System.Collections.Generic;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 키트 박스 슬롯 사이의 칸막이 자리와, 칸 안에 들어가는 물건의 크기 상한을 계산한다.
    /// 슬롯은 박스의 긴 변 방향 한 줄로 놓이므로 모든 값은 그 방향의 1차원 좌표(m)이다.
    /// 칸막이는 이웃한 슬롯의 한가운데에 둔다. 슬롯 간격이 고르지 않아도 칸마다 양쪽 슬롯에서 같은 거리에 선다.
    /// Unity 타입을 쓰지 않아 EditMode 시험으로 값을 확인한다.
    /// </summary>
    public sealed class KitBoxPartitionLayout
    {
        // 레시피마다 같은 슬롯 좌표를 부동소수점으로 적으므로, 이 거리보다 가까운 좌표는 같은 슬롯으로 본다.
        private const float SameSlotToleranceM = 1e-4f;

        private readonly List<float> _dividerCenters = new List<float>();

        /// <param name="slotCenters">슬롯 중심의 박스 긴 변 방향 좌표(m). 같은 좌표는 하나로 합친다.</param>
        /// <param name="thickness">칸막이 두께(m).</param>
        /// <param name="itemGap">물건과 칸막이 사이에 한쪽마다 남길 틈(m).</param>
        public KitBoxPartitionLayout(IEnumerable<float> slotCenters, float thickness, float itemGap)
        {
            if (slotCenters == null)
            {
                throw new ArgumentNullException(nameof(slotCenters));
            }

            if (thickness <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(thickness), "칸막이 두께는 0보다 커야 한다.");
            }

            if (itemGap < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(itemGap), "물건과 칸막이 사이 틈은 0 이상이어야 한다.");
            }

            Thickness = thickness;
            ItemGap = itemGap;

            List<float> centers = DistinctSorted(slotCenters);
            SlotCenters = centers;

            float narrowestPitch = float.PositiveInfinity;
            for (int i = 1; i < centers.Count; i++)
            {
                _dividerCenters.Add((centers[i - 1] + centers[i]) * 0.5f);
                narrowestPitch = Math.Min(narrowestPitch, centers[i] - centers[i - 1]);
            }

            // 슬롯이 하나뿐이면 나눌 곳이 없으므로 칸 길이에 제한이 없다.
            CellLength = float.IsPositiveInfinity(narrowestPitch) ? float.PositiveInfinity : narrowestPitch - thickness;
            MaxItemFaceDiagonal = CellLength - 2f * itemGap;

            if (MaxItemFaceDiagonal <= 0f)
            {
                throw new ArgumentException(
                    $"칸이 너무 좁아 물건이 들어갈 수 없다. 슬롯 간격 {narrowestPitch:0.0000}m, 칸막이 두께 {thickness:0.0000}m, 한쪽 틈 {itemGap:0.0000}m.");
            }
        }

        /// <summary>합쳐서 작은 값부터 늘어놓은 슬롯 중심 좌표(m).</summary>
        public IReadOnlyList<float> SlotCenters { get; }

        /// <summary>칸막이 중심 좌표(m). 작은 값부터이며 슬롯 수보다 하나 적다.</summary>
        public IReadOnlyList<float> DividerCenters => _dividerCenters;

        public float Thickness { get; }

        public float ItemGap { get; }

        /// <summary>
        /// 칸막이 사이의 빈 길이(m). 가장 좁은 칸 기준이다. 칸막이 두께는 이미 뺀 값이다.
        /// 슬롯이 하나뿐이면 무한대이다.
        /// </summary>
        public float CellLength { get; }

        /// <summary>
        /// 칸 안에 끼지 않고 들어가는 물건의 "가장 긴 두 변이 이루는 면의 대각선" 최대 길이(m). 칸막이 양쪽에 남길 틈을 뺀 값이다.
        /// 놓이는 방향을 미리 알 수 없고, 얇은 판은 칸 안에서 세로로 서거나 기울면 가장 긴 변이 아니라 그 면의 대각선만큼 칸을 차지한다.
        /// 그래서 가장 긴 변이 칸 길이 이하여도 끼일 수 있다. 슬롯이 하나뿐이면 무한대이다.
        /// </summary>
        public float MaxItemFaceDiagonal { get; }

        private static List<float> DistinctSorted(IEnumerable<float> values)
        {
            var sorted = new List<float>(values);
            sorted.Sort();

            var result = new List<float>();
            foreach (float value in sorted)
            {
                if (result.Count == 0 || value - result[result.Count - 1] > SameSlotToleranceM)
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}
