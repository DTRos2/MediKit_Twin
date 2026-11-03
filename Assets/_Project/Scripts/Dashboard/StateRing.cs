using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 설비 상태를 알리는 둥근 표시: 바깥은 실선 고리, 안쪽은 점선 고리, 가운데는 상태 영문(IDLE, RUN, FAULT).
    /// 고리 색으로 상태를 한눈에 알린다.
    /// </summary>
    public static class StateRing
    {
        private const float OuterWidthPx = 3f;
        private const float DashInset = 12f;
        private const float DotSizePx = 2f;
        private const int DotCount = 40;

        public static void Draw(float diameter, string label, Color ringColor)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect rect = GUILayoutUtility.GetRect(diameter, diameter, GUILayout.Width(diameter), GUILayout.Height(diameter));
            float radius = diameter * 0.5f;

            DashboardDraw.FillCircle(rect.center, radius, palette.Surface);
            DashboardDraw.Ring(rect.center, radius, OuterWidthPx, ringColor);
            DashboardDraw.DashedRing(rect.center, radius - DashInset, DotSizePx, DotCount, palette.Faint);
            GUI.Label(rect, label, DashboardStyles.Text(TextRole.MonoBold, ringColor, TextAnchor.MiddleCenter));
        }
    }
}
