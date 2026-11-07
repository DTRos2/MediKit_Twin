using System;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 뚜껑에 붙는 라벨의 모양. 크기와 색, 글자 크기를 코드에 쓰지 않고 인스펙터에서 정한다. (FR-11)
    /// 기본값은 뚜껑(0.1176 x 0.2587m) 윗면에 들어가는 작은 종이 라벨이다.
    /// </summary>
    [Serializable]
    public sealed class KitLabelLook
    {
        [Tooltip("라벨 종이 크기(m). x는 뚜껑의 짧은 변, y는 뚜껑의 긴 변 방향이다. 뚜껑 윗면(0.1176 x 0.2587m)보다 작아야 한다.")]
        [SerializeField] private Vector2 _size = new Vector2(0.09f, 0.06f);

        [Tooltip("라벨 종이 두께(m).")]
        [SerializeField, Min(0.0001f)] private float _thickness = 0.002f;

        [Tooltip("뚜껑 윗면에서 띄우는 높이(m). 0이면 같은 면에서 깜빡거린다(z-fighting).")]
        [SerializeField, Min(0f)] private float _liftAboveSurface = 0.0003f;

        [Tooltip("종이 색.")]
        [SerializeField] private Color _paperColor = Color.white;

        [Tooltip("글자 색.")]
        [SerializeField] private Color _textColor = new Color(0.05f, 0.05f, 0.05f);

        [Tooltip("글자 크기(TextMesh의 Character Size). 값이 클수록 큰 글자다. 한 줄이 종이 안에 들어가게 맞춘다.")]
        [SerializeField, Min(0.0001f)] private float _characterSize = 0.0035f;

        [Tooltip("글꼴 해상도(TextMesh의 Font Size). 클수록 선명하다.")]
        [SerializeField, Min(1)] private int _fontSize = 64;

        [Tooltip("글꼴 이름을 앞에서부터 찾아 컴퓨터에 깔린 첫 글꼴을 쓴다. 한글이 나오는 글꼴을 앞에 둔다. 하나도 없으면 Unity 기본 글꼴이다.")]
        [SerializeField] private string[] _fontNames = { "Malgun Gothic", "Noto Sans KR", "Apple SD Gothic Neo", "Segoe UI", "Arial" };

        public Vector2 Size => _size;

        public float Thickness => _thickness;

        public float LiftAboveSurface => _liftAboveSurface;

        public Color PaperColor => _paperColor;

        public Color TextColor => _textColor;

        public float CharacterSize => _characterSize;

        public int FontSize => _fontSize;

        public string[] FontNames => _fontNames;
    }
}
