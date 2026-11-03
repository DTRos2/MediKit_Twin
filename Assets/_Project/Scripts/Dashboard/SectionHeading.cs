using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>패널 안에서 구간을 나누는 제목: 위에 가는 선을 긋고 그 아래에 굵은 작은 글씨를 쓴다(예: "집는 자세 저장").</summary>
    public static class SectionHeading
    {
        private const float DividerHeight = 1f;

        public static void Draw(string text)
        {
            DashboardPalette palette = DashboardPalette.Default;

            DashboardSpacing.Section();
            Rect divider = GUILayoutUtility.GetRect(0f, DividerHeight, GUILayout.ExpandWidth(true), GUILayout.Height(DividerHeight));
            DashboardDraw.FillRounded(divider, palette.SurfaceBorder, 0f);
            DashboardSpacing.Block();
            DashboardText.Line(text, TextRole.BodyBold, palette.InkSoft);
        }
    }
}
