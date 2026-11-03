using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 패널의 바깥 틀. 둥근 바탕과 테두리, 머리줄(강조 막대, 제목, 영문 라벨, 접기 단추)을 그리고,
    /// 머리줄 오른쪽에 패널이 따로 그릴 수 있는 빈 자리와 내용이 들어갈 자리를 계산해 준다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardCard
    {
        private const float ToggleSize = 18f;
        private const float ToggleRadius = 6f;
        private const float BorderPx = 1f;
        private const string ExpandedMark = "−";
        private const string CollapsedMark = "+";

        /// <summary>패널 바탕과 테두리.</summary>
        public static void DrawFrame(Rect card)
        {
            DashboardPalette palette = DashboardPalette.Default;
            DashboardDraw.FillRounded(card, palette.Card, DashboardMetrics.CardRadius);
            DashboardDraw.StrokeRounded(card, palette.CardBorder, BorderPx, DashboardMetrics.CardRadius);
        }

        /// <summary>머리줄을 그린다. 제목 쪽을 누르면 접기 단추가 눌린 것으로 알려 준다.</summary>
        public static CardHeader DrawHeader(Rect card, string title, string subtitle, bool isCollapsed)
        {
            DashboardPalette palette = DashboardPalette.Default;
            float centerY = card.y + DashboardMetrics.HeaderHeight * 0.5f;
            float x = card.x + DashboardMetrics.CardPadding;

            var bar = new Rect(x, centerY - DashboardMetrics.AccentBarHeight * 0.5f, DashboardMetrics.AccentBarWidth, DashboardMetrics.AccentBarHeight);
            DashboardDraw.FillRounded(bar, palette.Accent, DashboardMetrics.AccentBarWidth * 0.5f);
            x += DashboardMetrics.AccentBarWidth + DashboardMetrics.BlockGap;

            GUIStyle titleStyle = DashboardStyles.Text(TextRole.Title, palette.Ink);
            float titleWidth = DashboardStyles.WidthOf(title, titleStyle) + 2f;
            GUI.Label(new Rect(x, card.y, titleWidth, DashboardMetrics.HeaderHeight), title, titleStyle);
            x += titleWidth + DashboardMetrics.BlockGap;

            if (!string.IsNullOrEmpty(subtitle))
            {
                GUIStyle subtitleStyle = DashboardStyles.Text(TextRole.Subtitle, palette.Faint);
                float subtitleWidth = DashboardStyles.WidthOf(subtitle, subtitleStyle) + 2f;
                GUI.Label(new Rect(x, card.y, subtitleWidth, DashboardMetrics.HeaderHeight), subtitle, subtitleStyle);
                x += subtitleWidth + DashboardMetrics.BlockGap;
            }

            var toggle = new Rect(x, centerY - ToggleSize * 0.5f, ToggleSize, ToggleSize);
            var hit = new Rect(card.x, card.y, toggle.xMax - card.x, DashboardMetrics.HeaderHeight);
            bool isClicked = GuiClick.Detect(hit, out bool isHover, out _);

            DashboardDraw.FillRounded(toggle, isHover ? palette.SurfaceBorder : palette.Surface, ToggleRadius);
            GUI.Label(toggle, isCollapsed ? CollapsedMark : ExpandedMark, DashboardStyles.Text(TextRole.MonoBold, palette.Muted, TextAnchor.MiddleCenter));

            float rightStart = toggle.xMax + DashboardMetrics.BlockGap;
            float rightEnd = card.xMax - DashboardMetrics.CardPadding;
            var rightArea = new Rect(rightStart, card.y, Mathf.Max(0f, rightEnd - rightStart), DashboardMetrics.HeaderHeight);
            return new CardHeader(isClicked, rightArea);
        }

        /// <summary>머리줄 아래, 패널 안쪽 여백을 뺀 내용 자리.</summary>
        public static Rect ContentRect(Rect card)
        {
            float pad = DashboardMetrics.CardPadding;
            return new Rect(
                card.x + pad,
                card.y + DashboardMetrics.HeaderHeight,
                Mathf.Max(0f, card.width - pad * 2f),
                Mathf.Max(0f, card.height - DashboardMetrics.HeaderHeight - pad));
        }
    }

    /// <summary>DashboardCard.DrawHeader의 결과.</summary>
    public readonly struct CardHeader
    {
        public CardHeader(bool isToggleClicked, Rect rightArea)
        {
            IsToggleClicked = isToggleClicked;
            RightArea = rightArea;
        }

        /// <summary>제목 쪽이 눌려 접거나 펼쳐야 하는지.</summary>
        public bool IsToggleClicked { get; }

        /// <summary>머리줄 오른쪽에서 패널이 쓸 수 있는 자리.</summary>
        public Rect RightArea { get; }
    }
}
