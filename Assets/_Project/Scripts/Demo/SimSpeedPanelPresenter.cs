using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 배속 패널의 동작. 버튼이 눌리면 ISimSpeedControl로 배속을 바꾸고, 배속이나 고정 여부가 바뀐 프레임에만 화면을 갱신한다.
    /// 배속은 이 패널 말고도 바뀔 수 있으므로(ROS 제어가 시작되면 1배속으로 고정된다) 매 프레임 지금 값을 확인한다.
    /// 시계가 무엇인지는 ISimSpeedControl 뒤에 숨어 있고, 기본 구현은 SimClock이다.
    /// </summary>
    public sealed class SimSpeedPanelPresenter : MonoBehaviour
    {
        [SerializeField] private SimSpeedPanelView _view;

        private ISimSpeedControl _control;
        private bool _hasShown;
        private float _shownSpeed;
        private bool _shownLocked;

        /// <summary>다른 배속 구현으로 바꾼다(시험용). Awake 전에 부르면 기본 구현을 만들지 않는다.</summary>
        public void Initialize(ISimSpeedControl control)
        {
            _control = control;
            _hasShown = false;
        }

        private void Awake()
        {
            if (_control == null)
            {
                _control = new SimClockSpeedControl();
            }
        }

        private void OnEnable()
        {
            _view.SpeedRequested += OnSpeedRequested;
            Refresh();
        }

        private void OnDisable()
        {
            _view.SpeedRequested -= OnSpeedRequested;
        }

        private void Update()
        {
            Refresh();
        }

        private void OnSpeedRequested(float speed)
        {
            _control.TrySetSpeed(speed);
            Refresh();
        }

        private void Refresh()
        {
            float speed = _control.Speed;
            bool isLocked = _control.IsLocked;
            if (_hasShown && Mathf.Approximately(speed, _shownSpeed) && isLocked == _shownLocked)
            {
                return;
            }

            _view.Show(speed, isLocked);
            _hasShown = true;
            _shownSpeed = speed;
            _shownLocked = isLocked;
        }
    }
}
