using System;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 배속 하나를 고르는 버튼. 어떤 배속인지(_speed)만 알고, 눌리면 그 배속을 알리며, 지금 배속과 같으면 켜진 모양으로 바뀐다.
    /// 배속을 실제로 바꾸는 일은 이 버튼이 하지 않는다(SimSpeedPanelPresenter가 한다). 계층에서 버튼을 복제하고 _speed만 고쳐도 새 배속 버튼이 된다.
    /// </summary>
    public sealed class SimSpeedButton : MonoBehaviour
    {
        [Tooltip("이 버튼이 고르는 배속. SimClock이 허용하는 값이어야 적용된다.")]
        [SerializeField] private float _speed = 1f;
        [SerializeField] private UiKindButton _button;

        /// <summary>눌렸을 때 이 버튼의 배속을 알린다.</summary>
        public event Action<float> Clicked;

        public float Speed => _speed;

        // 버튼 글은 배속에서 나오므로, 계층에서 _speed를 고쳐도 글이 어긋나지 않는다.
        private void Awake()
        {
            _button.SetLabel(SimSpeedPanelTexts.Speed(_speed));
        }

        private void OnEnable()
        {
            _button.Button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.Button.onClick.RemoveListener(OnClick);
        }

        /// <param name="isLocked">true이면 눌러도 소용없으므로 눌리지 않는 모양으로 보인다.</param>
        public void Show(float currentSpeed, bool isLocked)
        {
            _button.Show(SimSpeedButtonKinds.For(_speed, currentSpeed), !isLocked);
        }

        private void OnClick()
        {
            Clicked?.Invoke(_speed);
        }
    }
}
