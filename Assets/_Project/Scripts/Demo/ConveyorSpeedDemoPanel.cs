using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 컨베이어 이송 속도를 조절하는 패널. 슬라이더와 −/+ 버튼으로 속도를 바꾸고, "처음 속도로" 버튼으로 이 패널이 처음 본 속도로 되돌린다.
    /// 속도는 실행 중에도 바로 반영된다. 컨베이어 구간이나 에셋은 모르고 IConveyorSpeedControl만 부르므로,
    /// 속도를 정하는 쪽이 바뀌어도 이 패널은 그대로다. 범위를 벗어난 값을 맞추는 일도 구현체가 맡는다.
    /// </summary>
    public sealed class ConveyorSpeedDemoPanel : DemoPanel
    {
        private const string SpeedLabel = "속도";
        private const string ResetLabel = "처음 속도로";

        [Tooltip("컨베이어 속도 조절(IConveyorSpeedControl을 구현한 컴포넌트, 보통 ConveyorSpeedSetter). 비워 두면 씬에서 찾는다.")]
        [SerializeField] private MonoBehaviour _speedSource;

        [Tooltip("−/+ 버튼을 한 번 누를 때 바뀌는 속도(m/s).")]
        [SerializeField, Min(0.01f)] private float _nudgeMps = 0.1f;

        private IConveyorSpeedControl _control;
        private float _initialSpeedMps;

        public ConveyorSpeedDemoPanel()
            : base("컨베이어", "CONVEYOR · 속도", DemoPanelSlots.Conveyor)
        {
        }

        private void Awake()
        {
            _control = _speedSource as IConveyorSpeedControl ?? DemoComponentFinder.FindFirst<IConveyorSpeedControl>();
            if (_control == null)
            {
                Debug.LogError($"{name}: 컨베이어 속도 조절(IConveyorSpeedControl)을 구현한 컴포넌트를 찾을 수 없다.", this);
                enabled = false;
            }
        }

        // 구현체가 Awake에서 속도를 범위 안으로 맞춘 뒤의 값이 "처음 속도"다. 모든 Awake가 끝난 Start에서 읽는다.
        private void Start()
        {
            if (_control != null)
            {
                _initialSpeedMps = _control.SpeedMps;
            }
        }

        protected override void DrawHeaderRight()
        {
            DashboardText.Inline(ConveyorSpeedText.Format(_control.SpeedMps), TextRole.Mono, DashboardPalette.Default.Muted);
        }

        protected override void DrawContents()
        {
            float speed = _control.SpeedMps;
            if (JointSliderRow.Draw(SpeedLabel, speed, _control.MinSpeedMps, _control.MaxSpeedMps, _nudgeMps, ConveyorSpeedText.Format(speed), acceptsSlider: true, out float edited))
            {
                _control.SetSpeed(edited);
            }

            DashboardSpacing.Block();
            using (DashboardRow.Begin())
            {
                if (DashboardButton.Draw($"{ResetLabel} ({ConveyorSpeedText.Format(_initialSpeedMps)})"))
                {
                    _control.SetSpeed(_initialSpeedMps);
                }
            }
        }
    }
}
