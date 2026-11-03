using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediKitTwin.Inspection;
using MediKitTwin.ML;
using UnityEditor;
using UnityEngine;

// 비전 검사(FR-07) 학습용 합성 데이터셋을 만드는 창. (FR-08)
// 레시피대로 채운 정상 박스와 누락, 오구성 박스를 검사 카메라 시점에서 찍고, 슬롯 위치마다 잘라 PNG와 labels.csv로 저장한다.
// 같은 시드와 같은 설정은 같은 데이터셋을 만든다. 만드는 일은 SyntheticDatasetGenerator가 하고, 이 창은 설정 입력과 진행 표시만 맡는다.
public sealed class SyntheticDatasetWindow : EditorWindow
{
    private const string PrefKeyOutput = "MediKitTwin.SyntheticDataset.OutputRoot";

    private readonly SyntheticDatasetSettings _settings = new SyntheticDatasetSettings();

    [MenuItem("Tools/MediKit/Synthetic Dataset (Inspection)")]
    private static void Open()
    {
        GetWindow<SyntheticDatasetWindow>("Synthetic Dataset");
    }

    private void OnEnable()
    {
        _settings.OutputRoot = EditorPrefs.GetString(PrefKeyOutput, SyntheticDatasetSettings.DefaultOutputRoot());
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "정상, 누락, 오구성 박스를 검사 카메라 시점에서 찍어 슬롯별 크롭 PNG와 labels.csv를 저장한다. 결과는 프로젝트 밖 폴더에 쌓이며 커밋하지 않는다.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _settings.OutputRoot = EditorGUILayout.TextField("출력 폴더", _settings.OutputRoot);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PrefKeyOutput, _settings.OutputRoot);
        }

        _settings.Seed = EditorGUILayout.IntField("시드", _settings.Seed);
        _settings.TrainCount = Mathf.Max(0, EditorGUILayout.IntField("학습 박스 수", _settings.TrainCount));
        _settings.ValidationCount = Mathf.Max(0, EditorGUILayout.IntField("검증 박스 수", _settings.ValidationCount));
        _settings.TestCount = Mathf.Max(0, EditorGUILayout.IntField("테스트 박스 수 (짝수)", _settings.TestCount));
        _settings.TrainDefectRatio = EditorGUILayout.Slider("학습·검증 불량 비율", _settings.TrainDefectRatio, 0f, 1f);
        _settings.BoxFloorThickness = EditorGUILayout.FloatField("박스 바닥 두께(m)", _settings.BoxFloorThickness);

        int crops = (_settings.TrainCount + _settings.ValidationCount + _settings.TestCount) * 4;
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
            if (!SyntheticDatasetAssets.TryLoad(out SyntheticDatasetAssets assets))
            {
                return;
            }

            if (preview)
            {
                RenderPreview(assets);
            }
            else
            {
                string runDir = SyntheticDatasetGenerator.Generate(_settings, assets, ReportProgress);
                if (runDir != null)
                {
                    EditorUtility.RevealInFinder(runDir);
                }
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

    // true를 돌려주면 사용자가 취소한 것이다.
    private static bool ReportProgress(string label, float fraction)
    {
        return EditorUtility.DisplayCancelableProgressBar("합성 데이터 생성", label, fraction);
    }

    // ───────────────────────── 샘플 ─────────────────────────

    private void RenderPreview(SyntheticDatasetAssets assets)
    {
        string dir = Path.Combine(_settings.OutputRoot, "_preview");
        Directory.CreateDirectory(dir);

        var kits = assets.Recipes.Select(SyntheticKitSpec.FromRecipe).ToList();
        List<SyntheticBoxCase> planned = SyntheticCasePlanner.Plan(kits, DataSplit.Train, 60, _settings.Seed);

        using (var renderer = new SyntheticBoxRenderer(assets.Camera, assets.Looks, assets.Layout, assets.Recipes, _settings.BoxFloorThickness))
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
    private static Texture2D DrawCropRects(Texture2D full, SyntheticBoxCase boxCase, SyntheticDatasetAssets assets, TopDownCameraModel model)
    {
        var copy = new Texture2D(full.width, full.height, TextureFormat.RGBA32, false);
        copy.SetPixels(full.GetPixels());

        for (int slot = 1; slot <= boxCase.SlotCount; slot++)
        {
            RectInt rect = SyntheticDatasetGenerator.GetCropRect(assets, model, boxCase.KitCode, slot);
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
}
