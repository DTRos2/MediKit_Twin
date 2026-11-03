using System;
using MediKitTwin.Recipes;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 구역이 박스를 찍는 방법과 레시피 조회를 넘겨 줘야 동작하는 주 판정기. InspectionStation이 Awake에서 한 번 연결한다.
    /// 판정기(IInspector)는 박스의 위치를 모르기 때문에 이 접점으로 크롭 공급원을 받는다.
    /// </summary>
    public interface IStationBoundInspector
    {
        void Bind(Func<string, RecipeSO> findRecipe, ISlotCropSource cropSource);
    }
}
