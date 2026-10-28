using MediKitTwin.Recipes;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 지그 위 박스를 공급하고 치우는 접점. 로봇암 A와 주문 관리는 박스 공급부를 직접 참조하지 않는다. (FR-02, FR-13)
    /// </summary>
    public interface IBoxSupply
    {
        /// <summary>레시피의 박스 크기에 맞는 빈 박스를 지그에 놓는다. 키트를 시작할 때 호출한다.</summary>
        void PlaceBox(KitData kit, BoxSize size);

        /// <summary>
        /// 지그의 박스를 치운다. 재고 소진으로 반쯤 담긴 박스를 제거할 때 쓴다(스크립트 처리, 로봇 동작 없음).
        /// 담겨 있던 구성품을 IStock.Return으로 되돌리는 일은 호출하는 쪽이 한다.
        /// 보류된 주문이 재개되면 PlaceBox로 새 박스를 놓는다.
        /// </summary>
        void RemoveBox();
    }
}
