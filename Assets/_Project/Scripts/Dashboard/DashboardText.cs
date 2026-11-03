using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 패널 안에 글을 줄 단위로 쌓는 도구. 패널마다 GUIStyle을 새로 만들지 않고 DashboardStyles의 모양을 쓰며,
    /// 줄 높이를 글꼴에서 구하므로 글 크기가 달라도 줄 사이 간격이 같다. OnGUI 안에서만 부른다.
    ///  - Line: 한 줄. 넘치는 글은 잘린다.
    ///  - Paragraph: 칸 폭에 맞춰 줄바꿈한다.
    ///  - Inline: 가로로 늘어놓는 줄 안에서 글자 길이만큼만 차지한다.
    /// </summary>
    public static class DashboardText
    {
        // 글자 높이에 더하는 위아래 여유. 글자가 줄 가장자리에 붙어 잘려 보이지 않게 한다.
        private const float LinePaddingPx = 4f;
        private const string HeightProbe = "Ag";
        private const float InlineWidthSlackPx = 2f;

        // 줄바꿈 글은 폭이 모자라도 가로 스크롤이 생기지 않도록 최소 폭을 0으로 풀어 둔다.
        private static readonly GUILayoutOption[] WrapOptions = { GUILayout.MinWidth(0f), GUILayout.ExpandWidth(true) };

        /// <param name="height">0이면 글꼴에서 구한 줄 높이를 쓴다. 옆의 칩이나 버튼과 세로 가운데를 맞출 때 정해 준다.</param>
        public static void Line(string text, TextRole role = TextRole.Body, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, float height = 0f)
        {
            GUIStyle style = DashboardStyles.Text(role, ColorOrInk(color), anchor);
            float lineHeight = height > 0f ? height : LineHeight(style);
            Rect rect = GUILayoutUtility.GetRect(0f, lineHeight, GUILayout.ExpandWidth(true), GUILayout.Height(lineHeight));
            GUI.Label(rect, text, style);
        }

        public static void Paragraph(string text, TextRole role = TextRole.Body, Color? color = null)
        {
            GUIStyle style = DashboardStyles.Text(role, ColorOrInk(color), TextAnchor.UpperLeft, wrap: true);
            GUILayout.Label(text, style, WrapOptions);
        }

        /// <param name="height">0이면 글꼴에서 구한 줄 높이를 쓴다.</param>
        public static void Inline(string text, TextRole role = TextRole.Body, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, float height = 0f)
        {
            GUIStyle style = DashboardStyles.Text(role, ColorOrInk(color), anchor);
            float width = DashboardStyles.WidthOf(text, style) + InlineWidthSlackPx;
            float lineHeight = height > 0f ? height : LineHeight(style);
            Rect rect = GUILayoutUtility.GetRect(width, lineHeight, GUILayout.Width(width), GUILayout.Height(lineHeight));
            GUI.Label(rect, text, style);
        }

        // 빈 글자여도 높이가 0이 되지 않도록 높이를 재는 용도의 글자로 잰다.
        private static float LineHeight(GUIStyle style)
        {
            return Mathf.Ceil(style.CalcSize(new GUIContent(HeightProbe)).y) + LinePaddingPx;
        }

        // 부모의 GUI.enabled가 꺼져 있으면 흐리게 보이도록 DashboardDraw.Faded를 거친다.
        private static Color ColorOrInk(Color? color)
        {
            return DashboardDraw.Faded(color ?? DashboardPalette.Default.Ink);
        }
    }
}
