using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediKitTwin.Inspection;
using MediKitTwin.ML;
using MediKitTwin.Recipes;
using UnityEngine;

// 비전 검사(FR-07) 학습용 합성 데이터셋을 만든다. (FR-08)
// 레시피대로 채운 정상 박스와 누락, 오구성 박스를 검사 카메라 시점에서 찍고, 슬롯 위치마다 잘라 PNG와 labels.csv로 저장한다.
// 같은 시드와 같은 설정은 같은 데이터셋을 만든다. 창과 배치 실행이 이 클래스를 함께 쓴다.
public static class SyntheticDatasetGenerator
{
    /// <summary>
    /// 데이터셋을 만들어 실행 폴더를 돌려준다. 중간에 취소하거나 설정이 맞지 않으면 null이다.
    /// reportProgress(표시할 글, 0~1 진행률)가 true를 돌려주면 취소한다.
    /// </summary>
    public static string Generate(SyntheticDatasetSettings settings, SyntheticDatasetAssets assets, Func<string, float, bool> reportProgress)
    {
        if (settings.TestCount % 2 != 0)
        {
            Debug.LogError("[SyntheticDataset] 테스트 박스 수는 정상과 불량을 같게 하려고 짝수여야 한다.");
            return null;
        }

        string runDir = Path.Combine(settings.OutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(runDir);

        var kits = assets.Recipes.Select(SyntheticKitSpec.FromRecipe).ToList();
        RandomizationRanges ranges = assets.Randomization.ToRanges();
        var csv = new StringBuilder();
        csv.Append(LabelCsv.Header).Append('\n');

        var splits = new[]
        {
            (split: DataSplit.Train, count: settings.TrainCount, seed: settings.Seed),
            (split: DataSplit.Validation, count: settings.ValidationCount, seed: settings.Seed + 1),
            (split: DataSplit.Test, count: settings.TestCount, seed: settings.Seed + 2),
        };

        int total = splits.Sum(s => s.count);
        int done = 0;
        int cropCount = 0;
        var perSplit = new Dictionary<DataSplit, int>();

        using (var renderer = new SyntheticBoxRenderer(assets.Camera, assets.Looks, assets.Layout, assets.Recipes, settings.BoxFloorThickness))
        {
            foreach (var entry in splits)
            {
                float ratio = entry.split == DataSplit.Test ? 0.5f : settings.TrainDefectRatio;
                List<SyntheticBoxCase> cases = SyntheticCasePlanner.Plan(kits, entry.split, entry.count, entry.seed, ratio);
                perSplit[entry.split] = 0;

                foreach (SyntheticBoxCase boxCase in cases)
                {
                    if (reportProgress != null && reportProgress($"{entry.split} {boxCase.Index + 1}/{cases.Count}", (float)done / Math.Max(1, total)))
                    {
                        Debug.LogWarning("[SyntheticDataset] 취소했다. 지금까지의 파일은 남아 있고 labels.csv는 쓰지 않았다.");
                        return null;
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
        File.WriteAllText(Path.Combine(runDir, "dataset_info.txt"), BuildInfo(settings, assets, ranges, perSplit, cropCount), new UTF8Encoding(false));

        Debug.Log($"[SyntheticDataset] 박스 {total}개, 크롭 {cropCount}장을 저장했다: {runDir}");

        // 저장한 결과가 FR-08 인수 기준(행 수와 이미지 수, 테스트 세트의 정상·불량 수)을 지키는지 바로 확인한다. (TC-08)
        DatasetVerification verification = DatasetVerifier.Verify(runDir);
        if (verification.IsValid)
        {
            Debug.Log($"[SyntheticDataset] 검증 통과: labels.csv {verification.RowCount}행 = 이미지 {verification.ImageCount}장, "
                + $"테스트 정상 {verification.GetNormalBoxCount(DataSplit.Test)} / 불량 {verification.GetDefectBoxCount(DataSplit.Test)}");
        }
        else
        {
            Debug.LogError($"[SyntheticDataset] 검증 실패:\n{string.Join("\n", verification.Errors.Take(20))}");
        }

        return runDir;
    }

    /// <summary>슬롯의 크롭이 찍힌 전체 이미지에서 차지하는 사각형. 이미지 밖으로 나가면 예외이다.</summary>
    public static RectInt GetCropRect(SyntheticDatasetAssets assets, TopDownCameraModel model, string kitCode, int slotNumber)
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

    private static string BuildInfo(SyntheticDatasetSettings settings, SyntheticDatasetAssets assets, RandomizationRanges ranges, Dictionary<DataSplit, int> perSplit, int cropCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"seed: {settings.Seed}");
        sb.AppendLine($"boxes: train={perSplit[DataSplit.Train]} validation={perSplit[DataSplit.Validation]} test={perSplit[DataSplit.Test]}");
        sb.AppendLine($"crops: {cropCount}");
        sb.AppendLine($"train_validation_defect_ratio: {settings.TrainDefectRatio}");
        sb.AppendLine($"recipes: {string.Join(",", assets.Recipes.Select(r => r.KitCode))}");
        sb.AppendLine($"camera: height={assets.Camera.HeightAboveBox} fov={assets.Camera.VerticalFovDegrees} image={assets.Camera.ImageWidth}x{assets.Camera.ImageHeight}");
        sb.AppendLine($"crop_size_m: {assets.Camera.CropSizeMeters.x}x{assets.Camera.CropSizeMeters.y}");
        sb.AppendLine($"item_lying_chance: {ranges.ItemLyingChance} lying_yaw_deg: {ranges.ItemLyingYawDegrees}");
        sb.AppendLine($"item_tilt_deg: {ranges.ItemTiltDegrees} item_edge_stand_chance: {ranges.ItemEdgeStandChance} item_flat_yaw_deg: {ranges.ItemFlatYawDegrees}");
        sb.AppendLine("classes: empty + component ids");
        return sb.ToString();
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
}
