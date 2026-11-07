namespace MediKitTwin.ArmA
{
    /// <summary>도구 방향 후보 하나를 따져 본 결과.</summary>
    public enum CandidateVerdict
    {
        /// <summary>팔이 닿지 않는다.</summary>
        Unreachable,

        /// <summary>닿기는 하지만 팔이 장애물과 겹칠 수 있다.</summary>
        Blocked,

        /// <summary>닿고, 장애물과도 겹치지 않는다.</summary>
        Clear
    }
}
