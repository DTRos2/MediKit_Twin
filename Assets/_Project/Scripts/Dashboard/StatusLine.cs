using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>색 점과 한 줄 글로 된 상태 줄(예: "● 물건 탐색 안 함"). 글이 줄 폭보다 길면 잘린다.</summary>
    public static class StatusLine
    {
        private const float Height = 22f;
        private const float DotSize = 7f;
        private const float DotGap = 9f;

        public static void Draw(string text, Color dotColor, Color textColor)
        {
            Rect row = GUILayoutUtility.GetRect(0f, Height, GUILayout.ExpandWidth(true), GUILayout.Height(Height));
            DashboardDraw.FillCircle(new Vector2(row.x + DotSize * 0.5f + 2f, row.center.y), DotSize * 0.5f, dotColor);

            float textX = row.x + DotSize + DotGap;
            GUI.Label(new Rect(textX, row.y, Mathf.Max(0f, row.xMax - textX), row.height), text, DashboardStyles.Text(TextRole.Small, textColor));
        }
    }
}
