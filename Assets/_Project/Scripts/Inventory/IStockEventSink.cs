namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 재고가 바깥에 알리는 일. 재고가 CellEvents(정적 이벤트)를 직접 부르지 않고 이 인터페이스로만 알려서,
    /// 이벤트 버스 없이도 테스트할 수 있다.
    /// </summary>
    public interface IStockEventSink
    {
        void StockEmpty(string shelfSlotId, int orderSequence);

        void StockRefilled(string shelfSlotId);
    }
}
