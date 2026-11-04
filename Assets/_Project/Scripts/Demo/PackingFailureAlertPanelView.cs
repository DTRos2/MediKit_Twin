using System;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 팩킹 실패 경고 패널의 화면. 계층에 놓인 글 둘과 "확인하고 다시 덮기" 버튼을 다루기만 하고, 언제 뜨고 무엇을 하는지는 모른다.
    /// 평소에는 보이지 않는다. 오브젝트를 끄지 않고 CanvasGroup으로 숨기는 것은, 꺼진 패널은 사건을 받을 수 없기 때문이다.
    /// </summary>
    public sealed class PackingFailureAlertPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Text _headline;
        [SerializeField] private Text _reason;
        [SerializeField] private UiKindButton _retryButton;

        /// <summary>사용자가 "확인하고 다시 덮기"를 눌렀음을 알린다.</summary>
        public event Action RetryRequested;

        public bool IsShown { get; private set; }

        private void OnEnable()
        {
            _retryButton.Button.onClick.AddListener(OnRetryClicked);
        }

        private void OnDisable()
        {
            _retryButton.Button.onClick.RemoveListener(OnRetryClicked);
        }

        public void Show(string headline, string reason)
        {
            _headline.text = headline;
            _reason.text = reason;
            Apply(true);
        }

        public void Hide()
        {
            Apply(false);
        }

        private void Apply(bool isShown)
        {
            IsShown = isShown;
            _group.alpha = isShown ? 1f : 0f;
            _group.interactable = isShown;
            _group.blocksRaycasts = isShown;
        }

        private void OnRetryClicked()
        {
            RetryRequested?.Invoke();
        }
    }
}
