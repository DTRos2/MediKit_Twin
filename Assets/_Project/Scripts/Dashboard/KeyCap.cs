using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>키보드 키 모양의 작은 칸(R-Shift, Enter). 글자 길이에 맞춰 가로를 정한다.</summary>
    public static class KeyCap
    {
        private const float Height = 22f;
        private const float PaddingX = 8f;
        private const float Radius = 6f;
        private const float BorderPx = 1f;

        public static void Draw(string key)
        {
            DashboardPalette palette = DashboardPalette.Default;
            GUIStyle style = DashboardStyles.Text(TextRole.Mono, palette.InkSoft, TextAnchor.MiddleCenter);
            float width = DashboardStyles.WidthOf(key, style) + PaddingX * 2f;

            Rect rect = GUILayoutUtility.GetRect(width, Height, GUILayout.Width(width), GUILayout.Height(Height));
            DashboardDraw.FillRounded(rect, palette.Surface, Radius);
            DashboardDraw.StrokeRounded(rect, palette.SurfaceBorder, BorderPx, Radius);
            GUI.Label(rect, key, style);
        }
    }
}
