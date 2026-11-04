using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using MediKitTwin.Dashboard.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 관절 제어 패널의 화면. 로봇 이름 칩, 수동 제어 스위치, 관절 슬라이더 줄, 베이스 키보드 구간, 도구 끝점 글, 집는 자세 저장 구간은
    /// 모두 계층에 놓인 오브젝트이고, 이 클래스는 그 연결 고리만 쥐고 값을 채운다. 코드로 위젯을 만들거나 그리지 않는다.
    /// 로봇이 무엇인지는 모르고, 눌림과 바꾼 값은 이벤트로 알리며, 보여 줄 값은 밖에서 받는다.
    /// 관절 줄은 미리 놓아 둔 만큼(ArmJointPanelTexts.JointRowCapacity)만 있고, 로봇의 관절 수만큼 켜고 나머지는 끈다.
    /// </summary>
    public sealed class ArmJointPanelView : MonoBehaviour
    {
        [Header("머리줄")]
        [Tooltip("머리줄 끝의 접기 틀. 접힌 동안은 값을 갱신하지 않는다.")]
        [SerializeField] private UiCollapsiblePanel _panel;
        [Tooltip("로봇 이름 칩. 로봇이 여럿이면 눌러서 다음 로봇으로 바꾼다.")]
        [SerializeField] private UiChip _robotChip;

        [Header("본문")]
        [SerializeField] private Text _notReadyText;
        [Tooltip("로봇이 준비된 뒤에만 보이는 본문 전체.")]
        [SerializeField] private GameObject _readyContent;
        [SerializeField] private UiSwitchRow _manualSwitch;
        [SerializeField] private UiKindButton _homeButton;
        [SerializeField] private UiKindButton _matchButton;

        [Header("ArmA 베이스 키보드 조작")]
        [Tooltip("로봇이 굴러가는 베이스에 실려 있을 때만 보이는 구간.")]
        [SerializeField] private GameObject _baseSection;
        [SerializeField] private UiSwitchRow _keyboardSwitch;
        [SerializeField] private Text _basePose;

        [Header("도구 끝점")]
        [SerializeField] private Text _toolPosition;
        [SerializeField] private Text _toolApproach;
        [SerializeField] private Text _toolEuler;

        [Header("집는 자세 저장")]
        [Tooltip("저장할 수 있는 로봇일 때만 보이는 구간.")]
        [SerializeField] private GameObject _taughtSection;
        [SerializeField] private InputField _slotInput;
        [SerializeField] private UiKindButton _saveButton;
        [SerializeField] private UiKindButton _loadButton;
        [SerializeField] private UiKindButton _removeButton;
        [SerializeField] private Text _savedText;
        [SerializeField] private Text _taughtMessage;

        private UiSliderRow[] _jointRows = Array.Empty<UiSliderRow>();
        private UiSlotList<UiSliderRow> _jointSlots;
        private Action<float>[] _jointHandlers = Array.Empty<Action<float>>();

        /// <summary>로봇 이름 칩을 눌렀다.</summary>
        public event Action RobotChipClicked;

        /// <summary>수동 제어 스위치를 눌렀다. 바꾸려는 새 값을 알린다.</summary>
        public event Action<bool> ManualToggled;

        /// <summary>관절 슬라이더나 −/+로 값을 골랐다. 관절 번호와 도(한계 맞추기 전)를 알린다.</summary>
        public event Action<int, float> JointEdited;

        /// <summary>"시작 자세로"를 눌렀다.</summary>
        public event Action HomeRequested;

        /// <summary>"지금 자세에 맞춤"을 눌렀다.</summary>
        public event Action MatchRequested;

        /// <summary>키보드 조작 스위치를 눌렀다. 바꾸려는 새 값을 알린다.</summary>
        public event Action<bool> KeyboardToggled;

        public event Action SaveRequested;

        public event Action LoadRequested;

        public event Action RemoveRequested;

        /// <summary>패널이 접혀 있다. 접힌 동안은 값을 보여 줄 필요가 없다.</summary>
        public bool IsCollapsed => _panel.IsCollapsed;

        /// <summary>칸 ID 입력칸에 글자를 치는 중이다. 이 동안은 방향키가 베이스를 움직이면 안 된다.</summary>
        public bool IsTyping => _slotInput.isFocused;

        /// <summary>입력칸에 적힌 칸 ID.</summary>
        public string SlotId => _slotInput.text;

        private void Awake()
        {
            _jointRows = GetComponentsInChildren<UiSliderRow>(true);
            _jointSlots = new UiSlotList<UiSliderRow>(_jointRows);
            _jointHandlers = new Action<float>[_jointRows.Length];
            for (int i = 0; i < _jointRows.Length; i++)
            {
                int index = i;
                _jointHandlers[i] = degrees => JointEdited?.Invoke(index, degrees);
            }

            _notReadyText.text = ArmJointPanelTexts.NotReady;
            _manualSwitch.SetTexts(ArmJointPanelTexts.ManualTitle, ArmJointPanelTexts.ManualDescription);
            _keyboardSwitch.SetTexts(ArmJointPanelTexts.KeyboardTitle, ArmJointPanelTexts.KeyboardDescription);
            _homeButton.SetLabel(ArmJointPanelTexts.HomePose);
            _matchButton.SetLabel(ArmJointPanelTexts.MatchCurrent);
            _saveButton.SetLabel(ArmJointPanelTexts.Save);
            _loadButton.SetLabel(ArmJointPanelTexts.Load);
            _removeButton.SetLabel(ArmJointPanelTexts.Remove);
            _slotInput.characterLimit = ArmJointPanelTexts.SlotIdMaxLength;
            _slotInput.text = ArmJointPanelTexts.DefaultSlotId;
        }

        private void OnEnable()
        {
            _robotChip.Clicked += OnRobotChipClicked;
            _manualSwitch.Toggled += OnManualToggled;
            _keyboardSwitch.Toggled += OnKeyboardToggled;
            _homeButton.Button.onClick.AddListener(OnHomeClicked);
            _matchButton.Button.onClick.AddListener(OnMatchClicked);
            _saveButton.Button.onClick.AddListener(OnSaveClicked);
            _loadButton.Button.onClick.AddListener(OnLoadClicked);
            _removeButton.Button.onClick.AddListener(OnRemoveClicked);
            for (int i = 0; i < _jointRows.Length; i++)
            {
                _jointRows[i].ValueEdited += _jointHandlers[i];
            }
        }

        private void OnDisable()
        {
            _robotChip.Clicked -= OnRobotChipClicked;
            _manualSwitch.Toggled -= OnManualToggled;
            _keyboardSwitch.Toggled -= OnKeyboardToggled;
            _homeButton.Button.onClick.RemoveListener(OnHomeClicked);
            _matchButton.Button.onClick.RemoveListener(OnMatchClicked);
            _saveButton.Button.onClick.RemoveListener(OnSaveClicked);
            _loadButton.Button.onClick.RemoveListener(OnLoadClicked);
            _removeButton.Button.onClick.RemoveListener(OnRemoveClicked);
            for (int i = 0; i < _jointRows.Length; i++)
            {
                _jointRows[i].ValueEdited -= _jointHandlers[i];
            }
        }

        /// <param name="hasChoice">로봇이 여럿이라 칩을 눌러 바꿀 수 있다.</param>
        public void ShowRobot(string robotName, bool hasChoice)
        {
            _robotChip.Show(robotName, ChipTone.Neutral);
            _robotChip.SetClickable(hasChoice);
        }

        /// <summary>로봇이 준비되기 전에는 안내 글만, 준비된 뒤에는 본문을 보인다.</summary>
        public void ShowReady(bool isReady)
        {
            SetActive(_notReadyText.gameObject, !isReady);
            SetActive(_readyContent, isReady);
        }

        /// <summary>
        /// 로봇의 관절 수만큼 관절 줄을 켜고 이름과 움직이는 범위(도)를 적는다. 줄이 모자라면 앞의 줄 수만큼만 보이고 못 보인 개수를 돌려준다.
        /// </summary>
        /// <param name="nudgeDeg">−/+ 한 번에 움직이는 각도(도).</param>
        public int ShowJointLayout(IReadOnlyList<string> jointNames, JointTargetEditor editor, float nudgeDeg)
        {
            int hidden = _jointSlots.Show(jointNames, (row, jointName) => row.SetName(jointName));
            for (int i = 0; i < _jointSlots.ActiveCount; i++)
            {
                _jointRows[i].SetRange(editor.LowerDeg(i), editor.UpperDeg(i), nudgeDeg);
            }

            return hidden;
        }

        /// <param name="isManual">수동 제어 중이면 목표를 보여 주며 만질 수 있고, 아니면 지금 각도만 보여 주며 만질 수 없다.</param>
        public void ShowJointValues(JointTargetEditor editor, float[] currentRad, bool isManual)
        {
            for (int i = 0; i < _jointSlots.ActiveCount; i++)
            {
                float shownDeg = isManual ? editor.TargetDeg(i) : currentRad[i] * Mathf.Rad2Deg;
                _jointRows[i].Show(shownDeg, ArmJointPanelTexts.JointValue(shownDeg), isManual);
            }
        }

        /// <summary>수동 제어 스위치와, 수동 제어일 때만 누를 수 있는 자세 버튼들.</summary>
        public void ShowManual(bool isManual)
        {
            _manualSwitch.Show(isManual, isInteractable: true);
            _homeButton.Show(ButtonKind.Secondary, isManual);
            _matchButton.Show(ButtonKind.Secondary, isManual);
        }

        /// <param name="hasBase">로봇이 굴러가는 베이스에 실려 있다. 아니면 이 구간을 가린다.</param>
        public void ShowBase(bool hasBase, bool isKeyboardOn, string poseText)
        {
            SetActive(_baseSection, hasBase);
            if (!hasBase)
            {
                return;
            }

            _keyboardSwitch.Show(isKeyboardOn, isInteractable: true);
            _basePose.text = poseText;
        }

        public void ShowTool(Pose tool)
        {
            _toolPosition.text = ArmJointPanelTexts.ToolPosition(tool.position);
            _toolApproach.text = ArmJointPanelTexts.ToolApproach(tool.rotation * Vector3.forward);
            _toolEuler.text = ArmJointPanelTexts.ToolEuler(tool.rotation.eulerAngles);
        }

        /// <param name="hasTaught">이 로봇의 집는 자세를 저장할 수 있다. 아니면 이 구간을 가린다.</param>
        public void ShowTaught(bool hasTaught, string savedText, string message)
        {
            SetActive(_taughtSection, hasTaught);
            if (!hasTaught)
            {
                return;
            }

            _savedText.text = savedText;
            _taughtMessage.text = message;
            SetActive(_taughtMessage.gameObject, message.Length > 0);
        }

        private static void SetActive(GameObject target, bool isActive)
        {
            if (target.activeSelf != isActive)
            {
                target.SetActive(isActive);
            }
        }

        private void OnRobotChipClicked()
        {
            RobotChipClicked?.Invoke();
        }

        private void OnManualToggled(bool wanted)
        {
            ManualToggled?.Invoke(wanted);
        }

        private void OnKeyboardToggled(bool wanted)
        {
            KeyboardToggled?.Invoke(wanted);
        }

        private void OnHomeClicked()
        {
            HomeRequested?.Invoke();
        }

        private void OnMatchClicked()
        {
            MatchRequested?.Invoke();
        }

        private void OnSaveClicked()
        {
            SaveRequested?.Invoke();
        }

        private void OnLoadClicked()
        {
            LoadRequested?.Invoke();
        }

        private void OnRemoveClicked()
        {
            RemoveRequested?.Invoke();
        }
    }
}
