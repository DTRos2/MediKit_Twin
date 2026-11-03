using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 키트 박스 슬롯 사이에 칸막이를 세우고, 칸 안에 들어가도록 선반 물건을 줄인다. 한 번 눌러 세 가지를 맞춘다.
    ///   1) 설정 에셋(KitBoxPartitionConfig)을 만들고 레시피를 채운다. 이미 있으면 값은 그대로 둔다.
    ///   2) PF_KittingJig의 KitBox에 칸막이(Divider_N)를 새로 붙인다.
    ///   3) PF_Shelf의 물건(Item_*) 중 가장 긴 두 변의 대각선이 칸 상한(MaxItemFaceDiagonal)을 넘는 것을 같은 비율로 줄인다. 바닥 높이는 지킨다.
    /// 프리팹을 다시 만들지 않고 안의 오브젝트만 고친다. 다시 만들면 내부 오브젝트 ID가 바뀌어
    /// Scene_Cell 인스턴스에 걸린 오버라이드가 사라지기 때문이다. 몇 번을 눌러도 결과가 같다.
    /// </summary>
    public static class KitBoxPartitionSetup
    {
        private const string KitBoxName = "KitBox";
        private const string CellPrefix = "Shelf_";
        private const string ItemPrefix = "Item_";

        // 줄일 배율이 1에서 이 비율 이내로만 모자라면 이미 맞은 것으로 본다. 부동소수점 오차로 매번 다시 줄이지 않으려는 값이다.
        private const float AlreadyFitsScaleTolerance = 1e-3f;

        [MenuItem("Tools/MediKit/Add Kit Box Partitions")]
        public static void Apply()
        {
            KitBoxPartitionConfigSO config = EnsureConfig();
            if (config == null)
            {
                return;
            }

            ShelfLayoutConfigSO shelfLayout = AssignToShelfLayout(config);
            AddPartitionsToJig(config);
            if (shelfLayout != null)
            {
                FitShelfItems(shelfLayout);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static KitBoxPartitionConfigSO EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<KitBoxPartitionConfigSO>(ProjectAssetPaths.KitBoxPartitionConfig);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<KitBoxPartitionConfigSO>();
                AssetDatabase.CreateAsset(config, ProjectAssetPaths.KitBoxPartitionConfig);
                Debug.Log($"[KitBoxPartitionSetup] 설정 에셋을 만들었습니다: {ProjectAssetPaths.KitBoxPartitionConfig}");
            }

            if (config.Recipes.Count == 0)
            {
                var recipes = new List<RecipeSO>();
                foreach (string guid in AssetDatabase.FindAssets("t:RecipeSO"))
                {
                    recipes.Add(AssetDatabase.LoadAssetAtPath<RecipeSO>(AssetDatabase.GUIDToAssetPath(guid)));
                }

                if (recipes.Count == 0)
                {
                    Debug.LogError("[KitBoxPartitionSetup] RecipeSO 에셋이 없어 슬롯 좌표를 알 수 없습니다.");
                    return null;
                }

                config.SetRecipes(recipes.ToArray());
                EditorUtility.SetDirty(config);
            }

            return config;
        }

        // 선반 설정이 칸막이 설정을 가리켜야 물건 크기 상한(CreateItemSizeLimit)에 칸 크기가 반영된다. 선반 설정이 없으면 null이다.
        private static ShelfLayoutConfigSO AssignToShelfLayout(KitBoxPartitionConfigSO config)
        {
            var shelfLayout = AssetDatabase.LoadAssetAtPath<ShelfLayoutConfigSO>(ProjectAssetPaths.ShelfLayoutConfig);
            if (shelfLayout == null)
            {
                Debug.LogError($"[KitBoxPartitionSetup] 선반 설정이 없습니다: {ProjectAssetPaths.ShelfLayoutConfig}");
                return null;
            }

            var serialized = new SerializedObject(shelfLayout);
            SerializedProperty property = serialized.FindProperty("_kitBoxPartition");
            if (property.objectReferenceValue != config)
            {
                property.objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(shelfLayout);
            }

            return shelfLayout;
        }

        private static void AddPartitionsToJig(KitBoxPartitionConfigSO config)
        {
            string path = ProjectAssetPaths.KittingJigPrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"[KitBoxPartitionSetup] 프리팹이 없습니다: {path}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform kitBox = root.transform.Find(KitBoxName);
                if (kitBox == null)
                {
                    Debug.LogError($"[KitBoxPartitionSetup] {path} 아래에 {KitBoxName}이(가) 없습니다.");
                    return;
                }

                int built = KitBoxPartitionBuilder.Rebuild(kitBox, config);
                if (built < 0)
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[KitBoxPartitionSetup] PF_KittingJig 칸막이 {built}개를 붙였습니다 (두께 {config.ThicknessM * 1000f:0.0}mm, 높이 {config.HeightM * 1000f:0}mm).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // 크기 상한(그리퍼가 집을 수 있는 크기와 칸에 끼지 않는 크기)은 선반 설정이 만든다. 빌더, 합성 데이터 생성기와 같은 규칙을 쓰려고
        // 여기서 따로 계산하지 않고 그 한도(GraspSizeLimit)에 현재 크기를 물어 줄일 배율을 받는다.
        private static void FitShelfItems(ShelfLayoutConfigSO shelfLayout)
        {
            string path = ProjectAssetPaths.ShelfPrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"[KitBoxPartitionSetup] 프리팹이 없습니다: {path}");
                return;
            }

            GraspSizeLimit limit = shelfLayout.CreateItemSizeLimit();
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int shrunk = 0;
                int unchanged = 0;
                float largestDiagonalBefore = 0f;
                float largestDiagonalAfter = 0f;

                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    Transform cell = item.parent;
                    var itemRenderer = item.GetComponent<Renderer>();
                    if (!item.name.StartsWith(ItemPrefix) || cell == null || !cell.name.StartsWith(CellPrefix) || itemRenderer == null)
                    {
                        continue;
                    }

                    Bounds before = itemRenderer.bounds;
                    largestDiagonalBefore = Mathf.Max(largestDiagonalBefore, DiagonalOf(before.size));

                    float scale = limit.ScaleFor(before.size.x, before.size.y, before.size.z);
                    if (scale >= 1f - AlreadyFitsScaleTolerance)
                    {
                        unchanged++;
                        largestDiagonalAfter = Mathf.Max(largestDiagonalAfter, DiagonalOf(before.size));
                        continue;
                    }

                    // 모양을 지키려고 세 변을 같은 비율로 줄이고, 바닥 높이를 지키려고 줄어든 만큼 내린 자리를 다시 올린다.
                    item.localScale *= scale;
                    float lift = before.min.y - itemRenderer.bounds.min.y;
                    item.position += Vector3.up * lift;
                    shrunk++;

                    largestDiagonalAfter = Mathf.Max(largestDiagonalAfter, DiagonalOf(itemRenderer.bounds.size));
                }

                if (shrunk > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                Debug.Log(
                    $"[KitBoxPartitionSetup] PF_Shelf 물건 크기 상한(가장 긴 변 {limit.MaxSize * 100f:0.00}cm, 두 긴 변의 대각선 {limit.MaxFaceDiagonal * 100f:0.00}cm): " +
                    $"줄임 {shrunk}, 변경 없음 {unchanged}, 가장 큰 대각선 {largestDiagonalBefore * 100f:0.00}cm -> {largestDiagonalAfter * 100f:0.00}cm");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static float DiagonalOf(Vector3 size)
        {
            return GraspSizeLimit.FaceDiagonal(size.x, size.y, size.z);
        }
    }
}
