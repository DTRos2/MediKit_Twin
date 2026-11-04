using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>글자 쓰임새(TextRole) 하나가 정하는 모양: 글꼴 종류, 크기, 굵기. IMGUI와 uGUI가 같은 값을 쓰도록 화면 방식과 무관하게 둔다.</summary>
    public readonly struct TextSpec
    {
        public TextSpec(DashboardFontRole font, int size, FontStyle style)
        {
            Font = font;
            Size = size;
            Style = style;
        }

        public DashboardFontRole Font { get; }

        public int Size { get; }

        public FontStyle Style { get; }
    }
}
