using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 글자 쓰임새(TextRole)마다 정해진 글꼴 종류, 크기, 굵기를 한곳에 모은 표.
    /// 글자 크기를 위젯마다 다시 적지 않고 DashboardMetrics의 이름 붙은 값만 쓴다.
    /// 모든 uGUI 글자(UiFontBinding을 붙이는 쪽)가 같은 표를 보므로 패널마다 글자가 어긋나지 않는다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class TextSpecs
    {
        // 영문 작은 라벨은 같은 줄의 한글 제목보다 한 단계 작게 해야 제목이 먼저 읽힌다.
        private const int SubtitleShrink = 1;

        public static TextSpec For(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title:
                    return new TextSpec(DashboardFontRole.Sans, DashboardMetrics.TitleFontSize, FontStyle.Bold);
                case TextRole.Subtitle:
                    return new TextSpec(DashboardFontRole.Mono, DashboardMetrics.SmallFontSize - SubtitleShrink, FontStyle.Normal);
                case TextRole.BodyBold:
                    return new TextSpec(DashboardFontRole.Sans, DashboardMetrics.BodyFontSize, FontStyle.Bold);
                case TextRole.Small:
                    return new TextSpec(DashboardFontRole.Sans, DashboardMetrics.SmallFontSize, FontStyle.Normal);
                case TextRole.Mono:
                    return new TextSpec(DashboardFontRole.Mono, DashboardMetrics.MonoFontSize, FontStyle.Normal);
                case TextRole.MonoBold:
                    return new TextSpec(DashboardFontRole.Mono, DashboardMetrics.MonoFontSize, FontStyle.Bold);
                case TextRole.BigNumber:
                    return new TextSpec(DashboardFontRole.Mono, DashboardMetrics.BigNumberFontSize, FontStyle.Bold);
                case TextRole.State:
                    return new TextSpec(DashboardFontRole.Sans, DashboardMetrics.StateFontSize, FontStyle.Bold);
                default:
                    return new TextSpec(DashboardFontRole.Sans, DashboardMetrics.BodyFontSize, FontStyle.Normal);
            }
        }
    }
}
