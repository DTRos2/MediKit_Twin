using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// PF_Shelf의 선반 물건(Item_*)에 구성품 표시(KitItemLabel)를 붙인다. (FR-06 슬롯 감지 센서가 구성품 ID를 읽는다)
    /// 프리팹을 다시 만들지 않고 기존 오브젝트에 컴포넌트만 더한다. 다시 만들면 내부 오브젝트 ID가 모두 바뀌어
    /// Scene_Cell의 Shelf 인스턴스에 걸린 오버라이드(레이어, Base 위치 등)가 사라지기 때문이다.
    /// 구성품 ID는 CellSceneBuilder와 같은 방식으로 레시피의 선반 칸 ID에서 읽는다. 이미 있으면 ID만 맞춘다.
    /// </summary>
    public static class ShelfItemLabelSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/PF_Shelf.prefab";
        private const string CellPrefix = "Shelf_";
        private const string ItemPrefix = "Item_";

        [MenuItem("Tools/MediKit/Add Item Labels To Shelf Prefab")]
        public static void AddLabels()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[ShelfItemLabelSetup] 프리팹이 없습니다: {PrefabPath}");
                return;
            }

            Dictionary<string, string> componentByShelfSlot = LoadComponentsByShelfSlot();
            if (componentByShelfSlot.Count == 0)
            {
                Debug.LogError("[ShelfItemLabelSetup] RecipeSO 에셋이 없어 구성품 ID를 알 수 없습니다.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int added = 0;
                int updated = 0;
                int unchanged = 0;
                int skipped = 0;

                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    Transform cell = item.parent;
                    if (!item.name.StartsWith(ItemPrefix) || cell == null || !cell.name.StartsWith(CellPrefix))
                    {
                        continue;
                    }

                    string shelfSlotId = cell.name.Substring(CellPrefix.Length);
                    if (!componentByShelfSlot.TryGetValue(shelfSlotId, out string componentId))
                    {
                        Debug.LogWarning($"[ShelfItemLabelSetup] 레시피에 선반 칸 {shelfSlotId}의 구성품이 없어 {item.name}은 건너뜁니다.");
                        skipped++;
                        continue;
                    }

                    var label = item.GetComponent<KitItemLabel>();
                    if (label == null)
                    {
                        item.gameObject.AddComponent<KitItemLabel>().Configure(componentId);
                        added++;
                    }
                    else if (label.ComponentId != componentId)
                    {
                        label.Configure(componentId);
                        updated++;
                    }
                    else
                    {
                        unchanged++;
                    }
                }

                if (added + updated > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"[ShelfItemLabelSetup] PF_Shelf 선반 물건 표시: 추가 {added}, ID 수정 {updated}, 변경 없음 {unchanged}, 건너뜀 {skipped}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 레시피(RecipeSO)에서 선반 칸 ID → 구성품 ID를 읽는다. CellSceneBuilder가 물건을 만들 때 쓰는 값과 같다.
        private static Dictionary<string, string> LoadComponentsByShelfSlot()
        {
            var result = new Dictionary<string, string>();
            foreach (string guid in AssetDatabase.FindAssets("t:RecipeSO"))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe == null)
                {
                    continue;
                }

                foreach (RecipeItem item in recipe.Items)
                {
                    result[item.ShelfSlotId] = item.ComponentId;
                }
            }

            return result;
        }
    }
}
