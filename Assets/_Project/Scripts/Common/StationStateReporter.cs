namespace MediKitTwin.Common
{
    /// <summary>
    /// 설비 하나의 상태를 CellEvents로 알린다. 상태가 바뀔 때만 StationStateChanged를 보내고,
    /// 상태가 같아도 StationHeartbeat.IntervalSeconds마다 StationAlive를 보낸다. (FR-14)
    /// 이 규칙을 설비마다 다시 쓰지 않도록 모았으므로, 새 설비는 이 클래스를 가지고 Set과 Tick만 부르면 된다.
    /// </summary>
    public sealed class StationStateReporter : IStationStateReporter
    {
        private readonly StationId _station;
        private float? _nextAliveAt;

        public StationStateReporter(StationId station, StationState initialState = StationState.Idle)
        {
            _station = station;
            State = initialState;
        }

        public StationState State { get; private set; }

        /// <summary>상태를 바꾼다. 같은 상태이면 아무것도 보내지 않는다.</summary>
        public void Set(StationState state)
        {
            if (state == State)
            {
                return;
            }

            State = state;
            CellEvents.RaiseStationStateChanged(_station, state);
        }

        /// <summary>
        /// 주기 통지를 보낼 때가 되었으면 StationAlive를 보낸다. 일시정지와 배속에 영향받지 않도록
        /// 실제 시간(Time.unscaledTime)을 넘겨야 한다. 처음 부르면 바로 한 번 보낸다.
        /// </summary>
        public void Tick(float unscaledNow)
        {
            if (_nextAliveAt.HasValue && unscaledNow < _nextAliveAt.Value)
            {
                return;
            }

            _nextAliveAt = unscaledNow + StationHeartbeat.IntervalSeconds;
            CellEvents.RaiseStationAlive(_station, State);
        }
    }
}
