using MediKitTwin.Dashboard;
using MediKitTwin.Orders;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>주문 한 건을 한 줄로 그린다: 순번, 병원 이름, 키트 종류 배지, 수량.</summary>
    public static class OrderRowView
    {
        public const float Height = 34f;
        public const float Spacing = 4f;

        private const float SidePadding = 12f;
        private const float SequenceWidth = 28f;
        private const float QuantityWidth = 36f;
        private const float BadgeGap = 8f;
        private const string TimesMark = "×";

        public static void Draw(OrderRecord order)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect row = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            DashboardDraw.FillRounded(row, palette.Surface, DashboardMetrics.RowRadius);

            var quantityRect = new Rect(row.xMax - SidePadding - QuantityWidth, row.y, QuantityWidth, row.height);
            var badgeArea = new Rect(quantityRect.x - BadgeGap - DashboardMetrics.BadgeWidth, row.y, DashboardMetrics.BadgeWidth, row.height);
            float nameX = row.x + SidePadding + SequenceWidth;

            GUI.Label(new Rect(row.x + SidePadding, row.y, SequenceWidth, row.height), order.Sequence.ToString(), DashboardStyles.Text(TextRole.Mono, palette.Faint));
            GUI.Label(new Rect(nameX, row.y, Mathf.Max(0f, badgeArea.x - nameX - BadgeGap), row.height), order.HospitalName, DashboardStyles.Text(TextRole.BodyBold, palette.Ink));
            KitBadge.DrawIn(badgeArea, order.KitCode);
            GUI.Label(quantityRect, TimesMark + order.Quantity, DashboardStyles.Text(TextRole.Mono, palette.InkSoft, TextAnchor.MiddleRight));

            GUILayout.Space(Spacing);
        }
    }
}
