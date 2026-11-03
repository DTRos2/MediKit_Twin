using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>테두리만 있는 작은 태그(로그 줄의 READY, CAMERA …). 정해 준 자리의 세로 가운데에 그린다.</summary>
    public static class TagChip
    {
        private const float Height = 22f;
        private const float Radius = 6f;
        private const float BorderPx = 1.5f;

        public static void DrawIn(Rect area, string text, TagTone tone)
        {
            DashboardPalette palette = DashboardPalette.Default;
            TagColors.Resolve(tone, palette, out Color border, out Color foreground);

            var chip = new Rect(area.x, area.center.y - Height * 0.5f, area.width, Height);
            DashboardDraw.FillRounded(chip, palette.Field, Radius);
            DashboardDraw.StrokeRounded(chip, border, BorderPx, Radius);
            GUI.Label(chip, text, DashboardStyles.Text(TextRole.MonoBold, foreground, TextAnchor.MiddleCenter));
        }
    }
}
