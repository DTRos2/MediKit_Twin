using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 선반 칸 하나를 나타내는 눌러 볼 수 있는 칸. 위쪽에 행 색 띠, 칸 이름, 큰 재고 숫자와 단위, 오른쪽 아래에 짧은 알림(보충 대기 등)이 있다.
    /// 누름 판정은 GuiClick이 맡는다. 비활성이면 흐리게 보이고 눌리지 않는다.
    /// </summary>
    public static class StockTile
    {
        /// <summary>칸 한 개의 높이. 옆에 붙는 행 이름 칸이 같은 높이로 맞출 때 쓴다.</summary>
        public const float Height = 60f;
        private const float StripHeight = 4f;
        private const float StripInset = 6f;
        private const float TextInset = 10f;
        private const float IdTop = 8f;
        private const float IdHeight = 14f;
        private const float QuantityTop = 22f;
        private const float QuantityHeight = 30f;
        private const float UnitGap = 3f;
        private const float TileRadius = 12f;

        /// <summary>칸을 그리고 눌렸으면 true. 가로는 남는 폭을 나눠 갖는다.</summary>
        /// <param name="note">오른쪽 아래의 짧은 글. 없으면 비운다.</param>
        public static bool Draw(string slotId, string quantity, string unit, Color stripColor, string note)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect rect = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            bool isClicked = GuiClick.Detect(rect, out bool isHover, out bool isPressed);

            Color background = InteractionShade.Apply(palette.Surface, palette.Highlight, isHover, isPressed);

            DashboardDraw.FillRounded(rect, background, TileRadius);
            DashboardDraw.FillRounded(new Rect(rect.x + StripInset, rect.y, rect.width - StripInset * 2f, StripHeight), stripColor, StripHeight * 0.5f);

            GUI.Label(
                new Rect(rect.x + TextInset, rect.y + IdTop, rect.width - TextInset, IdHeight),
                slotId,
                DashboardStyles.Text(TextRole.Mono, DashboardDraw.Faded(palette.Muted)));

            GUIStyle quantityStyle = DashboardStyles.Text(TextRole.MonoBold, DashboardDraw.Faded(palette.Ink));
            GUIStyle bigStyle = new GUIStyle(quantityStyle) { fontSize = DashboardMetrics.BigNumberFontSize - 6 };
            float quantityWidth = DashboardStyles.WidthOf(quantity, bigStyle);
            GUI.Label(new Rect(rect.x + TextInset, rect.y + QuantityTop, quantityWidth + 2f, QuantityHeight), quantity, bigStyle);
            GUI.Label(
                new Rect(rect.x + TextInset + quantityWidth + UnitGap, rect.y + QuantityTop + 4f, rect.width, QuantityHeight - 4f),
                unit,
                DashboardStyles.Text(TextRole.Small, DashboardDraw.Faded(palette.Muted), TextAnchor.LowerLeft));

            if (!string.IsNullOrEmpty(note))
            {
                GUI.Label(
                    new Rect(rect.x, rect.y + IdTop, rect.width - TextInset, IdHeight),
                    note,
                    DashboardStyles.Text(TextRole.Small, DashboardDraw.Faded(palette.Warning), TextAnchor.MiddleRight));
            }

            return isClicked;
        }
    }
}
