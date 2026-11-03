using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 글자 길이에 맞는 작은 알약 모양 표시("보류 0건", "경고 없음", 로봇 이름). 가로는 글자에 맞추고 세로는 칩 높이로 고정한다.
    /// DrawClickable은 같은 모양에 누름 판정을 더한 것이다(눌러서 다음 로봇으로 바꾸기 등).
    /// </summary>
    public static class Chip
    {
        private const float DotSize = 6f;
        private const float DotGap = 6f;

        public static void Draw(string text, ChipTone tone = ChipTone.Neutral, bool withDot = false, TextRole role = TextRole.Small)
        {
            DrawAndGetRect(text, tone, withDot, role);
        }

        /// <summary>칩을 그리고 눌렸으면 true. 모든 이벤트에서 같은 횟수로 불러야 한다(GuiClick과 같다).</summary>
        public static bool DrawClickable(string text, ChipTone tone = ChipTone.Neutral, bool withDot = false, TextRole role = TextRole.Small)
        {
            Rect rect = DrawAndGetRect(text, tone, withDot, role);
            return GuiClick.Detect(rect, out _, out _);
        }

        private static Rect DrawAndGetRect(string text, ChipTone tone, bool withDot, TextRole role)
        {
            DashboardPalette palette = DashboardPalette.Default;
            ChipColors.Resolve(tone, palette, out Color background, out Color foreground);

            GUIStyle style = DashboardStyles.Text(role, foreground, TextAnchor.MiddleLeft);
            float dotWidth = withDot ? DotSize + DotGap : 0f;
            float width = DashboardStyles.WidthOf(text, style) + dotWidth + DashboardMetrics.ChipPaddingX * 2f + 2f;

            Rect rect = GUILayoutUtility.GetRect(width, DashboardMetrics.ChipHeight, GUILayout.Width(width), GUILayout.Height(DashboardMetrics.ChipHeight));
            DashboardDraw.FillRounded(rect, background, DashboardMetrics.ChipRadius);

            float textX = rect.x + DashboardMetrics.ChipPaddingX;
            if (withDot)
            {
                DashboardDraw.FillCircle(new Vector2(textX + DotSize * 0.5f, rect.center.y), DotSize * 0.5f, foreground);
                textX += dotWidth;
            }

            GUI.Label(new Rect(textX, rect.y, rect.xMax - textX, rect.height), text, style);
            return rect;
        }
    }
}
