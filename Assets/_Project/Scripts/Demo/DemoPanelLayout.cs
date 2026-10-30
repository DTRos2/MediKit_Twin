using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 화면 크기와 모서리, 여백, 패널 크기로 패널 자리를 계산한다. 화면 크기가 바뀌어도 오른쪽과 아래 모서리에 붙은 패널이
    /// 그 모서리를 따라가도록 좌표를 매번 새로 구한다. 화면 크기를 인자로 받으므로 Unity 없이 시험할 수 있다.
    /// </summary>
    public static class DemoPanelLayout
    {
        /// <param name="margin">anchor 모서리에서 패널까지의 가로, 세로 거리(px).</param>
        public static Rect Place(DemoPanelAnchor anchor, Vector2 margin, Vector2 size, float screenWidth, float screenHeight)
        {
            bool isRight = anchor == DemoPanelAnchor.TopRight || anchor == DemoPanelAnchor.BottomRight;
            bool isBottom = anchor == DemoPanelAnchor.BottomLeft || anchor == DemoPanelAnchor.BottomRight;

            float x = isRight ? screenWidth - margin.x - size.x : margin.x;
            float y = isBottom ? screenHeight - margin.y - size.y : margin.y;
            return new Rect(x, y, size.x, size.y);
        }
    }
}
