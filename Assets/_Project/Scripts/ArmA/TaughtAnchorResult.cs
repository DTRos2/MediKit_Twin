namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세를 지금 물건 자리에 맞춘 결과(TaughtPoseAnchor). 쓸 수 있으면 맞춘 계획과 옮긴 거리를, 쓸 수 없으면 그 이유를 담는다.
    /// </summary>
    public readonly struct TaughtAnchorResult
    {
        private TaughtAnchorResult(bool isUsable, TaughtPickPlan plan, float shiftM, string detail)
        {
            IsUsable = isUsable;
            Plan = plan;
            ShiftM = shiftM;
            Detail = detail;
        }

        /// <summary>맞춘 계획을 쓸 수 있으면 true.</summary>
        public bool IsUsable { get; }

        /// <summary>서는 자리와 도구 경로를 맞춘 계획. 쓸 수 없으면 맞추지 않은 원래 계획이다.</summary>
        public TaughtPickPlan Plan { get; }

        /// <summary>서는 자리를 옮긴 수평 거리(m). 옮기지 않았으면 0이다.</summary>
        public float ShiftM { get; }

        /// <summary>쓸 수 없는 이유(사람이 읽는 글). 쓸 수 있으면 빈 글자다.</summary>
        public string Detail { get; }

        public static TaughtAnchorResult Usable(TaughtPickPlan plan, float shiftM)
        {
            return new TaughtAnchorResult(true, plan, shiftM, string.Empty);
        }

        public static TaughtAnchorResult Unusable(TaughtPickPlan original, float shiftM, string detail)
        {
            return new TaughtAnchorResult(false, original, shiftM, detail);
        }
    }
}
