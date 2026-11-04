using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키팅 로그 패널의 화면. 머리줄의 단추들, 로그 줄 60개, 스크롤 영역은 모두 계층에 놓인 오브젝트이고, 이 클래스는 그 연결 고리만 쥐고
    /// 값을 채운다. 줄은 미리 놓아 둔 만큼(KittingLogPanelTexts.RowCapacity)만 있고, 쓰는 줄만 켜서 최근 로그로 채운다.
    /// 로그가 무엇인지는 모르고, 눌림은 이벤트로 알리며, 보여 줄 줄은 밖에서 받는다.
    /// </summary>
    public sealed class KittingLogPanelView : MonoBehaviour
    {
        [Tooltip("머리줄 끝의 접기 틀. 접힌 동안은 줄을 갱신하지 않는다.")]
        [SerializeField] private UiCollapsiblePanel _panel;
        [Tooltip("머리줄에 잠깐 적는 알림(복사 결과).")]
        [SerializeField] private Text _notice;
        [SerializeField] private UiKindButton _filterButton;
        [SerializeField] private UiKindButton _copyButton;
        [SerializeField] private UiKindButton _clearButton;
        [SerializeField] private ScrollRect _scroll;

        private UiSlotList<LogRowItem> _slots;
        private int _scrollRequestedFrame = -1;

        /// <summary>"경고·오류만"을 눌렀다.</summary>
        public event Action FilterToggled;

        public event Action CopyRequested;

        public event Action ClearRequested;

        public bool IsCollapsed => _panel.IsCollapsed;

        /// <summary>미리 놓아 둔 로그 줄 수. 한 번에 보여 줄 수 있는 최대 줄 수다.</summary>
        public int RowCapacity => _slots.Capacity;

        private void Awake()
        {
            _slots = new UiSlotList<LogRowItem>(GetComponentsInChildren<LogRowItem>(true));

            _filterButton.SetLabel(KittingLogPanelTexts.Filter);
            _copyButton.SetLabel(KittingLogPanelTexts.Copy);
            _clearButton.SetLabel(KittingLogPanelTexts.Clear);
        }

        private void OnEnable()
        {
            _filterButton.Button.onClick.AddListener(OnFilterClicked);
            _copyButton.Button.onClick.AddListener(OnCopyClicked);
            _clearButton.Button.onClick.AddListener(OnClearClicked);
        }

        private void OnDisable()
        {
            _filterButton.Button.onClick.RemoveListener(OnFilterClicked);
            _copyButton.Button.onClick.RemoveListener(OnCopyClicked);
            _clearButton.Button.onClick.RemoveListener(OnClearClicked);
        }

        // 줄 높이는 레이아웃이 다음 그리기 전에야 정해지므로, 맨 아래로 내리는 일은 줄을 바꾼 다음 프레임에 한다.
        private void LateUpdate()
        {
            if (_scrollRequestedFrame >= 0 && Time.frameCount > _scrollRequestedFrame)
            {
                _scroll.verticalNormalizedPosition = 0f;
                _scrollRequestedFrame = -1;
            }
        }

        /// <summary>쓰는 줄만 켜서 로그로 채운다(오래된 줄이 위). 줄에 들어가지 못한 개수를 돌려준다. 새 줄이 보이도록 맨 아래로 내린다.</summary>
        public int ShowRows(IReadOnlyList<LogRowEntry> rows)
        {
            int hidden = _slots.Show(rows, (item, entry) => item.Show(entry));
            _scrollRequestedFrame = Time.frameCount;
            return hidden;
        }

        /// <param name="isWarningsOnly">경고와 오류만 보고 있으면 단추가 켜진 모양이 된다.</param>
        public void ShowFilter(bool isWarningsOnly)
        {
            _filterButton.Show(isWarningsOnly ? ButtonKind.Selected : ButtonKind.Secondary, isInteractable: true);
        }

        public void ShowNotice(string text)
        {
            _notice.text = text;
        }

        private void OnFilterClicked()
        {
            FilterToggled?.Invoke();
        }

        private void OnCopyClicked()
        {
            CopyRequested?.Invoke();
        }

        private void OnClearClicked()
        {
            ClearRequested?.Invoke();
        }
    }
}
