using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 버튼 종류에 맞는 uGUI 버튼의 상태별 색 블록. 바탕색은 IMGUI 버튼과 같은 ButtonColors가 정하고, 마우스를 올리거나 누를 때의 색은
    /// InteractionShade가 정한다. 바탕 그림은 흰색으로 두고 실제 색은 색 블록에 담는다(색 블록은 그림 색에 곱해지기 때문이다).
    /// 버튼을 만들 때(편집기 도구)와 실행 중에 종류를 바꿀 때(UiKindButton)가 같은 색을 쓰도록 한곳에 둔다.
    /// </summary>
    public static class UiButtonColors
    {
        public static ColorBlock For(ButtonKind kind, DashboardPalette palette)
        {
            ButtonColors.Resolve(kind, palette, out Color background, out _, out _, out _);
            return FromBackground(background, palette);
        }

        /// <summary>
        /// 그림 자체의 색(칩의 종류별 색)은 건드리지 않고 올리거나 누를 때만 어둡게 하는 색 블록. 그림 색이 실행 중에 바뀌는 눌러 볼 수 있는
        /// 칩에 쓴다. 평소와 눌릴 수 없을 때는 그림 색 그대로 보인다(흰색을 곱한다).
        /// </summary>
        public static ColorBlock Shaded(float hoverShade, float pressedShade)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(hoverShade, hoverShade, hoverShade, 1f);
            colors.pressedColor = new Color(pressedShade, pressedShade, pressedShade, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            return colors;
        }

        /// <summary>
        /// 바탕색 하나로 만드는 상태별 색 블록. 버튼 종류가 없는 눌러 볼 수 있는 바탕(선반 칸, 스위치 줄)도 버튼과 같은 올림·누름 색을 쓰게 한다.
        /// </summary>
        public static ColorBlock FromBackground(Color background, DashboardPalette palette)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = background;
            colors.highlightedColor = InteractionShade.Apply(background, palette.Highlight, isHover: true, isPressed: false);
            colors.pressedColor = InteractionShade.Apply(background, palette.Highlight, isHover: true, isPressed: true);
            colors.selectedColor = background;
            colors.disabledColor = new Color(background.r, background.g, background.b, background.a * DashboardMetrics.DisabledAlpha);
            colors.colorMultiplier = 1f;

            // IMGUI 버튼처럼 색이 서서히 바뀌지 않고 바로 바뀐다.
            colors.fadeDuration = 0f;
            return colors;
        }
    }
}
