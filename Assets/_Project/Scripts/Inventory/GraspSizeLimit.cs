using System;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 구성품 크기의 상한. 두 가지를 함께 지킨다.
    ///   1) 그리퍼가 집을 수 있는 크기: 집는 방향을 미리 알 수 없으므로 세 변 중 가장 긴 변이
    ///      그리퍼가 열린 폭에 여유를 둔 값을 넘지 않는다.
    ///   2) 키트 박스 칸에 끼지 않는 크기(WithFaceDiagonalCeiling으로 더한다): 가장 긴 두 변이 이루는 면의 대각선이
    ///      칸 길이를 넘지 않는다. 얇은 판이 칸 안에서 세로로 서거나 기울면 길이가 아니라 그 대각선만큼 칸을 차지하므로,
    ///      가장 긴 변만 칸 길이 이하여도 끼일 수 있다.
    /// 넘으면 모양을 유지한 채 같은 비율로 줄인다.
    /// 그리퍼 폭은 로봇마다 다르므로 코드에 쓰지 않고 호출하는 쪽이 로봇 설정에서 읽어 넘긴다.
    /// </summary>
    public sealed class GraspSizeLimit
    {
        private readonly float _maxSize;
        private readonly float _maxFaceDiagonal;

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
            _maxFaceDiagonal = float.PositiveInfinity;
        }

        // 그리퍼 한도는 그대로 두고 칸 한도만 바꾼 복사본을 만든다. 공개 생성자와 매개변수 모양이 겹치지 않게 원본을 받는다.
        private GraspSizeLimit(GraspSizeLimit source, float maxFaceDiagonal)
        {
            _maxSize = source._maxSize;
            _maxFaceDiagonal = maxFaceDiagonal;
        }

        /// <summary>
        /// 가장 긴 두 변의 대각선 상한을 ceiling(m)까지 더 낮춘 한도를 돌려준다. 이미 ceiling 이하이면 그대로이다.
        /// 그리퍼가 아니라 키트 박스 칸처럼 다른 쪽이 정하는 크기 제한을 함께 지키려고 쓴다.
        /// </summary>
        public GraspSizeLimit WithFaceDiagonalCeiling(float ceiling)
        {
            if (ceiling <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ceiling), "상한은 0보다 커야 한다.");
            }

            return ceiling >= _maxFaceDiagonal ? this : new GraspSizeLimit(this, ceiling);
        }

        /// <summary>물건 한 변의 최대 길이(m).</summary>
        public float MaxSize => _maxSize;

        /// <summary>가장 긴 두 변이 이루는 면의 대각선 최대 길이(m). 칸 제한을 더하지 않았으면 무한대이다.</summary>
        public float MaxFaceDiagonal => _maxFaceDiagonal;

        /// <summary>크기가 (width, height, depth)인 물건에 곱할 배율. 이미 모든 한도를 지키면 1이다.</summary>
        public float ScaleFor(float width, float height, float depth)
        {
            float longest = Math.Max(width, Math.Max(height, depth));
            if (longest <= 0f)
            {
                return 1f;
            }

            float graspScale = longest <= _maxSize ? 1f : _maxSize / longest;

            float diagonal = FaceDiagonal(width, height, depth);
            float cellScale = diagonal <= _maxFaceDiagonal ? 1f : _maxFaceDiagonal / diagonal;

            return Math.Min(graspScale, cellScale);
        }

        /// <summary>가장 긴 두 변이 이루는 면의 대각선. 세 변 제곱의 합에서 가장 짧은 변의 제곱을 뺀 값의 제곱근이다.</summary>
        public static float FaceDiagonal(float width, float height, float depth)
        {
            float shortest = Math.Min(width, Math.Min(height, depth));
            float sumOfSquares = width * width + height * height + depth * depth;
            return (float)Math.Sqrt(sumOfSquares - shortest * shortest);
        }
    }
}
