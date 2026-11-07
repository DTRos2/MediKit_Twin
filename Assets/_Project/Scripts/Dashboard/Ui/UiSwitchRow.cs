using System;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Dashboard.Ui
{
    /// <summary>
    /// 제목과 설명이 왼쪽에, 켜고 끄는 스위치가 오른쪽에 있는 한 줄. 줄 어디를 눌러도 바뀐다.
    /// 지금 켜졌는지는 밖에서 Show로 받아 보여 주고, 눌리면 바꾸려는 새 값만 알린다(Toggled). 값을 직접 뒤집지 않으므로
    /// 실제 상태가 거절하면(예: 로봇이 움직이는 중) 줄은 그대로 있다.
    /// 조작할 수 없을 때(isInteractable가 false)는 흐리게 보이고 눌리지 않는다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiSwitchRow : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _title;
        [SerializeField] private Text _description;
        [SerializeField] private Image _track;
        [Tooltip("스위치 위를 움직이는 둥근 손잡이. 켜면 오른쪽, 끄면 왼쪽 끝에 붙는다.")]
        [SerializeField] private RectTransform _thumb;

        private CanvasGroup _group;
        private bool _isOn;

        /// <summary>줄을 눌렀다. 바꾸려는 새 값(지금이 꺼짐이면 true)을 알린다.</summary>
        public event Action<bool> Toggled;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void SetTexts(string title, string description)
        {
            _title.text = title;
            _description.text = description;
        }

        public void Show(bool isOn, bool isInteractable)
        {
            _isOn = isOn;

            DashboardPalette palette = DashboardPalette.Default;
            _track.color = isOn ? palette.Accent : palette.Track;

            // 켜면 오른쪽 끝, 끄면 왼쪽 끝에서 같은 거리만큼 띄워 붙인다.
            float side = isOn ? 1f : 0f;
            float inset = DashboardMetrics.SwitchThumbInset;
            _thumb.anchorMin = new Vector2(side, 0.5f);
            _thumb.anchorMax = new Vector2(side, 0.5f);
            _thumb.pivot = new Vector2(side, 0.5f);
            _thumb.anchoredPosition = new Vector2(isOn ? -inset : inset, 0f);

            Group.interactable = isInteractable;
            Group.alpha = isInteractable ? 1f : DashboardMetrics.DisabledAlpha;
        }

        private void OnClick()
        {
            Toggled?.Invoke(!_isOn);
        }
    }
}
