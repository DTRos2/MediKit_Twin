namespace MediKitTwin.Common
{
    /// <summary>
    /// 설비 상태. 대시보드의 "가동 / 대기 / 정지" 표시에 대응한다.
    /// Fault는 대시보드에서 "정지"로 표시한다. "응답 없음"은 신호가 10초 넘게 없을 때
    /// 대시보드가 스스로 판단하는 표시이므로 설비가 이 값을 보내지 않는다. (FR-14)
    /// </summary>
    public enum StationState
    {
        Idle,
        Running,
        Fault
    }
}
