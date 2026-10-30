using System;
using System.Collections.Generic;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸 하나에 구성품을 count개 놓을 자리를 계산한다. 격자로 놓되 한 줄에 놓는 개수를 넘으면 줄을 바꾸고,
    /// 구성품이 격자 한 칸보다 크면 같은 비율로 줄인다. 에디터 빌더, 재고 표시, 테스트가 같은 계산을 쓰도록
    /// Unity 타입 없이 숫자만 다룬다.
    /// </summary>
    public sealed class ShelfCellLayout
    {
        private readonly float _innerWidth;
        private readonly float _innerDepth;
        private readonly float _depthCenter;
        private readonly int _itemsPerRow;
        private readonly float _fillRatio;

        /// <param name="innerWidth">구성품을 놓을 수 있는 칸 안쪽 가로(m).</param>
        /// <param name="innerDepth">안쪽 깊이(m).</param>
        /// <param name="depthCenter">안쪽 영역의 깊이 중심이 칸 중심에서 벗어난 정도(m). 한쪽이 열린 칸처럼 벽이 한쪽에만 있을 때 쓴다.</param>
        /// <param name="itemsPerRow">한 줄에 놓는 최대 개수.</param>
        /// <param name="fillRatio">물건이 차지할 수 있는 격자 칸의 비율(0 초과 1 이하). 꽉 채우면 서로 닿아 보이므로 1보다 작게 쓴다.</param>
        public ShelfCellLayout(float innerWidth, float innerDepth, float depthCenter, int itemsPerRow, float fillRatio)
        {
            if (innerWidth <= 0f || innerDepth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(innerWidth), "칸 안쪽 크기는 0보다 커야 한다.");
            }

            if (itemsPerRow < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemsPerRow), "한 줄에 놓는 개수는 1 이상이어야 한다.");
            }

            if (fillRatio <= 0f || fillRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fillRatio), "채움 비율은 0 초과 1 이하여야 한다.");
            }

            _fillRatio = fillRatio;
            _innerWidth = innerWidth;
            _innerDepth = innerDepth;
            _depthCenter = depthCenter;
            _itemsPerRow = itemsPerRow;
        }

        /// <summary>
        /// 구성품 하나의 바닥 크기(가로 itemWidth, 깊이 itemDepth, m)를 받아 count개의 자리를 돌려준다.
        /// 왼쪽부터, 앞쪽 줄부터 채우며 목록 순서가 곧 슬롯 번호(집는 순서)이다. count가 0 이하이면 빈 목록이다.
        /// </summary>
        public IReadOnlyList<ShelfItemPlacement> Compute(int count, float itemWidth, float itemDepth)
        {
            var placements = new List<ShelfItemPlacement>();
            if (count <= 0)
            {
                return placements;
            }

            int columns = Math.Min(count, _itemsPerRow);
            int rows = (count + columns - 1) / columns;

            // 칸 너비는 줄의 최대 개수 기준으로 고정해, 개수가 적어도 구성품 간격이 달라지지 않게 한다.
            float slotWidth = _innerWidth / _itemsPerRow;
            float slotDepth = _innerDepth / rows;

            float scale = Math.Min(1f, Math.Min(
                itemWidth > 0f ? slotWidth * _fillRatio / itemWidth : 1f,
                itemDepth > 0f ? slotDepth * _fillRatio / itemDepth : 1f));

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                float x = (column - (columns - 1) * 0.5f) * slotWidth;

                // 첫 줄이 앞쪽(+Z, 열린 쪽)이 되도록 줄 번호가 클수록 안쪽(-Z)으로 간다. 로봇암이 앞줄부터 집기 때문이다.
                float z = _depthCenter + ((rows - 1) * 0.5f - row) * slotDepth;
                placements.Add(new ShelfItemPlacement(x, z, scale));
            }

            return placements;
        }
    }
}
