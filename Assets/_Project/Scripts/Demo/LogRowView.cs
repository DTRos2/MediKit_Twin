using System.Globalization;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로그 한 줄을 그린다: 시간, 분류 태그, 글. 글이 길면 줄바꿈하고 줄 높이가 따라 커진다.
    /// 경고와 오류 줄은 바탕색을 살짝 달리해서 훑어볼 때 눈에 띄게 한다. 패널 폭을 알고 있으므로 높이를 직접 계산해 모든 이벤트에서 같게 만든다.
    /// </summary>
    public static class LogRowView
    {
        private const float TimeWidth = 52f;
        private const float SidePadding = 12f;
        private const float Gap = 10f;
        private const float MinHeight = 30f;
        private const float TextPaddingY = 6f;
        private const float RowRadius = 8f;
        private const float RowSpacing = 4f;

        /// <summary>줄 하나가 차지하는 높이(간격 제외). 글이 줄바꿈되는 만큼 커진다.</summary>
        public static float HeightOf(string text, float rowWidth)
        {
            GUIStyle style = DashboardStyles.Text(TextRole.Body, DashboardPalette.Default.Ink, TextAnchor.UpperLeft, wrap: true);
            float textHeight = style.CalcHeight(new GUIContent(text), TextWidth(rowWidth));
            return Mathf.Max(MinHeight, textHeight + TextPaddingY * 2f);
        }

        public static void Draw(LogLine line, LogTag tag, float rowWidth, float rowHeight)
        {
            DashboardPalette palette = DashboardPalette.Default;
            Rect row = GUILayoutUtility.GetRect(rowWidth, rowHeight, GUILayout.Width(rowWidth), GUILayout.Height(rowHeight));
            DashboardDraw.FillRounded(row, BackgroundOf(line.Severity, palette), RowRadius);

            float x = row.x + SidePadding;
            string time = line.Seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
            GUI.Label(new Rect(x, row.y, TimeWidth, MinHeight), time, DashboardStyles.Text(TextRole.Mono, palette.Muted));
            x += TimeWidth + Gap;

            TagChip.DrawIn(new Rect(x, row.y, DashboardMetrics.TagWidth, MinHeight), tag.Name, tag.Tone);
            x += DashboardMetrics.TagWidth + Gap;

            GUIStyle textStyle = DashboardStyles.Text(TextRole.Body, palette.Ink, TextAnchor.UpperLeft, wrap: true);
            GUI.Label(new Rect(x, row.y + TextPaddingY, TextWidth(rowWidth), row.height - TextPaddingY), line.Text, textStyle);

            GUILayout.Space(RowSpacing);
        }

        private static float TextWidth(float rowWidth)
        {
            return Mathf.Max(0f, rowWidth - SidePadding * 2f - TimeWidth - DashboardMetrics.TagWidth - Gap * 2f);
        }

        private static Color BackgroundOf(LogSeverity severity, DashboardPalette palette)
        {
            switch (severity)
            {
                case LogSeverity.Error: return palette.DangerSoft;
                case LogSeverity.Warning: return palette.WarningSoft;
                default: return palette.Field;
            }
        }
    }
}
