using MediKitTwin.ArmA;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>키트 구성품 한 줄을 그린다: 슬롯 번호, 구성품 ID, 선반 칸, 진행 상태 칩.</summary>
    public static class KitComponentRow
    {
        private const float SlotWidth = 52f;
        private const float ShelfSlotWidth = 40f;

        public static void Draw(KittingItem item, KittingItemState state)
        {
            DashboardPalette palette = DashboardPalette.Default;
            float height = DashboardMetrics.ChipHeight;

            using (DashboardRow.Begin())
            {
                DrawFixed($"슬롯 {item.SlotNumber}", SlotWidth, TextRole.Mono, palette.Muted, height);
                DashboardText.Line(item.ComponentId, TextRole.BodyBold, palette.Ink, TextAnchor.MiddleLeft, height);
                DrawFixed(item.ShelfSlotId, ShelfSlotWidth, TextRole.Mono, palette.Faint, height);
                Chip.Draw(KittingStatusText.ItemText(state), KittingChipTones.Item(state));
            }

            DashboardSpacing.Row();
        }

        private static void DrawFixed(string text, float width, TextRole role, Color color, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            GUI.Label(rect, text, DashboardStyles.Text(role, DashboardDraw.Faded(color)));
        }
    }
}
