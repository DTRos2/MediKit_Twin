namespace MediKitTwin.Common
{
    /// <summary>
    /// 설비 상태를 알리는 접점. 설비 로직은 이 인터페이스로만 상태를 바꾸므로, 이벤트 버스 없이도 시험할 수 있고
    /// 알리는 방법을 바꿔 끼울 수 있다. 실제 구현은 StationStateReporter(CellEvents로 알림)이다.
    /// </summary>
    public interface IStationStateReporter
    {
        StationState State { get; }

        /// <summary>상태를 바꾼다. 같은 상태이면 아무것도 알리지 않는다.</summary>
        void Set(StationState state);

        /// <summary>주기 통지를 보낼 때가 되었으면 보낸다. 실제 시간(unscaled)을 넘긴다.</summary>
        void Tick(float unscaledNow);
    }
}
