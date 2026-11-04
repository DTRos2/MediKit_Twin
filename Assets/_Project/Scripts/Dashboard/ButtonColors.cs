using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 버튼 종류에 맞는 바탕색, 글자색, 테두리를 고른다. uGUI 버튼을 만드는 도구들이 같은 색 규칙을 쓰도록 한곳에 모았다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ButtonColors
    {
        public static void Resolve(
            ButtonKind kind, DashboardPalette palette, out Color background, out Color foreground, out Color border, out bool hasBorder)
        {
            border = palette.SurfaceBorder;
            hasBorder = true;

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
        }
    }
}
