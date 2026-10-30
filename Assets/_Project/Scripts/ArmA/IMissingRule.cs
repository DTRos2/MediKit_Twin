using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키트에서 빠뜨릴 구성품을 정하는 규칙. 키팅은 이 인터페이스만 보므로, 규칙을 바꾸거나 시연용으로 고정해도 키팅은 바뀌지 않는다.
    /// </summary>
    public interface IMissingRule
    {
        /// <summary>빠뜨릴 구성품의 슬롯 번호. 누락 없이 모두 담으면 null이다.</summary>
        int? SelectMissingSlot(KitData kit, RecipeSO recipe);
    }
}
