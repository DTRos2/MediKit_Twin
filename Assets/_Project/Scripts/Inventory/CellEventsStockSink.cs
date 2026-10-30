using MediKitTwin.Common;

namespace MediKitTwin.Inventory
{
    /// <summary>재고의 알림을 CellEvents로 내보낸다. 실제 씬에서 쓰는 구현이다.</summary>
    public sealed class CellEventsStockSink : IStockEventSink
    {
        public void StockEmpty(string shelfSlotId, int orderSequence) => CellEvents.RaiseStockEmpty(shelfSlotId, orderSequence);

        public void StockRefilled(string shelfSlotId) => CellEvents.RaiseStockRefilled(shelfSlotId);
    }
}
