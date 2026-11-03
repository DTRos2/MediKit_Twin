using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>카메라 화면 위에 올리는 표시: 네 모서리 꺾쇠, 가운데 조준 원, 아래쪽 정보 칩.</summary>
    public static class CameraOverlay
    {
        private const float BracketInset = 12f;
        private const float BracketLength = 18f;
        private const float BracketThickness = 2f;
        private const float CrosshairRadius = 14f;
        private const float CrosshairWidth = 1.5f;
        private const float CrosshairDotRadius = 1.5f;
        private const float ChipInset = 10f;
        private const float ChipHeight = 22f;
        private const float ChipPaddingX = 8f;
        private const float ChipRadius = 6f;

        /// <summary>네 모서리에 ㄱ자 꺾쇠를 그린다.</summary>
        public static void DrawBrackets(Rect area)
        {
            Color color = DashboardPalette.Default.CameraFrame;
            float inset = BracketInset;
            float length = BracketLength;
            float t = BracketThickness;

            DrawCorner(area.x + inset, area.y + inset, length, t, 1f, 1f, color);
            DrawCorner(area.xMax - inset, area.y + inset, length, t, -1f, 1f, color);
            DrawCorner(area.x + inset, area.yMax - inset, length, t, 1f, -1f, color);
            DrawCorner(area.xMax - inset, area.yMax - inset, length, t, -1f, -1f, color);
        }

        /// <summary>화면 가운데에 조준 원과 점을 그린다.</summary>
        public static void DrawCrosshair(Rect area)
        {
            Color color = DashboardPalette.Default.CameraFrame;
            DashboardDraw.Ring(area.center, CrosshairRadius, CrosshairWidth, color);
            DashboardDraw.FillCircle(area.center, CrosshairDotRadius, color);
        }

        /// <summary>화면 아래쪽 모서리에 붙는 정보 칩. 왼쪽이나 오른쪽에 붙인다.</summary>
        public static void DrawChip(Rect area, string text, bool isRightSide)
        {
            DashboardPalette palette = DashboardPalette.Default;
            GUIStyle style = DashboardStyles.Text(TextRole.Mono, palette.Ink, TextAnchor.MiddleCenter);
            float width = DashboardStyles.WidthOf(text, style) + ChipPaddingX * 2f;
            float x = isRightSide ? area.xMax - ChipInset - width : area.x + ChipInset;

            var chip = new Rect(x, area.yMax - ChipInset - ChipHeight, width, ChipHeight);
            DashboardDraw.FillRounded(chip, palette.CameraOverlay, ChipRadius);
            GUI.Label(chip, text, style);
        }

        // 꼭짓점(x, y)에서 가로 방향(dx)과 세로 방향(dy)으로 뻗는 ㄱ자. dx, dy는 1이나 -1이다.
        private static void DrawCorner(float x, float y, float length, float thickness, float dx, float dy, Color color)
        {
            float horizontalX = dx > 0f ? x : x - length;
            float verticalY = dy > 0f ? y : y - length;
            float horizontalBarY = dy > 0f ? y : y - thickness;
            float verticalBarX = dx > 0f ? x : x - thickness;

            DashboardDraw.FillRounded(new Rect(horizontalX, horizontalBarY, length, thickness), color, 0f);
            DashboardDraw.FillRounded(new Rect(verticalBarX, verticalY, thickness, length), color, 0f);
        }
    }
}
