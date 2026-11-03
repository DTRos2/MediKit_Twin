using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 글자를 넣는 상자. 흰 바탕과 옅은 테두리를 직접 그리고, 안쪽의 글 입력은 IMGUI 글 상자(바탕 없음)에 맡긴다.
    /// 여러 줄 입력(CSV)과 한 줄 입력(칸 ID)을 같은 모양으로 그린다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class DashboardField
    {
        private const float BorderPx = 1f;

        /// <summary>여러 줄 글 상자. 줄이 넘치면 줄바꿈한다.</summary>
        public static string TextArea(string text, float height)
        {
            Rect inner = DrawFrame(0f, height);
            return GUI.TextArea(inner, text ?? string.Empty, EditStyle(true));
        }

        /// <summary>한 줄 글 상자. 글자 수를 maxLength까지로 제한한다.</summary>
        public static string TextField(string text, int maxLength, float width)
        {
            Rect inner = DrawFrame(width, DashboardMetrics.ControlHeight);
            return GUI.TextField(inner, text ?? string.Empty, maxLength, EditStyle(false));
        }

        // 바탕과 테두리를 그리고 글이 들어갈 안쪽 자리를 돌려준다.
        private static Rect DrawFrame(float width, float height)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect outer = width > 0f
                ? GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height))
                : GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));

            DashboardDraw.FillRounded(outer, palette.Field, DashboardMetrics.RowRadius);
            DashboardDraw.StrokeRounded(outer, palette.SurfaceBorder, BorderPx, DashboardMetrics.RowRadius);

            return new Rect(
                outer.x + DashboardMetrics.FieldPaddingX,
                outer.y + DashboardMetrics.FieldPaddingY,
                Mathf.Max(0f, outer.width - DashboardMetrics.FieldPaddingX * 2f),
                Mathf.Max(0f, outer.height - DashboardMetrics.FieldPaddingY * 2f));
        }

        // 바탕이 없는 입력용 글자 모양. 한 줄 상자는 세로 가운데에 맞춘다.
        private static GUIStyle EditStyle(bool isMultiline)
        {
            return DashboardStyles.Text(
                TextRole.Mono,
                DashboardPalette.Default.Ink,
                isMultiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft,
                isMultiline);
        }
    }
}
