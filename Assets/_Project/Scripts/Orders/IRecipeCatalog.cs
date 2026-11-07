using System.Collections.Generic;
using MediKitTwin.Recipes;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 키트 코드로 레시피를 찾는 곳. 주문 관리가 이 인터페이스만 보므로 레시피를 어디서 모으는지(인스펙터 목록, 에셋 검색)는 바꿔 끼울 수 있다.
    /// </summary>
    public interface IRecipeCatalog
    {
        /// <summary>등록된 키트 코드(대문자). 주문 파일 검증과 기본 주문 세트가 같은 목록을 쓴다.</summary>
        IReadOnlyCollection<string> KitCodes { get; }

        /// <summary>레시피가 없으면 false이다. 호출하는 쪽이 주문을 보류하고 경고한다. (FR-02 예외 처리)</summary>
        bool TryGetRecipe(string kitCode, out RecipeSO recipe);
    }
}
