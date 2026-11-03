using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 둥근 버튼. 모양은 직접 그리고 누름 판정은 GuiClick이 맡는다. 마우스를 올리면 바탕이 조금 밝아지고 누르면 더 밝아진다(InteractionShade).
    /// 부모의 GUI.enabled가 꺼져 있으면 흐리게 보이고 눌리지 않는다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardButton
    {
        private const float BorderPx = 1f;
        private const float FitPaddingX = 14f;

        /// <summary>레이아웃 안에서 버튼을 그린다. width가 0이면 남는 가로를 나눠 갖는다.</summary>
        public static bool Draw(string text, ButtonKind kind = ButtonKind.Secondary, float width = 0f, float height = DashboardMetrics.ControlHeight)
        {
            Rect rect = width > 0f
                ? GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height))
                : GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            return DrawIn(rect, text, kind);
        }

        /// <summary>글자 길이에 맞는 가로로 버튼을 그린다. 머리줄 오른쪽처럼 남는 폭을 나눠 갖지 않는 자리에 쓴다.</summary>
        public static bool DrawFit(string text, ButtonKind kind = ButtonKind.Secondary, float height = DashboardMetrics.SmallControlHeight)
        {
            GUIStyle style = DashboardStyles.Text(kind == ButtonKind.Primary ? TextRole.BodyBold : TextRole.Body, Color.black);
            float width = DashboardStyles.WidthOf(text, style) + FitPaddingX * 2f;
            return Draw(text, kind, width, height);
        }

        /// <summary>정해 준 자리에 버튼을 그린다.</summary>
        public static bool DrawIn(Rect rect, string text, ButtonKind kind = ButtonKind.Secondary, TextRole? role = null)
        {
            DashboardPalette palette = DashboardPalette.Default;
            bool isClicked = GuiClick.Detect(rect, out bool isHover, out bool isPressed);

            Color background;
            Color foreground;
            bool hasBorder = true;
            Color border = palette.SurfaceBorder;
            switch (kind)
            {
                case ButtonKind.Primary:
                    background = palette.Accent;
                    foreground = palette.OnAccent;
                    hasBorder = false;
                    break;
                case ButtonKind.Selected:
                    background = palette.AccentSoft;
                    foreground = palette.Accent;
                    border = palette.AccentThumb;
                    break;
                default:
                    background = palette.Surface;
                    foreground = palette.Ink;
                    break;
            }

            background = InteractionShade.Apply(background, palette.Highlight, isHover, isPressed);

            DashboardDraw.FillRounded(rect, background, DashboardMetrics.RowRadius);
            if (hasBorder)
            {
                DashboardDraw.StrokeRounded(rect, border, BorderPx, DashboardMetrics.RowRadius);
            }

            TextRole textRole = role ?? (kind == ButtonKind.Primary ? TextRole.BodyBold : TextRole.Body);
            GUI.Label(rect, text, DashboardStyles.Text(textRole, DashboardDraw.Faded(foreground), TextAnchor.MiddleCenter));
            return isClicked;
        }
    }
}
