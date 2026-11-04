using System;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 값 조절 한 줄: 이름, − 버튼, 슬라이더, + 버튼, 값 글자. 모양이 같은 줄을 여러 패널(컨베이어 속도, 관절 제어)이 같이 쓴다.
    /// 값의 단위와 자르는 규칙은 쓰는 쪽이 맡고, 이 줄은 사용자가 바꾼 새 값만 알린다(ValueEdited). 지금 값은 밖에서 Show로 받아 보여 준다.
    /// 슬라이더 막대는 어디를 눌러도 그 값으로 가고, 끌 수 있다. 조작할 수 없을 때(isInteractable가 false)는 흐리게 보이고 눌리지 않는다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiSliderRow : MonoBehaviour
    {
        [SerializeField] private Text _name;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Slider _slider;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Text _valueText;

        private CanvasGroup _group;
        private float _nudge = 1f;
        private float _shown;
        private bool _isShowing;

        /// <summary>사용자가 값을 바꿨다. 슬라이더는 범위 안의 값, − / + 는 지금 값에서 한 번 움직인 값(범위를 벗어날 수 있다)을 알린다.</summary>
        public event Action<float> ValueEdited;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            _minusButton.onClick.AddListener(OnMinus);
            _plusButton.onClick.AddListener(OnPlus);
            _slider.onValueChanged.AddListener(OnSlid);
        }

        private void OnDisable()
        {
            _minusButton.onClick.RemoveListener(OnMinus);
            _plusButton.onClick.RemoveListener(OnPlus);
            _slider.onValueChanged.RemoveListener(OnSlid);
        }

        public void SetName(string name)
        {
            _name.text = name;
        }

        /// <param name="nudge">− / + 버튼을 한 번 누를 때 바뀌는 크기.</param>
        public void SetRange(float lower, float upper, float nudge)
        {
            _isShowing = true;
            _slider.minValue = lower;
            _slider.maxValue = Mathf.Max(lower, upper);
            _isShowing = false;
            _nudge = nudge;
        }

        /// <param name="value">슬라이더가 가리킬 값.</param>
        /// <param name="valueText">오른쪽에 적을 글.</param>
        /// <param name="isInteractable">false이면 값을 보여 주기만 하고 만질 수 없다.</param>
        public void Show(float value, string valueText, bool isInteractable)
        {
            _shown = value;
            _isShowing = true;
            _slider.SetValueWithoutNotify(value);
            _isShowing = false;

            _valueText.text = valueText;
            Group.interactable = isInteractable;
            Group.alpha = isInteractable ? 1f : DashboardMetrics.DisabledAlpha;
        }

        private void OnMinus()
        {
            ValueEdited?.Invoke(_shown - _nudge);
        }

        private void OnPlus()
        {
            ValueEdited?.Invoke(_shown + _nudge);
        }

        // 값을 보여 주려고 코드가 슬라이더를 움직일 때는 사용자가 바꾼 것이 아니므로 알리지 않는다.
        private void OnSlid(float value)
        {
            if (_isShowing)
            {
                return;
            }

            _shown = value;
            ValueEdited?.Invoke(value);
        }
    }
}
