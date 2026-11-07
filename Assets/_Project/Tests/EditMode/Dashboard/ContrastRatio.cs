using UnityEngine;

namespace MediKitTwin.Dashboard.Tests
{
    /// <summary>
    /// 두 색의 밝기 대비(WCAG 2 기준, 1~21). 대시보드는 눈으로 확인하기 전에 글자가 바탕 위에서 읽히는지 수치로 지키려고 쓴다.
    /// 일반 글자는 4.5 이상이 읽기 편한 기준이다.
    /// </summary>
    public static class ContrastRatio
    {
        /// <summary>일반 크기 글자가 읽히려면 필요한 최소 대비(WCAG 2 AA).</summary>
        public const float MinimumForText = 4.5f;

        public static float Between(Color a, Color b)
        {
            float la = Luminance(a);
            float lb = Luminance(b);
            float lighter = Mathf.Max(la, lb);
            float darker = Mathf.Min(la, lb);
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        // sRGB 색을 선형 밝기로 바꿔 사람 눈의 민감도(초록 > 빨강 > 파랑)로 섞는다.
        private static float Luminance(Color color)
        {
            return 0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) + 0.0722f * Linear(color.b);
        }

        private static float Linear(float channel)
        {
            return channel <= 0.04045f ? channel / 12.92f : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }
    }
}
