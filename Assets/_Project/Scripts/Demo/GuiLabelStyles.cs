using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>화면 표시들이 함께 쓰는 IMGUI 글자 모양. 같은 스타일을 표시마다 새로 만들지 않고 한 번만 만든다. OnGUI 안에서만 부른다.</summary>
    public static class GuiLabelStyles
    {
        private static GUIStyle _centeredBold;
        private static GUIStyle _wrappedTextArea;

        /// <summary>
        /// 줄이 넘치면 줄바꿈하는 글 상자. 라벨과 달리 마우스로 골라 Ctrl+C로 복사할 수 있다.
        /// 고친 글은 호출하는 쪽이 버리므로 읽기 전용처럼 쓴다.
        /// </summary>
        public static GUIStyle WrappedTextArea
        {
            get
            {
                if (_wrappedTextArea == null)
                {
                    _wrappedTextArea = new GUIStyle(GUI.skin.textArea) { wordWrap = true, richText = false };
                }

                return _wrappedTextArea;
            }
        }

        /// <summary>가운데 정렬, 굵은 글자.</summary>
        public static GUIStyle CenteredBold
        {
            get
            {
                if (_centeredBold == null)
                {
                    _centeredBold = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                }

                return _centeredBold;
            }
        }
    }
}
