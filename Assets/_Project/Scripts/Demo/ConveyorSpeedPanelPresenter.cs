using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 컨베이어 속도 패널의 동작. 슬라이더나 −/+로 고른 속도를 IConveyorSpeedControl로 넣고, "처음 속도로"를 누르면
    /// 이 패널이 처음 본 속도로 되돌린다. 속도는 이 패널 말고도 바뀔 수 있으므로 매 프레임 지금 값을 확인해 달라진 때만 화면을 고친다.
    /// 컨베이어 구간이나 에셋은 모르고 IConveyorSpeedControl만 부르므로 속도를 정하는 쪽이 바뀌어도 이 패널은 그대로다.
    /// 범위를 벗어난 값을 맞추는 일도 구현체가 맡는다.
    /// </summary>
    public sealed class ConveyorSpeedPanelPresenter : MonoBehaviour
    {
        [Tooltip("컨베이어 속도 조절(IConveyorSpeedControl을 구현한 컴포넌트, 보통 ConveyorSpeedSetter). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _speedSource;
        [SerializeField] private ConveyorSpeedPanelView _view;

        [Tooltip("−/+ 버튼을 한 번 누를 때 바뀌는 속도(m/s).")]
        [SerializeField, Min(0.01f)] private float _nudgeMps = 0.1f;

        private IConveyorSpeedControl _control;
        private float _initialSpeedMps;
        private bool _hasShown;
        private float _shownSpeedMps;

        private void Awake()
        {
            _control = _speedSource as IConveyorSpeedControl ?? DemoComponentFinder.FindFirst<IConveyorSpeedControl>();
            if (_view == null || _control == null)
            {
                Debug.LogError($"{name}: 화면(ConveyorSpeedPanelView)과 컨베이어 속도 조절(IConveyorSpeedControl)을 구현한 컴포넌트가 모두 있어야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _view.SpeedEdited += OnSpeedEdited;
            _view.ResetRequested += OnResetRequested;
        }

        // 구현체가 Awake에서 속도를 범위 안으로 맞춘 뒤의 값이 "처음 속도"다. 모든 Awake가 끝난 Start에서 읽는다.
        private void Start()
        {
            _initialSpeedMps = _control.SpeedMps;
            _view.ShowRange(_control.MinSpeedMps, _control.MaxSpeedMps, _nudgeMps);
            _view.ShowInitialSpeed(_initialSpeedMps);
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void OnDisable()
        {
            _view.SpeedEdited -= OnSpeedEdited;
            _view.ResetRequested -= OnResetRequested;
        }

        private void OnSpeedEdited(float speedMps)
        {
            _control.SetSpeed(speedMps);
            Refresh();
        }

        private void OnResetRequested()
        {
            _control.SetSpeed(_initialSpeedMps);
            Refresh();
        }

        private void Refresh()
        {
            float speed = _control.SpeedMps;
            if (_hasShown && Mathf.Approximately(speed, _shownSpeedMps))
            {
                return;
            }

            _view.ShowSpeed(speed);
            _hasShown = true;
            _shownSpeedMps = speed;
        }
    }
}
