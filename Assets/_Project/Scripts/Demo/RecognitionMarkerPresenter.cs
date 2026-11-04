using System.Globalization;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇이 물건을 인식하면 메인 화면의 그 물건 자리에 초록 네모 표시와 짧은 글자를 보이게 하는 동작. 카메라 화면 자체는 ToolCameraPanelPresenter가 보여 준다.
    /// 인식 결과는 IToolCameraView로만 읽고(카메라가 화면을 그리는지 모른다), 어느 결과를 얼마나 보여 줄지는 RecognitionMarkTracker가 정한다.
    /// 카메라는 로봇이 준비된 뒤에 생기므로, 생길 때까지 LazyComponentFinder가 주기적으로 찾는다. 표시가 어떻게 생겼는지는 모른다(RecognitionMarkerView).
    /// </summary>
    public sealed class RecognitionMarkerPresenter : MonoBehaviour
    {
        private const float FindIntervalSeconds = 1f;

        [Tooltip("보여 줄 카메라(IToolCameraView를 구현한 컴포넌트). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _camera;
        [SerializeField] private RecognitionMarkerView _view;
        [Tooltip("물건을 찾은 뒤 표시를 보여 주는 시간(초).")]
        [SerializeField, Min(0.5f)] private float _showSeconds = 3f;

        private LazyComponentFinder<IToolCameraView> _finder;
        private RecognitionMarkTracker _tracker;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError($"{name}: 화면(RecognitionMarkerView)이 있어야 한다.", this);
                enabled = false;
                return;
            }

            _finder = new LazyComponentFinder<IToolCameraView>(
                _camera as IToolCameraView, FindIntervalSeconds, DemoComponentFinder.FindFirst<IToolCameraView>);
            _tracker = new RecognitionMarkTracker(_showSeconds);
        }

        private void Update()
        {
            if (_finder.Refresh(Time.unscaledTime))
            {
                _tracker.Observe(_finder.Found.LastObservation, Time.unscaledTime);
            }

            if (_finder.Found == null || !_tracker.TryGetMark(Time.unscaledTime, out ItemObservation mark))
            {
                _view.Hide();
                return;
            }

            Camera screenCamera = Camera.main;
            if (screenCamera == null)
            {
                _view.Hide();
                return;
            }

            Vector3 screen = screenCamera.WorldToScreenPoint(mark.EstimatedPose.position);
            if (screen.z <= 0f)
            {
                _view.Hide();
                return;
            }

            _view.Show(new Vector2(screen.x, screen.y), Label(mark));
        }

        private static string Label(ItemObservation mark)
        {
            return string.Format(CultureInfo.InvariantCulture, "인식 {0} · {1:0.00}m", mark.SlotId, mark.DistanceM);
        }
    }
}
