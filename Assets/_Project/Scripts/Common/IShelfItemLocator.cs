namespace MediKitTwin.Common
{
    /// <summary>
    /// 선반 칸에서 집을 물건의 위치를 알려 주는 접점. 로봇암 A는 선반 화면을 직접 참조하지 않고 이 인터페이스로만 위치를 묻는다.
    /// 재고 수량은 IStock이 맡고, 이 인터페이스는 눈에 보이는 물건의 위치만 맡는다. (FR-03)
    /// </summary>
    public interface IShelfItemLocator
    {
        /// <summary>칸에서 가장 먼저 집을 물건을 돌려준다. 칸에 물건이 없거나 알 수 없는 칸이면 false이다.</summary>
        bool TryGetPickItem(string shelfSlotId, out ShelfPickItem item);
    }
}
