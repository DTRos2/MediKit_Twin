using System.IO;
using UnityEngine;

// 합성 데이터셋을 만들 때의 설정. 창(SyntheticDatasetWindow)과 배치 실행(SyntheticDatasetBatch)이 같은 값과 같은 기본값을 쓴다. (FR-08)
public sealed class SyntheticDatasetSettings
{
    public string OutputRoot = DefaultOutputRoot();
    public int Seed = 20261102;
    public int TrainCount = 800;
    public int ValidationCount = 100;

    // 정상과 불량을 같은 수로 두려고 짝수여야 한다.
    public int TestCount = 200;
    public float TrainDefectRatio = 0.5f;
    public float BoxFloorThickness = 0.01f;

    // 프로젝트 밖(루트)의 Datasets 폴더. 이 폴더는 커밋하지 않는다.
    public static string DefaultOutputRoot()
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Datasets", "InspectionSynthetic");
    }
}
