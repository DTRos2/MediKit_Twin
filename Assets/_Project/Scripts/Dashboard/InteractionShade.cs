using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 마우스를 올리거나 누른 위젯의 바탕색. 어두운 관제 화면에서는 더 어둡게 해도 티가 나지 않으므로 팔레트의 Highlight(밝은색) 쪽으로 살짝 섞는다.
    /// 버튼·선반 칸·스위치 줄이 같은 값을 쓰게 한곳에 모았다. 섞는 정도는 위에 놓인 글자가 계속 읽히는 한계(대비 4.5:1)를 DashboardReadabilityTests가 지킨다.
    /// </summary>
    public static class InteractionShade
    {
        /// <summary>마우스를 올렸을 때 Highlight를 섞는 비율.</summary>
        public const float HoverAmount = 0.05f;

        /// <summary>누르고 있을 때 Highlight를 섞는 비율. 올렸을 때보다 확실히 달라 보이되 흐린 글자(Muted)가 읽히는 한계 안이다.</summary>
        public const float PressedAmount = 0.09f;

        /// <param name="background">아무 것도 안 했을 때의 바탕색.</param>
        /// <param name="highlight">섞어 넣을 밝은색(보통 팔레트의 Highlight).</param>
        /// <param name="isHover">마우스가 위에 있다.</param>
        /// <param name="isPressed">마우스 버튼을 누르고 있다. 위젯 밖으로 벗어났으면 눌림으로 치지 않는다(isHover도 함께 참일 때만).</param>
        public static Color Apply(Color background, Color highlight, bool isHover, bool isPressed)
        {
            if (isPressed && isHover)
            {
                return Color.Lerp(background, highlight, PressedAmount);
            }

            return isHover ? Color.Lerp(background, highlight, HoverAmount) : background;
        }
    }
}
