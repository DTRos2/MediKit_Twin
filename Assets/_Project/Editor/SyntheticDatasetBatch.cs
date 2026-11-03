using System;
using UnityEditor;
using UnityEngine;

// 창을 열지 않고 합성 데이터셋을 만드는 배치 실행 진입점. (FR-08)
// 예: Unity.exe -batchmode -projectPath <경로> -executeMethod SyntheticDatasetBatch.Generate -datasetOutput <폴더> -datasetSeed 20261103
// 촬영에 그리기 장치가 필요하므로 -nographics 없이 실행한다. 끝나면 성공 0, 실패 1로 종료한다.
// 인자를 주지 않으면 창과 같은 기본값(SyntheticDatasetSettings)을 쓴다.
public static class SyntheticDatasetBatch
{
    private const string OutputArg = "-datasetOutput";
    private const string SeedArg = "-datasetSeed";
    private const string TrainArg = "-datasetTrain";
    private const string ValidationArg = "-datasetValidation";
    private const string TestArg = "-datasetTest";

    private const int SuccessExitCode = 0;
    private const int FailureExitCode = 1;

    public static void Generate()
    {
        int exitCode = FailureExitCode;
        try
        {
            if (SyntheticDatasetAssets.TryLoad(out SyntheticDatasetAssets assets))
            {
                string runDir = SyntheticDatasetGenerator.Generate(ReadSettings(Environment.GetCommandLineArgs()), assets, reportProgress: null);
                exitCode = runDir != null ? SuccessExitCode : FailureExitCode;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SyntheticDataset] 실패: {e.Message}\n{e.StackTrace}");
        }

        EditorApplication.Exit(exitCode);
    }

    private static SyntheticDatasetSettings ReadSettings(string[] args)
    {
        var settings = new SyntheticDatasetSettings();
        settings.OutputRoot = ValueOf(args, OutputArg) ?? settings.OutputRoot;
        settings.Seed = IntValueOf(args, SeedArg, settings.Seed);
        settings.TrainCount = IntValueOf(args, TrainArg, settings.TrainCount);
        settings.ValidationCount = IntValueOf(args, ValidationArg, settings.ValidationCount);
        settings.TestCount = IntValueOf(args, TestArg, settings.TestCount);
        return settings;
    }

    private static int IntValueOf(string[] args, string name, int fallback)
    {
        string text = ValueOf(args, name);
        return text != null && int.TryParse(text, out int value) ? value : fallback;
    }

    // "-이름 값" 형태에서 값. 없으면 null.
    private static string ValueOf(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
