using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 패널의 슬라이더 한 줄: 이름, -, 슬라이더, +, 값 글자. 모양이 같은 줄을 한 곳에서 그린다.
    /// 값의 단위와 범위, 자르는 규칙은 호출하는 쪽(JointTargetEditor)이 맡고 여기서는 새 값을 알려 주기만 한다.
    /// </summary>
    internal static class DemoSliderRow
    {
        private const float NameWidth = 100f;
        private const float NudgeButtonWidth = 28f;
        private const float ValueWidth = 110f;

        /// <summary>한 줄을 그린다. 사용자가 값을 바꿨으면 true이고 edited에 새 값이 담긴다(자르기 전 값).</summary>
        /// <param name="shown">슬라이더가 보여 줄 값.</param>
        /// <param name="nudge">-/+ 버튼을 한 번 누를 때 바뀌는 크기.</param>
        /// <param name="valueText">오른쪽에 적을 글자.</param>
        /// <param name="acceptsSlider">슬라이더 움직임을 값 변경으로 받을지. 끄면 값을 보여 주기만 한다.</param>
        public static bool Draw(
            string label, float shown, float lower, float upper, float nudge, string valueText, bool acceptsSlider, out float edited)
        {
            edited = shown;
            bool isEdited = false;

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(NameWidth));

            if (GUILayout.Button("-", GUILayout.Width(NudgeButtonWidth)))
            {
                edited = shown - nudge;
                isEdited = true;
            }

            float slider = GUILayout.HorizontalSlider(shown, lower, upper);
            if (acceptsSlider && !Mathf.Approximately(slider, shown))
            {
                edited = slider;
                isEdited = true;
            }

            if (GUILayout.Button("+", GUILayout.Width(NudgeButtonWidth)))
            {
                edited = shown + nudge;
                isEdited = true;
            }

            GUILayout.Label(valueText, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();

            return isEdited;
        }
    }
}
