namespace MediKitTwin.Common
{
    /// <summary>
    /// 주문 처리 현황. 대시보드가 1초마다 읽어 표시한다. (FR-13, FR-14)
    /// 변화 자체는 CellEvents.OrderHeld와 OrderResumed로 알리고, 이 인터페이스는 현재 값을 조회한다.
    /// </summary>
    public interface IOrderStatus
    {
        /// <summary>재고 부족으로 보류 중인 주문 수.</summary>
        int HeldOrderCount { get; }

        /// <summary>아직 피킹을 시작하지 않은 키트 수(대기열과 보류 주문의 남은 키트 합계).</summary>
        int PendingKitCount { get; }
    }
}
