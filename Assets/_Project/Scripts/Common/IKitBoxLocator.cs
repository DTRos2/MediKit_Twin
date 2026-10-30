using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 지그 위 박스의 슬롯 위치를 알려 주는 접점. 박스를 놓고 치우는 일은 IBoxSupply가 맡고,
    /// 이 인터페이스는 지금 놓인 박스에서 슬롯의 월드 좌표를 계산해 준다. (FR-03)
    /// </summary>
    public interface IKitBoxLocator
    {
        /// <summary>
        /// 박스 기준 로컬 좌표(RecipeItem.SlotLocalPosition)에 해당하는 월드 자리를 돌려준다.
        /// 지그에 박스가 없거나 아직 내려오는 중이면 false이다.
        /// </summary>
        bool TryGetSlotTarget(Vector3 slotLocalPosition, out BoxSlotTarget target);
    }
}
