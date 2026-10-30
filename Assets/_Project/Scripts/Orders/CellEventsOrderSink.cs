using MediKitTwin.Common;

namespace MediKitTwin.Orders
{
    /// <summary>주문 관리의 알림을 CellEvents로 내보낸다. 실제 씬에서 쓰는 구현이다.</summary>
    public sealed class CellEventsOrderSink : IOrderEventSink
    {
        public void OrderHeld(int orderSequence) => CellEvents.RaiseOrderHeld(orderSequence);

        public void OrderResumed(int orderSequence) => CellEvents.RaiseOrderResumed(orderSequence);

        public void WarningRaised(string code, string message) => CellEvents.RaiseWarning(code, message);
    }
}
