using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 화면에 그리는 시험 패널(IMGUI)의 공통 뼈대. 표시 여부, 제목, 화면 모서리 기준 자리, 창 그리기를 맡고,
    /// 파생 패널은 안에 들어갈 내용(DrawContents)만 그린다. 패널마다 같은 코드를 복사하지 않고, 자리를 바꾸는 방법도 한 곳이다.
    /// 기본 제목, 모서리, 여백, 크기는 파생 클래스가 생성자로 정하고, Inspector 값이 있으면 그 값이 우선한다.
    /// </summary>
    public abstract class DemoPanel : MonoBehaviour
    {
        // IMGUI 창 제목 줄이 차지하는 높이. 내용이 제목에 가리지 않도록 비워 둔다.
        private const float TitleBarHeight = 18f;

        [SerializeField] private string _title;
        [Tooltip("패널이 붙는 화면 모서리. 왼쪽과 오른쪽으로 나눠 놓으면 패널이 가운데에 모이지 않는다.")]
        [SerializeField] private DemoPanelAnchor _anchor;
        [Tooltip("모서리에서 떨어진 가로, 세로 거리(px).")]
        [SerializeField] private Vector2 _margin;
        [SerializeField] private Vector2 _size;
        [Tooltip("끄면 패널을 화면에 그리지 않는다.")]
        [SerializeField] private bool _isVisible = true;

        // 내용이 창보다 길면 잘리는 대신 스크롤한다.
        private Vector2 _contentScroll;

        protected DemoPanel(string defaultTitle, DemoPanelAnchor defaultAnchor, Vector2 defaultMargin, Vector2 defaultSize)
        {
            _title = defaultTitle;
            _anchor = defaultAnchor;
            _margin = defaultMargin;
            _size = defaultSize;
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            Rect rect = DemoPanelLayout.Place(_anchor, _margin, _size, Screen.width, Screen.height);
            GUILayout.BeginArea(rect, _title, GUI.skin.window);
            GUILayout.Space(TitleBarHeight);

            // 가로 막대는 숨긴다. 패널 내용은 창 너비에 맞춰 그려지므로 세로로만 스크롤하면 된다.
            _contentScroll = GUILayout.BeginScrollView(_contentScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            DrawContents();
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        /// <summary>창 안쪽에 들어갈 내용을 그린다. 창과 제목은 이미 그려져 있다.</summary>
        protected abstract void DrawContents();
    }
}
