using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>패널 하나가 화면에서 차지하는 자리: 붙는 모서리, 여백, 크기, 처음부터 접어 둘지.</summary>
    public readonly struct DemoPanelSlot
    {
        public DemoPanelSlot(DemoPanelAnchor anchor, Vector2 margin, Vector2 size, bool startsCollapsed = false)
        {
            Anchor = anchor;
            Margin = margin;
            Size = size;
            StartsCollapsed = startsCollapsed;
        }

        public DemoPanelAnchor Anchor { get; }

        public Vector2 Margin { get; }

        public Vector2 Size { get; }

        public bool StartsCollapsed { get; }
    }
}
