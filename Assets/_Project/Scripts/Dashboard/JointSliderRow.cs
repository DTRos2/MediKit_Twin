using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 값 조절 한 줄: 이름, − 버튼, 슬라이더, + 버튼, 값 글자. 모양이 같은 줄을 한 곳에서 그린다.
    /// 값의 단위와 범위, 자르는 규칙은 호출하는 쪽이 맡고, 여기서는 사용자가 바꾼 새 값(자르기 전)만 알려 준다.
    /// 슬라이더는 직접 그린다: 가는 막대와 둥근 손잡이를 쓰고, 막대 어디를 눌러도 그 값으로 가서 끌 수 있다. OnGUI 안에서만 부른다.
    /// </summary>
    public static class JointSliderRow
    {
        private const float NameWidth = 44f;
        private const float ValueWidth = 84f;
        private const float Gap = 6f;
        private const string MinusMark = "−";
        private const string PlusMark = "+";

        /// <summary>한 줄을 그린다. 사용자가 값을 바꿨으면 true이고 edited에 새 값이 담긴다.</summary>
        /// <param name="shown">슬라이더가 보여 줄 값.</param>
        /// <param name="nudge">−/+ 버튼을 한 번 누를 때 바뀌는 크기.</param>
        /// <param name="valueText">오른쪽에 적을 글자.</param>
        /// <param name="acceptsSlider">슬라이더 움직임을 값 변경으로 받을지. 끄면 값을 보여 주기만 한다.</param>
        public static bool Draw(
            string label, float shown, float lower, float upper, float nudge, string valueText, bool acceptsSlider, out float edited)
        {
            DashboardPalette palette = DashboardPalette.Default;
            edited = shown;
            bool isEdited = false;

            float height = DashboardMetrics.SliderHeight;
            Rect row = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            float buttonSize = DashboardMetrics.IconButtonSize;
            float buttonY = row.y + (height - buttonSize) * 0.5f;

            var nameRect = new Rect(row.x, row.y, NameWidth, height);
            var minusRect = new Rect(row.x + NameWidth, buttonY, buttonSize, buttonSize);
            var valueRect = new Rect(row.xMax - ValueWidth, row.y, ValueWidth, height);
            var plusRect = new Rect(valueRect.x - Gap - buttonSize, buttonY, buttonSize, buttonSize);
            float sliderX = minusRect.xMax + Gap;
            var sliderRect = new Rect(sliderX, row.y, Mathf.Max(0f, plusRect.x - Gap - sliderX), height);

            GUI.Label(nameRect, label, DashboardStyles.Text(TextRole.MonoBold, DashboardDraw.Faded(palette.Accent)));

            if (DashboardButton.DrawIn(minusRect, MinusMark, ButtonKind.Secondary, TextRole.MonoBold))
            {
                edited = shown - nudge;
                isEdited = true;
            }

            float slid = DrawSlider(sliderRect, shown, lower, upper, acceptsSlider, out bool isSlid);
            if (isSlid)
            {
                edited = slid;
                isEdited = true;
            }

            if (DashboardButton.DrawIn(plusRect, PlusMark, ButtonKind.Secondary, TextRole.MonoBold))
            {
                edited = shown + nudge;
                isEdited = true;
            }

            GUI.Label(valueRect, valueText, DashboardStyles.Text(TextRole.Mono, DashboardDraw.Faded(palette.InkSoft), TextAnchor.MiddleRight));
            return isEdited;
        }

        private static float DrawSlider(Rect rect, float value, float min, float max, bool isInteractive, out bool isChanged)
        {
            DashboardPalette palette = DashboardPalette.Default;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            isChanged = false;

            // 손잡이 가운데가 움직이는 구간. 손잡이가 막대 밖으로 삐져나가지 않게 양끝을 손잡이 반지름만큼 뺀다.
            float thumbRadius = DashboardMetrics.SliderThumbSize * 0.5f;
            float travelStart = rect.x + thumbRadius;
            float travelWidth = rect.width - DashboardMetrics.SliderThumbSize;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (isInteractive && GUI.enabled && current.button == 0 && rect.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        value = SliderMapping.ValueAtX(current.mousePosition.x, travelStart, travelWidth, min, max);
                        isChanged = true;
                        current.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        value = SliderMapping.ValueAtX(current.mousePosition.x, travelStart, travelWidth, min, max);
                        isChanged = true;
                        current.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }

                    break;
            }

            var track = new Rect(rect.x, rect.center.y - DashboardMetrics.SliderTrackHeight * 0.5f, rect.width, DashboardMetrics.SliderTrackHeight);
            DashboardDraw.FillRounded(track, palette.Track, track.height * 0.5f);

            float thumbX = SliderMapping.XOfValue(value, travelStart, travelWidth, min, max);
            var thumbCenter = new Vector2(thumbX, rect.center.y);
            DashboardDraw.FillCircle(thumbCenter, thumbRadius, palette.Thumb);
            DashboardDraw.Ring(thumbCenter, thumbRadius, DashboardMetrics.SliderThumbBorder, palette.AccentThumb);
            return value;
        }
    }
}
