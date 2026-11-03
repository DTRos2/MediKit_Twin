namespace MediKitTwin.ML
{
    /// <summary>
    /// 합성 이미지의 랜덤 범위. 모든 값은 0을 중심으로 한 ± 반폭(또는 0 이상 편차)이며 0이면 그 요소를 흔들지 않는다.
    /// 에셋(SyntheticRandomizationSO)과 분리해 두어 범위를 코드에서 바로 만들어 시험할 수 있다.
    /// </summary>
    public readonly struct RandomizationRanges
    {
        public RandomizationRanges(
            float itemOffsetMeters,
            float itemYawDegrees,
            float boxOffsetMeters,
            float boxYawDegrees,
            float lightIntensityDeviation,
            float lightYawDegrees,
            float lightPitchDegrees,
            float hueShift,
            float saturationDeviation,
            float valueDeviation,
            float lightWarmthDeviation = 0f,
            float itemLyingChance = 0f,
            float itemLyingYawDegrees = 0f)
        {
            ItemOffsetMeters = itemOffsetMeters;
            ItemYawDegrees = itemYawDegrees;
            BoxOffsetMeters = boxOffsetMeters;
            BoxYawDegrees = boxYawDegrees;
            LightIntensityDeviation = lightIntensityDeviation;
            LightYawDegrees = lightYawDegrees;
            LightPitchDegrees = lightPitchDegrees;
            HueShift = hueShift;
            SaturationDeviation = saturationDeviation;
            ValueDeviation = valueDeviation;
            LightWarmthDeviation = lightWarmthDeviation;
            ItemLyingChance = itemLyingChance;
            ItemLyingYawDegrees = itemLyingYawDegrees;
        }

        /// <summary>구성품이 슬롯 중심에서 벗어나는 최대 거리(m, X와 Z 각각).</summary>
        public float ItemOffsetMeters { get; }

        /// <summary>구성품이 제자리에서 도는 최대 각도(도).</summary>
        public float ItemYawDegrees { get; }

        /// <summary>박스가 기준 위치에서 벗어나는 최대 거리(m, X와 Z 각각).</summary>
        public float BoxOffsetMeters { get; }

        /// <summary>박스가 도는 최대 각도(도).</summary>
        public float BoxYawDegrees { get; }

        /// <summary>조명 세기 배율의 기준 1에서 벗어나는 최대 편차. 0.3이면 0.7~1.3배이다.</summary>
        public float LightIntensityDeviation { get; }

        /// <summary>조명 방향(수평)이 기준에서 도는 최대 각도(도).</summary>
        public float LightYawDegrees { get; }

        /// <summary>조명 방향(수직)이 기준에서 기우는 최대 각도(도).</summary>
        public float LightPitchDegrees { get; }

        /// <summary>색조(Hue) 이동의 최대 폭(0~1 단위).</summary>
        public float HueShift { get; }

        /// <summary>채도 배율의 기준 1에서 벗어나는 최대 편차.</summary>
        public float SaturationDeviation { get; }

        /// <summary>명도 배율의 기준 1에서 벗어나는 최대 편차.</summary>
        public float ValueDeviation { get; }

        /// <summary>
        /// 조명 색의 따뜻함 편차. 0이 씬의 조명 색, -1이 흰색, +1이 씬보다 두 배 따뜻한 색이다.
        /// 실제 씬은 따뜻한 조명이라 흰 조명으로만 학습하면 색이 달라져 오분류하기 때문이다.
        /// </summary>
        public float LightWarmthDeviation { get; }

        /// <summary>
        /// 원기둥 구성품이 옆으로 누워 있을 확률(0~1). 키팅에서 물건이 칸 안에 놓일 때 쓰러져 눕는 일이 실제로 있어서,
        /// 서 있는 모습으로만 학습하면 누운 물건을 다른 구성품(소독제→채혈관, 붕대→면봉 등)으로 읽는다. 0이면 모두 서 있다.
        /// </summary>
        public float ItemLyingChance { get; }

        /// <summary>누운 구성품이 제자리에서 도는 최대 각도(도). 눕는 방향은 정해져 있지 않아 서 있을 때보다 넓다.</summary>
        public float ItemLyingYawDegrees { get; }
    }
}
