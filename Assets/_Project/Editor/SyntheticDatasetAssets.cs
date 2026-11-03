using System.Collections.Generic;
using System.Linq;
using MediKitTwin.Inspection;
using MediKitTwin.Inventory;
using MediKitTwin.ML;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEngine;

// 합성 데이터셋을 만드는 데 필요한 설정 에셋 묶음. 창과 배치 실행이 같은 방법으로 찾는다. (FR-08)
public sealed class SyntheticDatasetAssets
{
    private const string ConfigDir = "Assets/_Project/Config";
    private const string CameraSettingsPath = ConfigDir + "/InspectionCameraSettings.asset";
    private const string RandomizationPath = ConfigDir + "/SyntheticRandomization.asset";

    public InspectionCameraSettingsSO Camera { get; private set; }
    public SyntheticRandomizationSO Randomization { get; private set; }
    public ShelfItemLooksSO Looks { get; private set; }
    public ShelfLayoutConfigSO Layout { get; private set; }
    public List<RecipeSO> Recipes { get; private set; }

    public static bool TryLoad(out SyntheticDatasetAssets assets)
    {
        assets = new SyntheticDatasetAssets
        {
            Camera = LoadOrCreate<InspectionCameraSettingsSO>(CameraSettingsPath),
            Randomization = LoadOrCreate<SyntheticRandomizationSO>(RandomizationPath),
            Looks = FindAsset<ShelfItemLooksSO>(),
            Layout = FindAsset<ShelfLayoutConfigSO>(),
            Recipes = AssetDatabase.FindAssets("t:RecipeSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<RecipeSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(r => r != null)
                .OrderBy(r => r.KitCode)
                .ToList(),
        };

        if (assets.Looks == null || assets.Layout == null)
        {
            Debug.LogError("[SyntheticDataset] ShelfItemLooks 또는 ShelfLayoutConfig 에셋을 찾지 못했다.");
            return false;
        }

        if (assets.Recipes.Count < 2)
        {
            Debug.LogError("[SyntheticDataset] 오구성 박스를 만들려면 구성품이 서로 다른 레시피가 2종 이상 필요하다.");
            return false;
        }

        return true;
    }

    private static T FindAsset<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T existing = FindAsset<T>();
        if (existing != null)
        {
            return existing;
        }

        var created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SyntheticDataset] {typeof(T).Name} 에셋이 없어 기본값으로 만들었다: {path}");
        return created;
    }
}
