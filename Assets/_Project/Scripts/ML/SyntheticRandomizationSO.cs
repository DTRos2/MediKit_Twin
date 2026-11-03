using UnityEngine;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 합성 이미지의 랜덤 범위 설정. 범위를 코드에 쓰지 않고 이 에셋에서 고친다.
    /// 검사 카메라는 고정 시점과 고정 조명이 전제이므로, 공정에서 실제로 생길 수 있는 오차(구성품이 조금 밀리거나 돌아간 것,
    /// 박스가 지그에서 약간 어긋난 것)와 조명 차이를 학습이 견디게 하는 정도로만 잡는다. (FR-08)
    /// </summary>
    [CreateAssetMenu(menuName = "MediKitTwin/Synthetic Randomization", fileName = "SyntheticRandomization")]
    public sealed class SyntheticRandomizationSO : ScriptableObject
    {
        [Header("구성품")]
        [Tooltip("슬롯 중심에서 벗어나는 최대 거리(m, X와 Z 각각).")]
        [SerializeField, Min(0f)] private float _itemOffsetMeters = 0.006f;
        [Tooltip("제자리에서 도는 최대 각도(도).")]
        [SerializeField, Range(0f, 180f)] private float _itemYawDegrees = 20f;

        [Header("박스")]
        [Tooltip("기준 위치에서 벗어나는 최대 거리(m, X와 Z 각각).")]
        [SerializeField, Min(0f)] private float _boxOffsetMeters = 0.01f;
        [Tooltip("도는 최대 각도(도).")]
        [SerializeField, Range(0f, 45f)] private float _boxYawDegrees = 5f;

        [Header("조명")]
        [Tooltip("세기 배율의 기준 1에서 벗어나는 최대 편차. 0.3이면 0.7~1.3배.")]
        [SerializeField, Range(0f, 0.9f)] private float _lightIntensityDeviation = 0.3f;
        [SerializeField, Range(0f, 90f)] private float _lightYawDegrees = 25f;
        [SerializeField, Range(0f, 45f)] private float _lightPitchDegrees = 10f;

        [Tooltip("조명 색의 따뜻함 편차. 0이 씬의 조명 색, -1이 흰색, +1이 씬보다 두 배 따뜻한 색. 1이면 흰색부터 아주 따뜻한 색까지 흔든다.")]
        [SerializeField, Range(0f, 1.5f)] private float _lightWarmthDeviation = 1f;

        [Header("색")]
        [Tooltip("색조(Hue) 이동의 최대 폭(0~1 단위).")]
        [SerializeField, Range(0f, 0.5f)] private float _hueShift = 0.03f;
        [SerializeField, Range(0f, 0.9f)] private float _saturationDeviation = 0.15f;
        [SerializeField, Range(0f, 0.9f)] private float _valueDeviation = 0.15f;

        public RandomizationRanges ToRanges()
        {
            return new RandomizationRanges(
                _itemOffsetMeters,
                _itemYawDegrees,
                _boxOffsetMeters,
                _boxYawDegrees,
                _lightIntensityDeviation,
                _lightYawDegrees,
                _lightPitchDegrees,
                _hueShift,
                _saturationDeviation,
                _valueDeviation,
                _lightWarmthDeviation);
        }
    }
}
