using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediKitTwin.Inspection;
using MediKitTwin.Inventory;
using MediKitTwin.ML;
using MediKitTwin.Recipes;
using UnityEditor;
using UnityEngine;

// 비전 검사(FR-07) 학습용 합성 데이터셋을 만드는 창. (FR-08)
// 레시피대로 채운 정상 박스와 누락, 오구성 박스를 검사 카메라 시점에서 찍고, 슬롯 위치마다 잘라 PNG와 labels.csv로 저장한다.
// 같은 시드와 같은 설정은 같은 데이터셋을 만든다.
public sealed class SyntheticDatasetWindow : EditorWindow
{
    private const string ConfigDir = "Assets/_Project/Config";
    private const string CameraSettingsPath = ConfigDir + "/InspectionCameraSettings.asset";
    private const string RandomizationPath = ConfigDir + "/SyntheticRandomization.asset";
    private const string PrefKeyOutput = "MediKitTwin.SyntheticDataset.OutputRoot";

    private string _outputRoot;
    private int _seed = 20261102;
    private int _trainCount = 800;
    private int _validationCount = 100;
    private int _testCount = 200;
    private float _trainDefectRatio = 0.5f;
    private float _boxFloorThickness = 0.01f;

    [MenuItem("Tools/MediKit/Synthetic Dataset (Inspection)")]
    private static void Open()
    {
        GetWindow<SyntheticDatasetWindow>("Synthetic Dataset");
    }

    private void OnEnable()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        _outputRoot = EditorPrefs.GetString(PrefKeyOutput, Path.Combine(projectRoot, "Datasets", "InspectionSynthetic"));
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "정상, 누락, 오구성 박스를 검사 카메라 시점에서 찍어 슬롯별 크롭 PNG와 labels.csv를 저장한다. 결과는 프로젝트 밖 폴더에 쌓이며 커밋하지 않는다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _outputRoot = EditorGUILayout.TextField("출력 폴더", _outputRoot);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PrefKeyOutput, _outputRoot);
        }

        _seed = EditorGUILayout.IntField("시드", _seed);
        _trainCount = Mathf.Max(0, EditorGUILayout.IntField("학습 박스 수", _trainCount));
        _validationCount = Mathf.Max(0, EditorGUILayout.IntField("검증 박스 수", _validationCount));
        _testCount = Mathf.Max(0, EditorGUILayout.IntField("테스트 박스 수 (짝수)", _testCount));
        _trainDefectRatio = EditorGUILayout.Slider("학습·검증 불량 비율", _trainDefectRatio, 0f, 1f);
        _boxFloorThickness = EditorGUILayout.FloatField("박스 바닥 두께(m)", _boxFloorThickness);

        int crops = (_trainCount + _validationCount + _testCount) * 4;
        EditorGUILayout.LabelField("예상 크롭 수", $"약 {crops}장 (박스당 슬롯 수만큼)");

        EditorGUILayout.Space();
        if (GUILayout.Button("샘플 촬영 (정상·누락·오구성 1건씩, 크롭 영역 확인용)"))
        {
            Run(preview: true);
        }

        if (GUILayout.Button("데이터셋 생성"))
        {
            Run(preview: false);
        }
    }

    private void Run(bool preview)
    {
        try
        {
            if (!TryLoadAssets(out Assets assets))
            {
                return;
            }

            if (preview)
            {
                RenderPreview(assets);
            }
            else
            {
                GenerateDataset(assets);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SyntheticDataset] 실패: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ───────────────────────── 샘플 ─────────────────────────

    private void RenderPreview(Assets assets)
    {
        string dir = Path.Combine(_outputRoot, "_preview");
        Directory.CreateDirectory(dir);

        var kits = assets.Recipes.Select(SyntheticKitSpec.FromRecipe).ToList();
        List<SyntheticBoxCase> planned = SyntheticCasePlanner.Plan(kits, DataSplit.Train, 60, _seed);

        using (var renderer = new SyntheticBoxRenderer(assets.Camera, assets.Looks, assets.Layout, assets.Recipes, _boxFloorThickness))
        {
            foreach (BoxVariant variant in Enum.GetValues(typeof(BoxVariant)))
            {
                SyntheticBoxCase boxCase = planned.First(c => c.Variant == variant);
                SyntheticVariation variation = SyntheticVariation.Create(boxCase.Seed, assets.Randomization.ToRanges(), boxCase.SlotCount);
                Texture2D full = renderer.Render(boxCase, variation);
                try
                {
                    Texture2D overlay = DrawCropRects(full, boxCase, assets, renderer.CameraModel);
                    string path = Path.Combine(dir, $"{LabelCsv.ToSnakeCase(variant)}_{boxCase.KitCode}_case{boxCase.Index:D5}.png");
                    File.WriteAllBytes(path, overlay.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(overlay);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(full);
                }
            }
        }

        Debug.Log($"[SyntheticDataset] 샘플 3장을 저장했다: {dir}");
        EditorUtility.RevealInFinder(dir);
    }

    // 크롭 사각형을 빨강(정상 슬롯) 또는 노랑(바뀐 슬롯)으로 그려 눈으로 확인한다.
    private static Texture2D DrawCropRects(Texture2D full, SyntheticBoxCase boxCase, Assets assets, TopDownCameraModel model)
    {
        var copy = new Texture2D(full.width, full.height, TextureFormat.RGBA32, false);
        copy.SetPixels(full.GetPixels());

        for (int slot = 1; slot <= boxCase.SlotCount; slot++)
        {
            RectInt rect = GetCropRect(assets, model, boxCase.KitCode, slot);
            Color color = slot == boxCase.AffectedSlotNumber ? Color.yellow : Color.red;
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                copy.SetPixel(x, rect.yMin, color);
                copy.SetPixel(x, rect.yMax - 1, color);
            }

            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                copy.SetPixel(rect.xMin, y, color);
                copy.SetPixel(rect.xMax - 1, y, color);
            }
        }

        copy.Apply();
        return copy;
    }

    // ───────────────────────── 데이터셋 ─────────────────────────

    private void GenerateDataset(Assets assets)
    {
        if (_testCount % 2 != 0)
        {
            Debug.LogError("[SyntheticDataset] 테스트 박스 수는 정상과 불량을 같게 하려고 짝수여야 한다.");
            return;
        }

        string runDir = Path.Combine(_outputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDir);

        var kits = assets.Recipes.Select(SyntheticKitSpec.FromRecipe).ToList();
        RandomizationRanges ranges = assets.Randomization.ToRanges();
        var csv = new StringBuilder();
        csv.Append(LabelCsv.Header).Append('\n');

        var splits = new[]
        {
            (split: DataSplit.Train, count: _trainCount, seed: _seed),
            (split: DataSplit.Validation, count: _validationCount, seed: _seed + 1),
            (split: DataSplit.Test, count: _testCount, seed: _seed + 2),
        };

        int total = splits.Sum(s => s.count);
        int done = 0;
        int cropCount = 0;
        var perSplit = new Dictionary<DataSplit, int>();

        using (var renderer = new SyntheticBoxRenderer(assets.Camera, assets.Looks, assets.Layout, assets.Recipes, _boxFloorThickness))
        {
            foreach (var entry in splits)
            {
                float ratio = entry.split == DataSplit.Test ? 0.5f : _trainDefectRatio;
                List<SyntheticBoxCase> cases = SyntheticCasePlanner.Plan(kits, entry.split, entry.count, entry.seed, ratio);
                perSplit[entry.split] = 0;

                foreach (SyntheticBoxCase boxCase in cases)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("합성 데이터 생성", $"{entry.split} {boxCase.Index + 1}/{cases.Count}", (float)done / Math.Max(1, total)))
                    {
                        Debug.LogWarning("[SyntheticDataset] 취소했다. 지금까지의 파일은 남아 있고 labels.csv는 쓰지 않았다.");
                        return;
                    }

                    SyntheticVariation variation = SyntheticVariation.Create(boxCase.Seed, ranges, boxCase.SlotCount);
                    Texture2D full = renderer.Render(boxCase, variation);
                    try
                    {
                        foreach (SlotCropRecord record in LabelCsv.CreateRecords(boxCase))
                        {
                            RectInt rect = GetCropRect(assets, renderer.CameraModel, boxCase.KitCode, record.SlotNumber);
                            SaveCrop(full, rect, Path.Combine(runDir, record.ImagePath));
                            csv.Append(LabelCsv.FormatRow(record)).Append('\n');
                            cropCount++;
                        }
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(full);
                    }

                    perSplit[entry.split]++;
                    done++;
                }
            }
        }

        // labels.csv는 모든 이미지를 저장한 뒤 한 번에 쓴다. 중간에 멈추면 이미지와 어긋난 labels.csv가 남지 않는다.
        File.WriteAllText(Path.Combine(runDir, "labels.csv"), csv.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(runDir, "dataset_info.txt"), BuildInfo(assets, perSplit, cropCount), new UTF8Encoding(false));

        Debug.Log($"[SyntheticDataset] 박스 {total}개, 크롭 {cropCount}장을 저장했다: {runDir}");
        EditorUtility.RevealInFinder(runDir);
    }

    private string BuildInfo(Assets assets, Dictionary<DataSplit, int> perSplit, int cropCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"seed: {_seed}");
        sb.AppendLine($"boxes: train={perSplit[DataSplit.Train]} validation={perSplit[DataSplit.Validation]} test={perSplit[DataSplit.Test]}");
        sb.AppendLine($"crops: {cropCount}");
        sb.AppendLine($"train_validation_defect_ratio: {_trainDefectRatio}");
        sb.AppendLine($"recipes: {string.Join(",", assets.Recipes.Select(r => r.KitCode))}");
        sb.AppendLine($"camera: height={assets.Camera.HeightAboveBox} fov={assets.Camera.VerticalFovDegrees} image={assets.Camera.ImageWidth}x{assets.Camera.ImageHeight}");
        sb.AppendLine($"crop_size_m: {assets.Camera.CropSizeMeters.x}x{assets.Camera.CropSizeMeters.y}");
        sb.AppendLine("classes: empty + component ids");
        return sb.ToString();
    }

    private static RectInt GetCropRect(Assets assets, TopDownCameraModel model, string kitCode, int slotNumber)
    {
        RecipeSO recipe = assets.Recipes.First(r => r.KitCode == kitCode);
        if (!recipe.TryGetItemBySlot(slotNumber, out RecipeItem item))
        {
            throw new InvalidOperationException($"{recipe.name}: 슬롯 {slotNumber}이 없다.");
        }

        var center = new Vector3(item.SlotLocalPosition.x, assets.Camera.CropPlaneLocalY, item.SlotLocalPosition.z);
        if (!model.TryGetCropRect(center, assets.Camera.CropSizeMeters, out RectInt rect))
        {
            throw new InvalidOperationException($"{recipe.name} 슬롯 {slotNumber}의 크롭이 이미지 밖으로 나간다. 검사 카메라 설정(높이, 시야각, 이미지 크기)을 확인한다.");
        }

        return rect;
    }

    private static void SaveCrop(Texture2D full, RectInt rect, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var crop = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
        try
        {
            crop.SetPixels(full.GetPixels(rect.x, rect.y, rect.width, rect.height));
            crop.Apply();
            File.WriteAllBytes(path, crop.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(crop);
        }
    }

    // ───────────────────────── 에셋 ─────────────────────────

    private sealed class Assets
    {
        public InspectionCameraSettingsSO Camera;
        public SyntheticRandomizationSO Randomization;
        public ShelfItemLooksSO Looks;
        public ShelfLayoutConfigSO Layout;
        public List<RecipeSO> Recipes;
    }

    private static bool TryLoadAssets(out Assets assets)
    {
        assets = new Assets
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

    private static T FindAsset<T>() where T : UnityEngine.Object
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

        var created = CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SyntheticDataset] {typeof(T).Name} 에셋이 없어 기본값으로 만들었다: {path}");
        return created;
    }
}
