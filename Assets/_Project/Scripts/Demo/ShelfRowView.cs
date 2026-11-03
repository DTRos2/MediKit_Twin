using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 한 행을 한 줄로 그린다: 왼쪽에 행 글자와 키트 종류, 오른쪽에 칸마다 재고 타일. 행 색은 키트 종류 색(KitBadgePalette)을 쓴다.
    /// 재고는 IStock으로 읽기만 하고, 어느 칸이 눌렸는지만 돌려준다(집기·되돌리기·로봇 지시는 호출하는 쪽이 정한다).
    /// </summary>
    public static class ShelfRowView
    {
        private const float LabelWidth = 40f;
        private const float LetterHeight = 24f;
        private const float KitCodeHeight = 16f;
        private const string QuantityUnit = "개";
        private const string RefillNoteFormat = "보충 {0:0}초";

        /// <returns>눌린 칸의 ID. 눌린 칸이 없으면 null이다.</returns>
        public static string Draw(ShelfRow row, string kitCode, IStock stock)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Color stripColor = KitBadgePalette.Fill(kitCode, palette);
            string pressed = null;

            using (DashboardRow.Begin())
            {
                DrawLabel(row.Letter, kitCode, palette);

                foreach (string slotId in row.SlotIds)
                {
                    float? refill = stock.GetRefillRemainingSeconds(slotId);
                    string note = refill.HasValue ? string.Format(RefillNoteFormat, refill.Value) : string.Empty;
                    if (StockTile.Draw(slotId, stock.GetQuantity(slotId).ToString(), QuantityUnit, stripColor, note))
                    {
                        pressed = slotId;
                    }

                    DashboardSpacing.Row();
                }
            }

            return pressed;
        }

        // 행 글자는 짙은 키트 색으로, 키트 종류는 그 아래에 작게 적는다. 키트 종류를 모르는 행은 글자만 중립색으로 적는다.
        private static void DrawLabel(char letter, string kitCode, DashboardPalette palette)
        {
            Rect area = GUILayoutUtility.GetRect(LabelWidth, StockTile.Height, GUILayout.Width(LabelWidth), GUILayout.Height(StockTile.Height));
            float top = area.center.y - (LetterHeight + KitCodeHeight) * 0.5f;

            GUI.Label(
                new Rect(area.x, top, area.width, LetterHeight),
                letter.ToString(),
                DashboardStyles.Text(TextRole.BodyBold, DashboardDraw.Faded(KitBadgePalette.Ink(kitCode, palette)), TextAnchor.MiddleCenter));
            GUI.Label(
                new Rect(area.x, top + LetterHeight, area.width, KitCodeHeight),
                kitCode,
                DashboardStyles.Text(TextRole.Small, DashboardDraw.Faded(palette.Faint), TextAnchor.MiddleCenter));
        }
    }
}
