using System;
using MediKitTwin.Common;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 선반 재고 패널의 화면. 칸 타일, 스위치 줄, 키트 종류 버튼, 안내 글은 모두 계층에 놓인 오브젝트이고, 이 클래스는 그 연결 고리만 쥐고 값을 채운다.
    /// 코드로 위젯을 만들거나 그리지 않으며, 재고가 무엇인지는 IStock으로 읽기만 한다. 눌림은 이벤트로 알린다.
    /// 행, 칸 타일, 키트 종류 버튼은 이 오브젝트 아래에서 찾으므로 계층에서 더하거나 빼면 그대로 따라온다.
    /// </summary>
    public sealed class ShelfStockPanelView : MonoBehaviour
    {
        [Tooltip("머리줄 오른쪽 요약(선반 전체 재고 수).")]
        [SerializeField] private Text _total;

        [Header("모드")]
        [Tooltip("수동 키팅 모드 스위치. 수동 키팅이 없는 씬에서는 꺼진다.")]
        [SerializeField] private UiSwitchRow _manualSwitch;
        [Tooltip("되돌리기 모드 스위치. 수동 모드에서는 가려진다.")]
        [SerializeField] private UiSwitchRow _returnSwitch;
        [Tooltip("수동 모드에서만 보이는 키트 종류 버튼들과 새 박스 버튼이 든 줄.")]
        [SerializeField] private GameObject _manualOperations;
        [SerializeField] private UiKindButton _newBoxButton;

        [Header("안내")]
        [SerializeField] private Text _message;

        private ShelfRowItem[] _rows = Array.Empty<ShelfRowItem>();
        private ManualKitCodeButton[] _kitButtons = Array.Empty<ManualKitCodeButton>();

        /// <summary>선반 칸을 눌렀다. 칸 ID를 알린다.</summary>
        public event Action<string> SlotPressed;

        /// <summary>수동 키팅 스위치를 눌렀다. 바꾸려는 새 값을 알린다.</summary>
        public event Action<bool> ManualModeToggled;

        /// <summary>되돌리기 스위치를 눌렀다. 바꾸려는 새 값을 알린다.</summary>
        public event Action<bool> ReturnModeToggled;

        /// <summary>키트 종류 버튼을 눌렀다.</summary>
        public event Action<string> KitCodeChosen;

        /// <summary>"새 박스"를 눌렀다.</summary>
        public event Action NewBoxRequested;

        private void Awake()
        {
            _rows = GetComponentsInChildren<ShelfRowItem>(true);
            _kitButtons = GetComponentsInChildren<ManualKitCodeButton>(true);

            _manualSwitch.SetTexts(ShelfStockPanelTexts.ManualTitle, ShelfStockPanelTexts.ManualDescription);
            _returnSwitch.SetTexts(ShelfStockPanelTexts.ReturnTitle, ShelfStockPanelTexts.ReturnDescription);
            _newBoxButton.SetLabel(ShelfStockPanelTexts.NewBox);
        }

        private void OnEnable()
        {
            _manualSwitch.Toggled += OnManualToggled;
            _returnSwitch.Toggled += OnReturnToggled;
            _newBoxButton.Button.onClick.AddListener(OnNewBoxClicked);
            foreach (ShelfRowItem row in _rows)
            {
                foreach (ShelfSlotTile tile in row.Tiles)
                {
                    tile.Clicked += OnSlotClicked;
                }
            }

            foreach (ManualKitCodeButton button in _kitButtons)
            {
                button.Clicked += OnKitCodeClicked;
            }
        }

        private void OnDisable()
        {
            _manualSwitch.Toggled -= OnManualToggled;
            _returnSwitch.Toggled -= OnReturnToggled;
            _newBoxButton.Button.onClick.RemoveListener(OnNewBoxClicked);
            foreach (ShelfRowItem row in _rows)
            {
                foreach (ShelfSlotTile tile in row.Tiles)
                {
                    tile.Clicked -= OnSlotClicked;
                }
            }

            foreach (ManualKitCodeButton button in _kitButtons)
            {
                button.Clicked -= OnKitCodeClicked;
            }
        }

        /// <summary>행마다 어떤 키트 종류의 재료가 놓이는지 알려 행 글자와 색띠를 정한다.</summary>
        public void ShowRowKits(ShelfRowKits kits)
        {
            foreach (ShelfRowItem row in _rows)
            {
                row.ShowKit(kits.KitCodeOf(row.Letter));
            }
        }

        public void ShowTotal(int total)
        {
            _total.text = ShelfStockPanelTexts.Total(total);
        }

        /// <param name="hasManualKitting">수동 키팅이 씬에 있다. 없으면 수동 스위치를 가린다.</param>
        /// <param name="isRobotBusy">로봇이 칸을 집는 중이면 키트 종류와 새 박스를 누를 수 없다.</param>
        public void ShowMode(bool hasManualKitting, bool isManualMode, bool isReturnMode, bool isRobotBusy, string selectedKitCode)
        {
            _manualSwitch.gameObject.SetActive(hasManualKitting);
            _manualSwitch.Show(isManualMode, isInteractable: true);

            // 수동 모드에서는 키트 종류 고르기와 새 박스가, 아니면 되돌리기 스위치가 같은 자리를 쓴다.
            _manualOperations.SetActive(isManualMode);
            _returnSwitch.gameObject.SetActive(!isManualMode);
            _returnSwitch.Show(isReturnMode, isInteractable: true);

            foreach (ManualKitCodeButton button in _kitButtons)
            {
                button.Show(selectedKitCode, !isRobotBusy);
            }

            _newBoxButton.Show(ButtonKind.Secondary, !isRobotBusy);
        }

        /// <param name="isInteractable">수동 모드에서 로봇이 움직일 수 없는 때는 칸을 누를 수 없다.</param>
        public void ShowTiles(IStock stock, bool isInteractable)
        {
            foreach (ShelfRowItem row in _rows)
            {
                foreach (ShelfSlotTile tile in row.Tiles)
                {
                    tile.Show(stock.GetQuantity(tile.SlotId), stock.GetRefillRemainingSeconds(tile.SlotId), row.StripColor, isInteractable);
                }
            }
        }

        public void ShowMessage(string message)
        {
            _message.text = message;
        }

        private void OnSlotClicked(string slotId)
        {
            SlotPressed?.Invoke(slotId);
        }

        private void OnManualToggled(bool wanted)
        {
            ManualModeToggled?.Invoke(wanted);
        }

        private void OnReturnToggled(bool wanted)
        {
            ReturnModeToggled?.Invoke(wanted);
        }

        private void OnKitCodeClicked(string kitCode)
        {
            KitCodeChosen?.Invoke(kitCode);
        }

        private void OnNewBoxClicked()
        {
            NewBoxRequested?.Invoke();
        }
    }
}
