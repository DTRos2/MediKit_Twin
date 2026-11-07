using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Demo;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 선반 재고 패널을 열린 씬(Scene_Cell)의 계층에 올린다. 패널은 프리팹(PF_ShelfStockPanel)이고, 씬의 DashboardCanvas 아래에
    /// 그 인스턴스가 눈에 보이는 오브젝트로 놓인다. 칸 타일은 씬 선반(ShelfStockPresenter) 아래 칸 화면(ShelfCellView)의 칸 ID마다,
    /// 키트 종류 버튼은 프로젝트의 레시피(RecipeSO) 키트 코드마다 하나씩 만든다. 재고와 수동 키팅은 실행할 때 인터페이스로 찾으므로 이을 참조가 없다.
    /// 놓는 순서는 모든 패널이 같아서 DashboardPanelPlacer가 맡고, 메뉴와 배치 모드 입구는 DashboardPanelsSetup이 맡는다.
    /// </summary>
    public static class ShelfStockPanelSetup
    {
        public static bool Apply(bool isRebuild)
        {
            DashboardPanelPlacer.Place<ShelfStockPanelPresenter>(
                ProjectAssetPaths.ShelfStockPanelPrefab, BuildPrefab, isRebuild);
            return true;
        }

        private static UnityEngine.GameObject BuildPrefab()
        {
            var slotIds = new List<string>();
            if (SceneObjectFinder.TryFindSingle(out ShelfStockPresenter shelf, "선반 재고(ShelfStockPresenter)"))
            {
                foreach (ShelfCellView view in shelf.GetComponentsInChildren<ShelfCellView>(true))
                {
                    slotIds.Add(view.ShelfSlotId);
                }
            }

            var kitCodes = new List<string>();
            foreach (RecipeSO recipe in SceneObjectFinder.FindAssets<RecipeSO>())
            {
                if (!kitCodes.Contains(recipe.KitCode))
                {
                    kitCodes.Add(recipe.KitCode);
                }
            }

            return ShelfStockPanelPrefabBuilder.Build(slotIds, kitCodes).gameObject;
        }
    }
}
