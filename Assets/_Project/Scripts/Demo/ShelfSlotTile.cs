using System;
using MediKitTwin.Dashboard;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 칸 하나를 나타내는 눌러 볼 수 있는 칸. 위쪽에 행 색 띠, 칸 이름, 큰 재고 숫자와 단위, 귀퉁이에 보충 남은 시간이 있다.
    /// 어느 칸인지(_slotId)만 알고, 수량과 남은 시간은 밖에서 받아 보여 준다. 계층에서 칸을 복제하고 _slotId만 고쳐도 새 칸이 된다.
    /// 같은 값이면 글을 다시 만들지 않는다(매 프레임 불러도 새 문자열이 생기지 않는다).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ShelfSlotTile : MonoBehaviour
    {
        [Tooltip("이 칸이 가리키는 선반 칸 ID(예: A-1). 재고의 칸 ID와 같아야 한다.")]
        [SerializeField] private string _slotId;
        [SerializeField] private Button _button;
        [Tooltip("위쪽의 행 색 띠. 색은 그 행의 키트 종류가 정한다.")]
        [SerializeField] private Image _strip;
        [SerializeField] private Text _id;
        [SerializeField] private Text _quantity;
        [SerializeField] private Text _unit;
        [Tooltip("보충을 기다릴 때만 켜지는 남은 시간 글.")]
        [SerializeField] private Text _note;

        private CanvasGroup _group;
        private int _shownQuantity = -1;
        private int _shownRefillSeconds = -1;

        /// <summary>칸을 눌렀다. 칸 ID를 알린다.</summary>
        public event Action<string> Clicked;

        public string SlotId => _slotId;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        private void Awake()
        {
            _id.text = _slotId;
            _unit.text = ShelfStockPanelTexts.Unit;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        /// <param name="refillRemainingSeconds">보충까지 남은 시간(초). 보충을 기다리지 않으면 null.</param>
        /// <param name="isInteractable">false이면 눌리지 않고 흐리게 보인다.</param>
        public void Show(int quantity, float? refillRemainingSeconds, Color stripColor, bool isInteractable)
        {
            if (quantity != _shownQuantity)
            {
                _quantity.text = quantity.ToString();
                _shownQuantity = quantity;
            }

            // 남은 시간은 초 단위로 적으므로 초가 바뀔 때만 글을 만든다.
            int refillSeconds = refillRemainingSeconds.HasValue ? Mathf.RoundToInt(refillRemainingSeconds.Value) : -1;
            if (refillSeconds != _shownRefillSeconds)
            {
                _note.gameObject.SetActive(refillRemainingSeconds.HasValue);
                if (refillRemainingSeconds.HasValue)
                {
                    _note.text = ShelfStockPanelTexts.RefillNote(refillSeconds);
                }

                _shownRefillSeconds = refillSeconds;
            }

            _strip.color = stripColor;
            Group.interactable = isInteractable;
            Group.alpha = isInteractable ? 1f : DashboardMetrics.DisabledAlpha;
        }

        private void OnClick()
        {
            Clicked?.Invoke(_slotId);
        }
    }
}
