using System;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 컨베이어 속도 패널의 화면. 머리줄의 현재 속도 글, 속도 슬라이더 줄, "처음 속도로" 버튼은 모두 계층에 놓인 오브젝트이고,
    /// 이 클래스는 그 연결 고리만 쥔다. 컨베이어가 무엇인지는 모르고, 바꾼 값과 눌림은 이벤트로 알리며, 보여 줄 값은 밖에서 받는다.
    /// </summary>
    public sealed class ConveyorSpeedPanelView : MonoBehaviour
    {
        [Tooltip("머리줄 오른쪽에 적는 지금 속도.")]
        [SerializeField] private Text _current;
        [SerializeField] private UiSliderRow _speedRow;
        [SerializeField] private UiKindButton _resetButton;

        /// <summary>사용자가 슬라이더나 −/+로 새 속도를 골랐다(m/s, 범위 맞추기 전).</summary>
        public event Action<float> SpeedEdited;

        /// <summary>"처음 속도로"를 눌렀다.</summary>
        public event Action ResetRequested;

        private void Awake()
        {
            _speedRow.SetName(ConveyorSpeedPanelTexts.SpeedLabel);
        }

        private void OnEnable()
        {
            _speedRow.ValueEdited += OnSpeedEdited;
            _resetButton.Button.onClick.AddListener(OnResetClicked);
        }

        private void OnDisable()
        {
            _speedRow.ValueEdited -= OnSpeedEdited;
            _resetButton.Button.onClick.RemoveListener(OnResetClicked);
        }

        /// <summary>슬라이더가 움직이는 범위와 −/+ 한 번의 크기(m/s).</summary>
        public void ShowRange(float minSpeedMps, float maxSpeedMps, float nudgeMps)
        {
            _speedRow.SetRange(minSpeedMps, maxSpeedMps, nudgeMps);
        }

        public void ShowSpeed(float speedMps)
        {
            string text = ConveyorSpeedText.Format(speedMps);
            _current.text = text;
            _speedRow.Show(speedMps, text, isInteractable: true);
        }

        public void ShowInitialSpeed(float initialSpeedMps)
        {
            _resetButton.SetLabel(ConveyorSpeedPanelTexts.Reset(initialSpeedMps));
        }

        private void OnSpeedEdited(float speedMps)
        {
            SpeedEdited?.Invoke(speedMps);
        }

        private void OnResetClicked()
        {
            ResetRequested?.Invoke();
        }
    }
}
