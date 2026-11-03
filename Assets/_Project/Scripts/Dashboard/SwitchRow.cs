using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 제목과 설명이 왼쪽에, 켜고 끄는 스위치가 오른쪽에 있는 한 줄. 줄 어디를 눌러도 바뀐다.
    /// 값은 호출하는 쪽이 갖고, 이 위젯은 눌렸을 때의 새 값만 돌려준다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class SwitchRow
    {
        private const float SidePadding = 14f;
        private const float TitleTop = 8f;
        private const float TitleHeight = 20f;
        private const float DescriptionTop = 28f;
        private const float DescriptionHeight = 18f;

        /// <returns>이번 이벤트가 끝났을 때의 값(눌렸으면 뒤집힌 값).</returns>
        public static bool Draw(bool value, string title, string description = null)
        {
            DashboardPalette palette = DashboardPalette.Default;
            bool hasDescription = !string.IsNullOrEmpty(description);
            float height = hasDescription ? DashboardMetrics.SwitchRowHeight : DashboardMetrics.SwitchRowTitleOnlyHeight;

            Rect row = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            bool isClicked = GuiClick.Detect(row, out bool isHover, out _);

            DashboardDraw.FillRounded(row, InteractionShade.Apply(palette.Surface, palette.Highlight, isHover, isPressed: false), DashboardMetrics.RowRadius);

            float textWidth = row.width - SidePadding * 2f - DashboardMetrics.SwitchWidth - DashboardMetrics.BlockGap;
            GUIStyle titleStyle = DashboardStyles.Text(TextRole.BodyBold, DashboardDraw.Faded(palette.Ink));
            if (hasDescription)
            {
                GUI.Label(new Rect(row.x + SidePadding, row.y + TitleTop, textWidth, TitleHeight), title, titleStyle);
                GUI.Label(
                    new Rect(row.x + SidePadding, row.y + DescriptionTop, textWidth, DescriptionHeight),
                    description,
                    DashboardStyles.Text(TextRole.Small, DashboardDraw.Faded(palette.Muted)));
            }
            else
            {
                GUI.Label(new Rect(row.x + SidePadding, row.y, textWidth, row.height), title, titleStyle);
            }

            DrawSwitch(new Rect(
                row.xMax - SidePadding - DashboardMetrics.SwitchWidth,
                row.center.y - DashboardMetrics.SwitchHeight * 0.5f,
                DashboardMetrics.SwitchWidth,
                DashboardMetrics.SwitchHeight), value, palette);

            return isClicked ? !value : value;
        }

        private static void DrawSwitch(Rect track, bool value, DashboardPalette palette)
        {
            DashboardDraw.FillRounded(track, value ? palette.Accent : palette.Track, track.height * 0.5f);

            float diameter = track.height - DashboardMetrics.SwitchThumbInset * 2f;
            float thumbX = value
                ? track.xMax - DashboardMetrics.SwitchThumbInset - diameter
                : track.x + DashboardMetrics.SwitchThumbInset;
            DashboardDraw.FillCircle(new Vector2(thumbX + diameter * 0.5f, track.center.y), diameter * 0.5f, palette.Thumb);
        }
    }
}
