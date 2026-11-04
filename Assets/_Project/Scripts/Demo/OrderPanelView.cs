using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using MediKitTwin.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문 패널의 화면. 버튼, 입력창, 칩, 목록 같은 위젯은 모두 계층(Hierarchy)에 놓인 오브젝트이고, 이 클래스는 그 연결 고리만 쥔다.
    /// 코드로 위젯을 만들거나 그리지 않으며, 주문 관리가 무엇인지도 모른다. 눌림은 이벤트로 알리고, 보여 줄 값은 밖에서 받아 채운다.
    /// 로직(주문 넣기, 현황 읽기)은 OrderPanelPresenter가 맡는다.
    /// </summary>
    public sealed class OrderPanelView : MonoBehaviour
    {
        [Header("머리줄")]
        [Tooltip("머리줄 오른쪽 요약(주문 건수와 키트 수).")]
        [SerializeField] private Text _summary;

        [Header("입력")]
        [SerializeField] private InputField _csvInput;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _sampleButton;
        [SerializeField] private Button _fileButton;

        [Header("주문 목록")]
        [Tooltip("미리 계층에 놓아 둔 주문 줄(OrderRowItem)들이 들어 있는 스크롤 내용 영역. 줄 수가 한 번에 보이는 주문의 최대 수다.")]
        [SerializeField] private RectTransform _orderRowContainer;
        [Tooltip("주문이 칸 수보다 많을 때 못 보인 개수를 알리는 글.")]
        [SerializeField] private Text _orderOverflow;
        [Tooltip("주문 목록 틀. 주문이 없으면 통째로 꺼진다.")]
        [SerializeField] private UiScrollListSizer _orderList;
        [Tooltip("주문이 없을 때 보이는 안내 글.")]
        [SerializeField] private GameObject _emptyNotice;

        [Header("현황")]
        [SerializeField] private UiChip _heldChip;
        [SerializeField] private UiChip _pendingChip;
        [SerializeField] private UiChip _issueChip;

        [Header("경고 목록")]
        [Tooltip("미리 계층에 놓아 둔 경고 줄(UiTextRow)들이 들어 있는 스크롤 내용 영역.")]
        [SerializeField] private RectTransform _issueRowContainer;
        [Tooltip("경고가 칸 수보다 많을 때 못 보인 개수를 알리는 글.")]
        [SerializeField] private Text _issueOverflow;
        [Tooltip("경고 목록 틀. 경고가 없으면 통째로 꺼진다.")]
        [SerializeField] private UiScrollListSizer _issueList;

        private UiSlotList<OrderRowItem> _orderRows;
        private UiSlotList<UiTextRow> _issueRows;

        /// <summary>"적용"을 눌렀다. 입력창에 적힌 CSV 글을 함께 알린다.</summary>
        public event Action<string> ApplyRequested;

        /// <summary>"샘플 채우기"를 눌렀다.</summary>
        public event Action SampleRequested;

        /// <summary>"파일 읽기"를 눌렀다.</summary>
        public event Action FileRequested;

        /// <summary>입력창에 적힌 CSV 글.</summary>
        public string CsvText
        {
            get => _csvInput.text;
            set => _csvInput.text = value ?? string.Empty;
        }

        // 줄은 편집기에서 미리 놓아 둔 것을 찾아 쓴다. 실행 중에 오브젝트를 만들지 않는다.
        private void Awake()
        {
            _orderRows = new UiSlotList<OrderRowItem>(_orderRowContainer.GetComponentsInChildren<OrderRowItem>(true));
            _issueRows = new UiSlotList<UiTextRow>(_issueRowContainer.GetComponentsInChildren<UiTextRow>(true));
        }

        private void OnEnable()
        {
            _applyButton.onClick.AddListener(OnApplyClicked);
            _sampleButton.onClick.AddListener(OnSampleClicked);
            _fileButton.onClick.AddListener(OnFileClicked);
        }

        private void OnDisable()
        {
            _applyButton.onClick.RemoveListener(OnApplyClicked);
            _sampleButton.onClick.RemoveListener(OnSampleClicked);
            _fileButton.onClick.RemoveListener(OnFileClicked);
        }

        /// <summary>주문 접수 결과를 보여 준다. 아직 불러온 적이 없으면 null을 넘긴다.</summary>
        public void ShowIntake(OrderIntakeResult intake)
        {
            var orders = intake != null ? intake.Orders : Array.Empty<OrderRecord>();
            var issues = intake != null ? intake.Issues : Array.Empty<OrderIssue>();

            _summary.gameObject.SetActive(intake != null);
            if (intake != null)
            {
                _summary.text = OrderPanelTexts.Summary(orders.Count, intake.KitRequests.Count);
            }

            ShowList(_orderRows, _orderList, _orderOverflow, orders, ShowOrder);
            _emptyNotice.SetActive(orders.Count == 0);

            ShowList(_issueRows, _issueList, _issueOverflow, issues, ShowIssue);
            _issueChip.Show(OrderPanelTexts.Issues(issues.Count), OrderChipTones.ForIssues(issues.Count), withDot: true);
        }

        /// <summary>보류 주문 수와 대기 키트 수를 보여 준다.</summary>
        public void ShowStatus(int heldOrderCount, int pendingKitCount)
        {
            _heldChip.Show(OrderPanelTexts.Held(heldOrderCount), ChipTone.Neutral);
            _pendingChip.Show(OrderPanelTexts.Pending(pendingKitCount), ChipTone.Neutral);
        }

        private static void ShowList<TView, TItem>(
            UiSlotList<TView> slots, UiScrollListSizer list, Text overflow, IReadOnlyList<TItem> items, Action<TView, TItem> bind)
            where TView : Component
        {
            int hidden = slots.Show(items, bind);

            // 칸에 들어가지 못한 항목이 있으면 몇 개인지 목록 맨 아래에 알린다.
            overflow.gameObject.SetActive(hidden > 0);
            if (hidden > 0)
            {
                overflow.text = OrderPanelTexts.Overflow(hidden);
            }

            list.gameObject.SetActive(items.Count > 0);
            if (items.Count > 0)
            {
                list.Refresh();
            }
        }

        private static void ShowOrder(OrderRowItem row, OrderRecord order)
        {
            row.Show(order);
        }

        private static void ShowIssue(UiTextRow row, OrderIssue issue)
        {
            row.Show(OrderPanelTexts.Issue(issue.Message), DashboardPalette.Default.Warning);
        }

        private void OnApplyClicked()
        {
            ApplyRequested?.Invoke(_csvInput.text);
        }

        private void OnSampleClicked()
        {
            SampleRequested?.Invoke();
        }

        private void OnFileClicked()
        {
            FileRequested?.Invoke();
        }
    }
}
