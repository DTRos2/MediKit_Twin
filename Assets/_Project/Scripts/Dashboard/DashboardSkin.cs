using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 대시보드용 IMGUI 스킨. Unity 기본 스킨을 복사해서 글꼴, 글자 색, 커서 색, 스크롤 막대만 디자인에 맞게 바꾼 것이다.
    /// 패널 안에서 기본 GUILayout 컨트롤(라벨, 글 상자, 스크롤 뷰)을 그대로 써도 모양이 맞도록 한다.
    /// 버튼, 스위치, 슬라이더처럼 모양이 중요한 것은 이 스킨이 아니라 전용 위젯이 직접 그린다. OnGUI 안에서 처음 부를 때 한 번 만든다.
    /// </summary>
    public static class DashboardSkin
    {
        private const float SelectionAlpha = 0.35f;

        private static GUISkin _skin;

        public static GUISkin Get()
        {
            if (_skin == null)
            {
                _skin = Build();
            }

            return _skin;
        }

        private static GUISkin Build()
        {
            DashboardPalette palette = DashboardPalette.Default;
            GUISkin skin = Object.Instantiate(GUI.skin);
            skin.hideFlags = HideFlags.HideAndDontSave;
            skin.font = DashboardFonts.Sans;

            ConfigureText(skin.label, palette.Ink);
            ConfigureText(skin.textArea, palette.Ink);
            ConfigureText(skin.textField, palette.Ink);

            skin.settings.cursorColor = palette.Ink;
            Color selection = palette.Accent;
            selection.a = SelectionAlpha;
            skin.settings.selectionColor = selection;

            ConfigureScrollbar(skin, palette);
            return skin;
        }

        private static void ConfigureText(GUIStyle style, Color color)
        {
            style.font = DashboardFonts.Sans;
            style.fontSize = DashboardMetrics.BodyFontSize;
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
        }

        // 위아래 화살표 버튼 없이 가는 막대 하나만 남긴다.
        private static void ConfigureScrollbar(GUISkin skin, DashboardPalette palette)
        {
            Flatten(skin.verticalScrollbar, DashboardMetrics.ScrollbarWidth, 0f);
            skin.verticalScrollbar.margin = new RectOffset(4, 0, 0, 0);

            Flatten(skin.verticalScrollbarThumb, DashboardMetrics.ScrollbarWidth, 0f);
            Texture2D thumb = SolidTexture.Of(palette.Track);
            skin.verticalScrollbarThumb.normal.background = thumb;
            skin.verticalScrollbarThumb.hover.background = thumb;
            skin.verticalScrollbarThumb.active.background = thumb;
            skin.verticalScrollbarThumb.focused.background = thumb;

            Flatten(skin.verticalScrollbarUpButton, 0f, 0f);
            Flatten(skin.verticalScrollbarDownButton, 0f, 0f);
        }

        private static void Flatten(GUIStyle style, float fixedWidth, float fixedHeight)
        {
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.border = new RectOffset(0, 0, 0, 0);
            style.padding = new RectOffset(0, 0, 0, 0);
            style.overflow = new RectOffset(0, 0, 0, 0);
            style.fixedWidth = fixedWidth;
            style.fixedHeight = fixedHeight;
        }
    }
}
