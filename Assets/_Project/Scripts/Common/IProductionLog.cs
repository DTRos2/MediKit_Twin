namespace MediKitTwin.Common
{
    /// <summary>
    /// 생산 로그. 실행마다 CSV 1개를 새로 만들고 덮어쓰지 않는다. (FR-14, NFR-05)
    /// 열: 시각(SimClock.Now), 이벤트명, 로트번호, 설비 ID, 판정 방식, 비고.
    /// 로그를 쓰는 쪽은 CellEvents를 구독하는 로거 하나이며, 설비는 로그 파일을 직접 다루지 않는다.
    /// </summary>
    public interface IProductionLog
    {
        /// <param name="eventName">CellEvents 신호 이름 또는 경고 이름.</param>
        /// <param name="kit">관련 키트. 키트와 무관한 이벤트(재고, 설비 상태)는 null이다.</param>
        /// <param name="station">신호를 보낸 설비. 알 수 없으면 null이다.</param>
        /// <param name="method">판정 방식. 판정 이벤트만 채운다.</param>
        /// <param name="note">판정과 정답 비교 결과, 재고 칸 ID 등 추가 설명. 없으면 null이다.</param>
        void Record(string eventName, KitData kit, StationId? station, JudgeMethod? method, string note);
    }
}
