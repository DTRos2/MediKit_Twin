using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>왼쪽에 이름, 오른쪽에 값이 있는 옅은 바탕의 한 줄(예: "마지막 결과 … 아직 없음"). 글이 칸보다 길면 잘린다.</summary>
    public static class LabeledValueRow
    {
        private const float Height = 38f;
        private const float SidePadding = 14f;
        private const float LabelShare = 0.4f;

        public static void Draw(string label, string value, Color valueColor)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect row = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            DashboardDraw.FillRounded(row, palette.Surface, DashboardMetrics.RowRadius);

            float inner = row.width - SidePadding * 2f;
            float labelWidth = inner * LabelShare;
            GUI.Label(
                new Rect(row.x + SidePadding, row.y, labelWidth, row.height),
                label,
                DashboardStyles.Text(TextRole.Body, DashboardDraw.Faded(palette.Muted)));
            GUI.Label(
                new Rect(row.x + SidePadding + labelWidth, row.y, inner - labelWidth, row.height),
                value,
                DashboardStyles.Text(TextRole.Body, DashboardDraw.Faded(valueColor), TextAnchor.MiddleRight));
        }
    }
}
