using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>작은 이름표와 큰 숫자가 든 칸(완료, 재고 부족, 오류 횟수). 가로는 남는 폭을 나눠 갖는다.</summary>
    public static class StatTile
    {
        private const float Height = 76f;
        private const float SidePadding = 14f;
        private const float LabelTop = 10f;
        private const float LabelHeight = 18f;
        private const float ValueTop = 28f;
        private const float ValueHeight = 40f;

        public static void Draw(string label, string value, Color valueColor)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect rect = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            DashboardDraw.FillRounded(rect, palette.Surface, DashboardMetrics.RowRadius + 2f);

            float width = rect.width - SidePadding * 2f;
            GUI.Label(new Rect(rect.x + SidePadding, rect.y + LabelTop, width, LabelHeight), label, DashboardStyles.Text(TextRole.Small, palette.Muted));
            GUI.Label(new Rect(rect.x + SidePadding, rect.y + ValueTop, width, ValueHeight), value, DashboardStyles.Text(TextRole.BigNumber, valueColor));
        }
    }
}
