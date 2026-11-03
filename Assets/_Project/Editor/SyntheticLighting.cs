using UnityEngine;

// 합성 이미지를 찍을 때의 조명 세기 기준값(환경광, 주광, 보조광). 랜덤화는 이 값에 배율을 곱한다.
// 값이 틀리면 칸막이·벽의 어두운 옆면 밝기가 씬과 달라진다. 검사 카메라는 그림자를 끄지만 빛을 등진 옆면은 어둡게 찍히는데,
// 학습 이미지에서 그 면이 씬보다 밝으면 빈 슬롯 가장자리의 짙은 띠를 누운 구성품으로 읽는다. 값을 바꿀 때는 씬 촬영과 같은 위치의 밝기를 비교한다.
public sealed class SyntheticLighting
{
    public SyntheticLighting(Color ambient, float keyIntensity, float fillIntensity)
    {
        Ambient = ambient;
        KeyIntensity = keyIntensity;
        FillIntensity = fillIntensity;
    }

    /// <summary>
    /// 씬 촬영의 밝기에 맞춘 기준값. 학습 데이터를 만들 때 쓴다.
    /// 같은 정상 박스를 흔들림 없이 합성해, 씬 촬영 사진의 여섯 위치(바닥, 위·왼쪽·아래·오른쪽 벽 안쪽, 벽 윗면)의 밝기와 비교해 정했다.
    /// 합성/씬이 0.90~1.01배이다. 환경광 0.40, 주광 1.4, 보조광 0.5였을 때는 빛을 등진 아래·오른쪽 벽 안쪽이 씬의 2.1배였다.
    /// 씬은 보조광이 없고 SSAO가 환경광을 깎아서, 보조광은 0이고 환경광이 주광을 등진 면의 밝기를 정한다.
    /// </summary>
    public static SyntheticLighting Default { get; } = new SyntheticLighting(new Color(0.30f, 0.30f, 0.315f), 1.75f, 0f);

    /// <summary>모든 면에 고르게 더해지는 환경광 색. 빛을 등진 면의 밝기를 정한다.</summary>
    public Color Ambient { get; }

    /// <summary>주광(씬의 Directional Light에 해당) 세기.</summary>
    public float KeyIntensity { get; }

    /// <summary>주광 반대편에서 오는 보조광 세기. 0이면 주광을 등진 면은 환경광만 받는다.</summary>
    public float FillIntensity { get; }
}
