using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇 도구 카메라가 지금 보는 화면을 보여 주는 패널(IMGUI). 카메라가 준비되면 플레이 내내 보이고(기본),
    /// 물건을 찾아본 직후 일정 시간 동안은 찾았으면 화면 속 물건 자리에 초록 네모를, 못 찾았으면 이유를 주황 글자로 함께 보여 준다.
    /// 카메라는 IToolCameraView로만 읽고(어떤 카메라인지 모른다), 찾아본 결과를 얼마나 보여 줄지는 ObservationShowTimer,
    /// 화면 위치 계산은 ViewportMapping, 늦게 생기는 카메라 찾기는 LazyComponentFinder가 맡는다.
    /// 카메라가 화면을 그리지 않는 설정이면(Image가 null) 그리지 않는다.
    /// </summary>
    public sealed class ToolCameraDemoPanel : DemoPanel
    {
        private const float FindIntervalSeconds = 1f;
        private const float ImageWidthPx = 320f;
        private const float MarkerSizePx = 28f;
        private const float MarkerLinePx = 2f;

        [Tooltip("보여 줄 카메라(IToolCameraView를 구현한 컴포넌트). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _camera;
        [Tooltip("물건을 찾아본 뒤 인식 표시(초록 네모, 결과 글자)를 보여 주는 시간(초).")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 6f;
        [Tooltip("켜면 물건을 찾아본 뒤 위 시간 동안만 패널을 보여 준다. 끄면(기본) 카메라가 준비된 뒤 계속 보여 준다.")]
        [SerializeField] private bool _onlyAfterRecognition;
        [SerializeField] private Color _foundColor = Color.green;
        [SerializeField] private Color _missedColor = new Color(1f, 0.6f, 0.2f);

        private LazyComponentFinder<IToolCameraView> _finder;
        private ObservationShowTimer _timer;

        // 왼쪽 위의 Orders(CSV) 패널(가로 340, 여백 10) 바로 오른쪽에 10px 띄워 놓는다.
        public ToolCameraDemoPanel()
            : base("Tool Camera", DemoPanelAnchor.TopLeft, new Vector2(360f, 10f), new Vector2(360f, 350f))
        {
        }

        protected override bool ShouldDraw =>
            _finder != null
            && _finder.Found != null
            && _finder.Found.Image != null
            && (!_onlyAfterRecognition || _timer.IsVisible(Time.unscaledTime));

        private void Awake()
        {
            _finder = new LazyComponentFinder<IToolCameraView>(
                _camera as IToolCameraView, FindIntervalSeconds, DemoComponentFinder.FindFirst<IToolCameraView>);
            _timer = new ObservationShowTimer(_showSeconds);
        }

        // 찾는 일과 횟수 읽기는 그리기 밖(Update)에서 한다.
        private void Update()
        {
            if (_finder.Refresh(Time.unscaledTime))
            {
                _timer.Observe(_finder.Found.ObservationCount, Time.unscaledTime);
            }
        }

        protected override void DrawContents()
        {
            IToolCameraView view = _finder.Found;
            Texture image = view.Image;
            ItemObservation observation = view.LastObservation;
            bool isRecent = _timer.IsVisible(Time.unscaledTime);

            // 화면 영역은 배치와 그리기 이벤트 모두에서 같은 크기로 잡아야 하고, 실제로 그리는 것은 그리기 이벤트에서만 한다.
            float height = ImageWidthPx * image.height / image.width;
            Rect imageArea = GUILayoutUtility.GetRect(ImageWidthPx, height, GUILayout.Width(ImageWidthPx), GUILayout.Height(height));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(imageArea, image, ScaleMode.StretchToFill, false);
                if (isRecent && observation.IsDetected)
                {
                    DrawMarker(imageArea, observation);
                }
            }

            DrawStatus(observation, isRecent);
        }

        private void DrawMarker(Rect imageArea, ItemObservation observation)
        {
            Vector2 center = ViewportMapping.ToGuiPoint(imageArea, observation.Viewport);
            float half = MarkerSizePx * 0.5f;
            GuiShapes.DrawFrame(new Rect(center.x - half, center.y - half, MarkerSizePx, MarkerSizePx), _foundColor, MarkerLinePx);
        }

        // 찾아본 직후가 아니면 지난 결과를 계속 보여 주지 않고 대기 중이라고만 알린다.
        private void DrawStatus(ItemObservation observation, bool isRecent)
        {
            Color previous = GUI.contentColor;
            if (isRecent)
            {
                GUI.contentColor = observation.IsDetected ? _foundColor : _missedColor;
                GUILayout.Label(observation.Describe());
            }
            else
            {
                GUILayout.Label("물건을 찾아보는 중이 아니다 (집으러 가면 여기에 결과가 나온다)");
            }

            GUI.contentColor = previous;
        }
    }
}
