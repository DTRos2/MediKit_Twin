using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇 도구 카메라가 지금 보는 화면을 보여 주는 패널(IMGUI). 카메라가 준비되면 플레이 내내 보이고(기본),
    /// 화면 위에는 네 모서리 꺾쇠와 조준 원, 카메라 위치와 찾은 물건 수가 겹쳐 보인다.
    /// 물건을 찾아본 직후 일정 시간 동안은 찾았으면 화면 속 물건 자리에 초록 네모를, 못 찾았으면 이유를 주황 글자로 함께 보여 준다.
    /// 카메라는 IToolCameraView로만 읽고(어떤 카메라인지 모른다), 찾아본 결과를 얼마나 보여 줄지는 ObservationShowTimer,
    /// 그림을 칸에 맞춰 자르고 위치를 옮기는 계산은 CameraImageFit, 늦게 생기는 카메라 찾기는 LazyComponentFinder가 맡는다.
    /// 카메라가 화면을 그리지 않는 설정이면(Image가 null) 그리지 않는다.
    /// </summary>
    public sealed class ToolCameraDemoPanel : DemoPanel
    {
        private const float FindIntervalSeconds = 1f;
        private const float MarkerSizePx = 28f;
        private const float MarkerLinePx = 2f;
        private const float CentimetersPerMeter = 100f;
        private const string IdleStatusText = "물건 탐색 안 함 · 집으러 가면 여기에 결과 표시";

        [Tooltip("보여 줄 카메라(IToolCameraView를 구현한 컴포넌트). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _camera;
        [Tooltip("물건을 찾아본 뒤 인식 표시(초록 네모, 결과 글자)를 보여 주는 시간(초).")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 6f;
        [Tooltip("켜면 물건을 찾아본 뒤 위 시간 동안만 패널을 보여 준다. 끄면(기본) 카메라가 준비된 뒤 계속 보여 준다.")]
        [SerializeField] private bool _onlyAfterRecognition;
        [Tooltip("화면 높이의 최대(px). 카드 폭에 꽉 채우고 이보다 높아지면 위아래를 잘라 보여 준다.")]
        [SerializeField, Min(40f)] private float _maxImageHeight = 168f;
        [Tooltip("찾은 물건 자리에 그리는 네모의 색. 카메라 화면 위에 올라가므로 밝은 색을 쓴다.")]
        [SerializeField] private Color _foundMarkerColor = Color.green;

        private LazyComponentFinder<IToolCameraView> _finder;
        private ObservationShowTimer _timer;

        public ToolCameraDemoPanel()
            : base("툴 카메라", "TCP CAM", DemoPanelSlots.Camera)
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

        protected override void DrawHeaderRight()
        {
            Chip.Draw("REC", ChipTone.Danger, withDot: true, role: TextRole.MonoBold);
        }

        protected override void DrawContents()
        {
            IToolCameraView view = _finder.Found;
            ItemObservation observation = view.LastObservation;
            bool isRecent = _timer.IsVisible(Time.unscaledTime);
            bool isDetected = isRecent && observation.IsDetected;

            DrawImage(view, observation, isDetected);
            DashboardSpacing.Block();
            DrawStatus(observation, isRecent);
        }

        // 화면 영역은 배치와 그리기 이벤트 모두에서 같은 크기로 잡아야 하고, 실제로 그리는 것은 그리기 이벤트에서만 한다.
        private void DrawImage(IToolCameraView view, ItemObservation observation, bool isDetected)
        {
            Texture image = view.Image;
            float naturalHeight = ContentWidth * image.height / image.width;
            float height = Mathf.Min(naturalHeight, _maxImageHeight);
            Rect area = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));

            Rect visibleUv = CameraImageFit.VisibleUv(image.width, image.height, area.width, area.height);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTextureWithTexCoords(area, image, visibleUv, false);
            }

            CameraOverlay.DrawBrackets(area);
            CameraOverlay.DrawCrosshair(area);
            CameraOverlay.DrawChip(area, CameraPositionText(view), isRightSide: false);
            CameraOverlay.DrawChip(area, $"DETECT {(isDetected ? 1 : 0)}", isRightSide: true);

            // 찾아본 순간의 화면 위치(observation.Viewport)를 쓰면 로봇이 움직인 뒤 표식이 물건에서 어긋난다.
            // 물건의 월드 위치를 카메라의 지금 자세로 매 프레임 다시 화면에 옮겨, 보이는 그림과 표식이 같은 자세를 기준으로 하게 한다.
            if (isDetected
                && observation.TryViewportFrom(view.Model, view.CameraPose, out Vector2 viewport)
                && CameraImageFit.TryToGuiPoint(area, visibleUv, viewport, out Vector2 center))
            {
                DrawMarker(center);
            }
        }

        private void DrawMarker(Vector2 center)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float half = MarkerSizePx * 0.5f;
            GuiShapes.DrawFrame(new Rect(center.x - half, center.y - half, MarkerSizePx, MarkerSizePx), _foundMarkerColor, MarkerLinePx);
        }

        // 카메라가 월드 어디에 있는지를 cm로 적는다. 로봇이 움직이는 동안 값이 따라 바뀐다.
        private static string CameraPositionText(IToolCameraView view)
        {
            Vector3 cm = view.CameraPose.position * CentimetersPerMeter;
            return $"x {cm.x:0.0} · y {cm.y:0.0} · z {cm.z:0.0} cm";
        }

        // 찾아본 직후가 아니면 지난 결과를 계속 보여 주지 않고 대기 중이라고만 알린다.
        private static void DrawStatus(ItemObservation observation, bool isRecent)
        {
            DashboardPalette palette = DashboardPalette.Default;
            if (!isRecent)
            {
                StatusLine.Draw(IdleStatusText, palette.Faint, palette.Muted);
                return;
            }

            // 밝은 바탕에서는 순수한 초록과 주황이 잘 안 보이므로 팔레트의 짙은 성공색과 경고색을 쓴다.
            Color color = observation.IsDetected ? palette.Success : palette.Warning;
            StatusLine.Draw(observation.Describe(), color, color);
        }
    }
}
