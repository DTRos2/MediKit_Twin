using MediKitTwin.Recipes;
using UnityEngine;

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

        /// <summary>
        /// 지그의 박스를 지그에서 떼어 호출한 쪽에 넘긴다. 키트를 다 만들어 컨베이어로 보낼 때 쓴다(FR-05).
        /// 지그에 박스가 없거나 아직 내려오는 중이면 null이다. 박스는 파괴되지 않고 담긴 구성품도 그대로이며,
        /// 공급부는 이 박스를 더 이상 자기 것으로 여기지 않으므로 받은 쪽이 바로 부모를 바꿔 옮겨야 한다.
        /// 지그가 비면 공급부가 새 박스를 다시 놓는다.
        /// </summary>
        Transform TakeBox();
    }
}
