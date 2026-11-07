using MediKitTwin.Recipes;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암 A가 다음에 만들 키트를 주문 관리에서 가져가는 접점. 로봇암 A는 한가할 때 이 인터페이스를 호출하고,
    /// 주문 관리를 직접 참조하지 않는다. (FR-01, FR-02, FR-13)
    /// </summary>
    public interface IKitSource
    {
        /// <summary>
        /// 다음 키트와 그 레시피를 돌려준다. 만들 키트가 없으면(모든 주문이 보류되었거나 끝났으면) false이다.
        /// 키트 순번(KitSequence)은 이 호출로 키트가 투입될 때 1, 2, 3, ... 순서로 부여한다.
        /// 재고 소진으로 보류되었다가 재개되는 키트는 새 KitData로 만들어 돌려주므로 누락 정답이 남지 않는다.
        /// </summary>
        bool TryGetNext(out KitData kit, out RecipeSO recipe);
    }
}
