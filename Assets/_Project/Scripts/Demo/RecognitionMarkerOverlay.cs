using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇이 물건을 인식하면 메인 화면의 그 물건 자리에 초록 네모 표시와 짧은 글자를 그린다. 카메라 화면 자체는 ToolCameraDemoPanel이 보여 준다.
    /// 인식 결과는 IToolCameraView로만 읽고(카메라가 화면을 그리는지 모른다), 어느 결과를 얼마나 보여 줄지는 RecognitionMarkTracker가 정한다.
    /// 카메라는 로봇이 준비된 뒤에 생기므로, 생길 때까지 LazyComponentFinder가 주기적으로 찾는다.
    /// </summary>
    public sealed class RecognitionMarkerOverlay : MonoBehaviour
    {
        private const float FindIntervalSeconds = 1f;
        private const float MarkerSizePx = 36f;
        private const float MarkerLinePx = 3f;
        private const float LabelWidthPx = 160f;
        private const float LabelHeightPx = 20f;

        [Tooltip("보여 줄 카메라(IToolCameraView를 구현한 컴포넌트). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _camera;
        [Tooltip("물건을 찾은 뒤 표시를 보여 주는 시간(초).")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 3f;
        [SerializeField] private Color _color = Color.green;

        private LazyComponentFinder<IToolCameraView> _finder;
        private RecognitionMarkTracker _tracker;

        private void Awake()
        {
            _finder = new LazyComponentFinder<IToolCameraView>(
                _camera as IToolCameraView, FindIntervalSeconds, DemoComponentFinder.FindFirst<IToolCameraView>);
            _tracker = new RecognitionMarkTracker(_showSeconds);
        }

        // 찾는 일은 그리기 밖(Update)에서 한다. 결과를 추적하는 일도 여기서 한다.
        private void Update()
        {
            if (_finder.Refresh(Time.unscaledTime))
            {
                _tracker.Observe(_finder.Found.LastObservation, Time.unscaledTime);
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || _finder.Found == null)
            {
                return;
            }

            if (!_tracker.TryGetMark(Time.unscaledTime, out ItemObservation mark))
            {
                return;
            }

            Camera screenCamera = Camera.main;
            if (screenCamera == null)
            {
                return;
            }

            Vector3 screen = screenCamera.WorldToScreenPoint(mark.EstimatedPose.position);
            if (screen.z <= 0f)
            {
                return;
            }

            // 화면 좌표는 아래가 0이고 IMGUI는 위가 0이라 세로를 뒤집는다.
            float x = screen.x;
            float y = Screen.height - screen.y;
            float half = MarkerSizePx * 0.5f;
            GuiShapes.DrawFrame(new Rect(x - half, y - half, MarkerSizePx, MarkerSizePx), _color, MarkerLinePx);

            Color previous = GUI.color;
            GUI.color = _color;
            GUI.Label(new Rect(x - LabelWidthPx * 0.5f, y + half + 2f, LabelWidthPx, LabelHeightPx), $"인식 {mark.SlotId} · {mark.DistanceM:0.00}m", GuiLabelStyles.CenteredBold);
            GUI.color = previous;
        }
    }
}
