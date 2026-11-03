using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>키트 종류를 색으로 알리는 작은 배지(예: EM). 키트 종류가 정해 주는 색은 KitBadgePalette가 맡는다.</summary>
    public static class KitBadge
    {
        private const float Height = 24f;
        private const float Radius = 8f;

        /// <summary>정해 준 자리의 가운데에 배지를 그린다.</summary>
        public static void DrawIn(Rect area, string kitCode)
        {
            DashboardPalette palette = DashboardPalette.Default;
            var badge = new Rect(
                area.center.x - DashboardMetrics.BadgeWidth * 0.5f,
                area.center.y - Height * 0.5f,
                DashboardMetrics.BadgeWidth,
                Height);
            DashboardDraw.FillRounded(badge, KitBadgePalette.Fill(kitCode, palette), Radius);
            GUI.Label(badge, kitCode, DashboardStyles.Text(TextRole.MonoBold, palette.OnAccent, TextAnchor.MiddleCenter));
        }
    }
}
