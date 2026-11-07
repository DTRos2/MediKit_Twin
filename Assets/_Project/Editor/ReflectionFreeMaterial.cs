using UnityEngine;

// 환경 반사를 끈 재질 복사본을 만든다. 직접광의 하이라이트는 그대로 둔다. 원본 에셋은 건드리지 않는다.
// 씬 렌더는 화면 공간 환경광 차폐(SSAO, 반경 0.3m)가 박스 안쪽의 환경광과 반사를 크게 깎아서 빛을 등진 칸막이·벽 옆면이 거의 까맣다.
// 합성 미리보기 렌더는 SSAO를 건너뛰고 하늘 반사가 남아서 같은 면이 두 배쯤 밝았다. 그 면이 학습에서 흐려지면 빈 슬롯 가장자리의
// 짙은 띠를 누운 구성품으로 읽는다. 복사본은 호출한 쪽이 지운다.
public static class ReflectionFreeMaterial
{
    private const string EnvironmentReflectionsProperty = "_EnvironmentReflections";
    private const string EnvironmentReflectionsOffKeyword = "_ENVIRONMENTREFLECTIONS_OFF";

    public static Material Create(Material source)
    {
        var clone = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
        if (clone.HasProperty(EnvironmentReflectionsProperty))
        {
            clone.SetFloat(EnvironmentReflectionsProperty, 0f);
        }

        clone.EnableKeyword(EnvironmentReflectionsOffKeyword);
        return clone;
    }
}
