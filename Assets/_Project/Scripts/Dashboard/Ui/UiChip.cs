using System;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 작은 알약 모양 표시("보류 0건", "경고 없음"). 글과 계열(ChipTone)만 받고, 계열에 맞는 바탕색과 글자색은
    /// IMGUI 칩과 같은 ChipColors가 정하므로 두 화면의 칩 색이 어긋나지 않는다.
    /// 눌러 볼 수 있는 칩(로봇 이름 칩처럼 눌러서 다음으로 넘기는 것)은 칩 전체를 덮는 버튼을 이어 두면 눌림을 Clicked로 알린다.
    /// 누를 수 없을 때는 SetClickable(false)로 두면 눌림을 가로채지 않으므로 뒤에 있는 것(머리줄 접기 단추)이 눌린다.
    /// </summary>
    public sealed class UiChip : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [Tooltip("글 앞의 작은 점. 점을 쓰지 않는 칩이면 비워 둔다.")]
        [SerializeField] private Image _dot;
        [SerializeField] private Text _label;
        [Tooltip("눌러 볼 수 있는 칩이면 칩 바탕에 붙은 버튼. 누를 수 없는 칩이면 비워 둔다.")]
        [SerializeField] private Button _button;

        /// <summary>칩을 눌렀다. 누를 수 있는 칩(버튼이 있고 SetClickable(true))에서만 난다.</summary>
        public event Action Clicked;

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        public void Show(string text, ChipTone tone, bool withDot = false)
        {
            ChipColors.Resolve(tone, DashboardPalette.Default, out Color background, out Color foreground);

            _background.color = background;
            _label.text = text;
            _label.color = foreground;

            if (_dot != null)
            {
                _dot.gameObject.SetActive(withDot);
                _dot.color = foreground;
            }
        }

        /// <summary>눌러 볼 수 있게 하거나 막는다. 막으면 눌림을 받지 않아 뒤의 오브젝트로 넘어간다. 버튼이 없는 칩에서는 아무 일도 없다.</summary>
        public void SetClickable(bool isClickable)
        {
            if (_button == null)
            {
                return;
            }

            _button.interactable = isClickable;
            _background.raycastTarget = isClickable;
        }

        private void OnClick()
        {
            Clicked?.Invoke();
        }
    }
}
