using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 패널 자리(DemoPanelSlot)를 캔버스 위 RectTransform의 앵커, 기준점, 위치, 크기로 바꾼 값.
    /// IMGUI 패널의 자리 표(DemoPanelSlots)를 uGUI 패널도 그대로 쓰게 해서, 자리를 두 곳에 따로 적지 않는다.
    /// 캔버스 좌표는 위로 갈수록 커지므로 위쪽 여백은 음수 y가 된다. Unity 없이 시험할 수 있다.
    /// </summary>
    public readonly struct DemoPanelRectAnchor
    {
        private DemoPanelRectAnchor(Vector2 anchor, Vector2 position, Vector2 size)
        {
            AnchorMin = anchor;
            AnchorMax = anchor;
            Pivot = anchor;
            AnchoredPosition = position;
            Size = size;
        }

        public Vector2 AnchorMin { get; }

        public Vector2 AnchorMax { get; }

        public Vector2 Pivot { get; }

        public Vector2 AnchoredPosition { get; }

        public Vector2 Size { get; }

        public static DemoPanelRectAnchor From(DemoPanelSlot slot)
        {
            Vector2 margin = slot.Margin;
            switch (slot.Anchor)
            {
                case DemoPanelAnchor.TopRight:
                    return new DemoPanelRectAnchor(new Vector2(1f, 1f), new Vector2(-margin.x, -margin.y), slot.Size);
                case DemoPanelAnchor.BottomLeft:
                    return new DemoPanelRectAnchor(new Vector2(0f, 0f), new Vector2(margin.x, margin.y), slot.Size);
                case DemoPanelAnchor.BottomRight:
                    return new DemoPanelRectAnchor(new Vector2(1f, 0f), new Vector2(-margin.x, margin.y), slot.Size);
                case DemoPanelAnchor.BottomCenter:
                    // 아래쪽 가운데는 가로 여백이 가운데에서 오른쪽으로 옮기는 거리다.
                    return new DemoPanelRectAnchor(new Vector2(0.5f, 0f), new Vector2(margin.x, margin.y), slot.Size);
                default:
                    return new DemoPanelRectAnchor(new Vector2(0f, 1f), new Vector2(margin.x, -margin.y), slot.Size);
            }
        }

        /// <summary>RectTransform에 이 자리를 적용한다.</summary>
        public void ApplyTo(RectTransform rect)
        {
            rect.anchorMin = AnchorMin;
            rect.anchorMax = AnchorMax;
            rect.pivot = Pivot;
            rect.anchoredPosition = AnchoredPosition;
            rect.sizeDelta = Size;
        }
    }
}
