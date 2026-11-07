namespace MediKitTwin.ArmA
{
    /// <summary>선반에서 집는 동작의 거리 설정과, 스스로 찾을 때 도구 방향이 기본에서 벗어날 수 있는 범위.</summary>
    public readonly struct ShelfPickSettings
    {
        /// <param name="taughtShiftLimitM">알려 준 자세로 집을 때 서는 자리를 물건 쪽으로 옮길 수 있는 최대 수평 거리(m). 0 이하이면 옮기지 않고 저장값 그대로 간다.</param>
        public ShelfPickSettings(float approachDistanceM, float retreatDistanceM, OrientationSearchSettings search, float taughtShiftLimitM = 0f)
        {
            ApproachDistanceM = approachDistanceM;
            RetreatDistanceM = retreatDistanceM;
            Search = search;
            TaughtShiftLimitM = taughtShiftLimitM;
        }

        /// <summary>선반 칸 앞에서 물건 쪽으로 들어가기 시작하는 거리(m).</summary>
        public float ApproachDistanceM { get; }

        /// <summary>물건을 쥔 뒤 칸 밖으로 빼내는 거리(m).</summary>
        public float RetreatDistanceM { get; }

        public OrientationSearchSettings Search { get; }

        /// <summary>
        /// 알려 준 자세로 집을 때, 알려 준 쥐는 자리와 지금 물건이 수평으로 어긋나 있으면 서는 자리를 그만큼 옮겨 맞춘다(TaughtPoseAnchor).
        /// 이 값은 옮길 수 있는 최대 거리(m)이다. 더 멀면 알려 준 자세를 쓰지 않는다. 0 이하이면 옮기지 않고 저장값 그대로 쓴다.
        /// </summary>
        public float TaughtShiftLimitM { get; }
    }
}
