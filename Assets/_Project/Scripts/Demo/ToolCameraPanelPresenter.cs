using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇 도구 카메라가 지금 보는 화면을 보여 주는 패널의 동작. 카메라가 준비되면 플레이 내내 보이고(기본),
    /// 화면 위에는 카메라 위치와 찾은 물건 수가 겹쳐 보인다.
    /// 물건을 찾아본 직후 일정 시간 동안은 찾았으면 화면 속 물건 자리에 초록 네모를, 못 찾았으면 이유를 주황 글자로 함께 보여 준다.
    /// 카메라는 IToolCameraView로만 읽고(어떤 카메라인지 모른다), 찾아본 결과를 얼마나 보여 줄지는 ObservationShowTimer,
    /// 늦게 생기는 카메라 찾기는 LazyComponentFinder가 맡는다. 화면이 어떻게 생겼는지는 모른다(ToolCameraPanelView).
    /// 카메라가 화면을 그리지 않는 설정이면(Image가 null) 패널을 숨긴다.
    /// </summary>
    public sealed class ToolCameraPanelPresenter : MonoBehaviour
    {
        private const float FindIntervalSeconds = 1f;

        [Tooltip("보여 줄 카메라(IToolCameraView를 구현한 컴포넌트). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _camera;
        [SerializeField] private ToolCameraPanelView _view;
        [Tooltip("물건을 찾아본 뒤 인식 표시(초록 네모, 결과 글자)를 보여 주는 시간(초).")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 6f;
        [Tooltip("켜면 물건을 찾아본 뒤 위 시간 동안만 패널을 보여 준다. 끄면(기본) 카메라가 준비된 뒤 계속 보여 준다.")]
        [SerializeField] private bool _onlyAfterRecognition;

        private LazyComponentFinder<IToolCameraView> _finder;
        private ObservationShowTimer _timer;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError($"{name}: 화면(ToolCameraPanelView)이 있어야 한다.", this);
                enabled = false;
                return;
            }

            _finder = new LazyComponentFinder<IToolCameraView>(
                _camera as IToolCameraView, FindIntervalSeconds, DemoComponentFinder.FindFirst<IToolCameraView>);
            _timer = new ObservationShowTimer(_showSeconds);
        }

        private void Update()
        {
            if (_finder.Refresh(Time.unscaledTime))
            {
                _timer.Observe(_finder.Found.ObservationCount, Time.unscaledTime);
            }

            IToolCameraView camera = _finder.Found;
            bool isVisible = camera != null && camera.Image != null && (!_onlyAfterRecognition || _timer.IsVisible(Time.unscaledTime));
            _view.SetVisible(isVisible);
            if (!isVisible)
            {
                return;
            }

            ItemObservation observation = camera.LastObservation;
            bool isRecent = _timer.IsVisible(Time.unscaledTime);
            bool isDetected = isRecent && observation.IsDetected;

            _view.ShowImage(camera.Image);
            _view.ShowChips(ToolCameraPanelTexts.CameraPosition(camera.CameraPose.position), ToolCameraPanelTexts.Detect(isDetected));

            // 찾아본 순간의 화면 위치(observation.Viewport)를 쓰면 로봇이 움직인 뒤 표식이 물건에서 어긋난다.
            // 물건의 월드 위치를 카메라의 지금 자세로 매 프레임 다시 화면에 옮겨, 보이는 그림과 표식이 같은 자세를 기준으로 하게 한다.
            Vector2 viewport = Vector2.zero;
            bool hasMarker = isDetected && observation.TryViewportFrom(camera.Model, camera.CameraPose, out viewport);
            _view.ShowMarker(hasMarker, viewport);

            ShowStatus(observation, isRecent);
        }

        // 찾아본 직후가 아니면 지난 결과를 계속 보여 주지 않고 대기 중이라고만 알린다.
        private void ShowStatus(ItemObservation observation, bool isRecent)
        {
            DashboardPalette palette = DashboardPalette.Default;
            if (!isRecent)
            {
                _view.ShowStatus(ToolCameraPanelTexts.Idle, palette.Faint, palette.Muted);
                return;
            }

            // 밝은 바탕에서는 순수한 초록과 주황이 잘 안 보이므로 팔레트의 짙은 성공색과 경고색을 쓴다.
            Color color = observation.IsDetected ? palette.Success : palette.Warning;
            _view.ShowStatus(observation.Describe(), color, color);
        }
    }
}
