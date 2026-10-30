using System;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 그리퍼가 집을 수 있는 구성품 크기의 상한. 집는 방향을 미리 알 수 없으므로 세 변 중 가장 긴 변이
    /// 그리퍼가 열린 폭에 여유를 둔 값을 넘지 않도록, 넘으면 모양을 유지한 채 같은 비율로 줄인다.
    /// 그리퍼 폭은 로봇마다 다르므로 코드에 쓰지 않고 호출하는 쪽이 로봇 설정에서 읽어 넘긴다.
    /// </summary>
    public sealed class GraspSizeLimit
    {
        private readonly float _maxSize;

        /// <param name="openingWidth">그리퍼가 완전히 열렸을 때 손가락 사이 간격(m).</param>
        /// <param name="clearanceRatio">열린 폭 중 물건이 차지해도 되는 비율(0~1). 나머지는 손가락이 들어갈 여유이다.</param>
        public GraspSizeLimit(float openingWidth, float clearanceRatio)
        {
            if (openingWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(openingWidth), "그리퍼 열린 폭은 0보다 커야 한다.");
            }

            if (clearanceRatio <= 0f || clearanceRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(clearanceRatio), "여유 비율은 0 초과 1 이하여야 한다.");
            }

            _maxSize = openingWidth * clearanceRatio;
        }

        /// <summary>물건 한 변의 최대 길이(m).</summary>
        public float MaxSize => _maxSize;

        /// <summary>크기가 (width, height, depth)인 물건에 곱할 배율. 이미 집을 수 있으면 1이다.</summary>
        public float ScaleFor(float width, float height, float depth)
        {
            float longest = Math.Max(width, Math.Max(height, depth));
            return longest <= _maxSize || longest <= 0f ? 1f : _maxSize / longest;
        }
    }
}
