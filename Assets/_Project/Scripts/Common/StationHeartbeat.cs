namespace MediKitTwin.Common
{
    /// <summary>
    /// 설비 주기 통지 규칙. 각 설비는 상태가 바뀌지 않아도 IntervalSeconds마다 CellEvents.StationAlive를 보내고,
    /// 대시보드는 TimeoutSeconds 넘게 받지 못하면 "응답 없음"으로 표시한다. (FR-14)
    /// 상태가 바뀔 때는 이와 별도로 CellEvents.StationStateChanged를 한 번 보낸다.
    /// 일시정지(timeScale 0) 중에도 응답 없음으로 오판하지 않도록 두 값 모두 실제 시간(Time.unscaledTime) 기준이다.
    /// </summary>
    public static class StationHeartbeat
    {
        public const float IntervalSeconds = 1f;

        public const float TimeoutSeconds = 10f;
    }
}
