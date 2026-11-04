using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 관절을 하나씩 손으로 움직여 보는 패널의 동작. 관절마다 슬라이더와 -/+ 버튼이 있고, 시작 자세로 보내거나 지금 자세에 맞출 수 있다.
    /// 어느 각도에서 물건에 닿는지, 어느 관절이 한계에 붙는지를 눈으로 찾는 데 쓴다.
    /// 로봇은 IArmState(읽기)와 IArmDriver(쓰기)로만 다루므로 FR5, UR3 어느 쪽에도 쓴다. 조작할 로봇을 지정하면 그 로봇만, 아니면 칩을 눌러 바꿔 가며 조작한다.
    /// "수동 제어"를 켠 동안만 목표를 보낸다. 끄면 현재 각도를 보여 주기만 한다.
    /// 수동 제어를 켜면 로봇의 자동 목표(IK)를 막는다(IManualHold). 남은 IK 목표를 지우고 새 목표도 받지 않으므로 하던 자동 키팅은
    /// 정리되어 끝나고 쉬며, 팔은 그 자리에 남아 사람이 관절을 맞출 수 있다. 끄면 자동 키팅이 다시 이어진다.
    /// 로봇이 굴러가는 베이스(ArmA)에 실려 있으면 키보드로 베이스를 움직이고 자리를 Console에 적는다(ArmBaseControlSection).
    /// 베이스 키보드 조작은 수동 제어 켜기와 상관없이 키를 누를 때만 움직이므로, 자동 키팅 중에는 키를 누르지 않는다.
    /// 화면이 어떻게 생겼는지는 모른다(ArmJointPanelView). 패널이 접힌 동안은 값을 갱신하지 않지만 키보드 조작은 계속 읽는다.
    /// </summary>
    public sealed class ArmJointPanelPresenter : MonoBehaviour
    {
        [Tooltip("조작할 로봇(IArmState와 IArmDriver를 구현한 컴포넌트, 보통 ArmController). 지정하면 이 로봇만 조작하고 다른 로봇은 고르지 않는다. 비워 두면 씬에서 찾은 모든 로봇을 칩으로 바꿔 가며 조작한다.")]
        [SerializeField] private MonoBehaviour _arm;
        [SerializeField] private ArmJointPanelView _view;

        [Tooltip("-/+ 버튼을 한 번 누를 때 움직이는 각도(도).")]
        [SerializeField, Min(0.1f)] private float _nudgeDeg = 5f;

        [Header("ArmA(굴러가는 베이스) 키보드 조작")]
        [Tooltip("방향키로 움직일 때의 속도(m/s).")]
        [SerializeField, Min(0.01f)] private float _baseMoveSpeedMps = 0.5f;
        [Tooltip(", . 키로 돌 때의 속도(도/초).")]
        [SerializeField, Min(1f)] private float _baseTurnSpeedDegPerSec = 45f;
        [Tooltip("오른쪽 Shift를 누르면 속도에 곱하는 값.")]
        [SerializeField, Min(1f)] private float _baseFastMultiplier = 4f;

        private CandidateSelection<MonoBehaviour> _robots;

        private IArmState _state;
        private IArmDriver _driver;
        private IManualHold _hold;

        private ArmBaseControlSection _baseSection;
        private TaughtPoseSection _taughtSection;
        private IPickPoseLibrary _pickPoses;

        private JointTargetEditor _editor;
        private float[] _currentRad;
        private float[] _targetRad;
        private bool _isManual;

        // 사용자가 키보드 조작 스위치로 바란 값. 글자를 치는 동안은 이 값과 상관없이 키를 읽지 않는다.
        private bool _isKeyboardWanted = true;

        private void Awake()
        {
            if (_view == null)
            {
                Debug.LogError($"{name}: 화면(ArmJointPanelView)이 있어야 한다.", this);
                enabled = false;
                return;
            }

            // 비어 있는 칸은 에디터에서 가짜 null일 수 있어, Unity의 ==로 확인해 진짜 null로 넘긴다.
            MonoBehaviour only = _arm != null ? _arm : null;

            // 상태를 읽는 쪽과 목표를 주는 쪽이 같은 로봇이어야 하므로 둘 다 구현한 것만 후보로 삼는다.
            _robots = new CandidateSelection<MonoBehaviour>(DemoComponentFinder.FindAll<IArmState, IArmDriver>(), only);

            if (_robots.IsOnlyMissing)
            {
                Debug.LogWarning($"{name}: 지정한 로봇 {_arm.name}을(를) 조작할 수 없다(꺼져 있거나 IArmState와 IArmDriver를 구현하지 않음). 씬에서 찾은 로봇으로 대신한다.", this);
            }

            if (_robots.IsEmpty)
            {
                Debug.LogError($"{name}: 로봇암(IArmState와 IArmDriver를 구현한 컴포넌트)을 찾을 수 없다.", this);
                enabled = false;
                return;
            }

            // 알려 준 집는 자세를 저장하는 파일. 자동 키팅(ArmItemTransfer)이 같은 파일을 읽는다.
            _pickPoses = new TaughtPickPoseRepository(FilePickPoseStorage.CreateDefault());

            SelectRobot(_robots.Current);
        }

        private void OnEnable()
        {
            _view.RobotChipClicked += OnRobotChipClicked;
            _view.ManualToggled += OnManualToggled;
            _view.JointEdited += OnJointEdited;
            _view.HomeRequested += OnHomeRequested;
            _view.MatchRequested += OnMatchRequested;
            _view.KeyboardToggled += OnKeyboardToggled;
            _view.SaveRequested += OnSaveRequested;
            _view.LoadRequested += OnLoadRequested;
            _view.RemoveRequested += OnRemoveRequested;
        }

        private void Update()
        {
            if (_baseSection != null)
            {
                _baseSection.IsKeyboardEnabled = _isKeyboardWanted && !_view.IsTyping;
                _baseSection.Update(Time.unscaledDeltaTime);
            }

            if (!_view.IsCollapsed)
            {
                Refresh();
            }
        }

        // 패널이 꺼지거나 사라질 때 수동 제어로 막아 둔 자동 목표를 풀어, 로봇이 영영 자동 동작을 못 받는 일이 없게 한다.
        private void OnDisable()
        {
            ReleaseManual();
            _view.RobotChipClicked -= OnRobotChipClicked;
            _view.ManualToggled -= OnManualToggled;
            _view.JointEdited -= OnJointEdited;
            _view.HomeRequested -= OnHomeRequested;
            _view.MatchRequested -= OnMatchRequested;
            _view.KeyboardToggled -= OnKeyboardToggled;
            _view.SaveRequested -= OnSaveRequested;
            _view.LoadRequested -= OnLoadRequested;
            _view.RemoveRequested -= OnRemoveRequested;
        }

        private void Refresh()
        {
            if (!_state.IsReady)
            {
                _view.ShowReady(false);
                return;
            }

            EnsureEditor();
            _view.ShowReady(true);
            _state.GetJointPositions(_currentRad);

            _view.ShowManual(_isManual);
            _view.ShowJointValues(_editor, _currentRad, _isManual);
            _view.ShowTool(_state.ToolPose);
            _view.ShowBase(_baseSection != null, _isKeyboardWanted, _baseSection != null ? _baseSection.PoseText : string.Empty);
            _view.ShowTaught(
                _taughtSection != null,
                _taughtSection != null ? _taughtSection.SavedText : string.Empty,
                _taughtSection != null ? _taughtSection.Message : string.Empty);
        }

        private void SelectRobot(MonoBehaviour robot)
        {
            // 다른 로봇으로 바꾸기 전에 이전 로봇의 수동 제어를 푼다. 풀지 않으면 이전 로봇이 막힌 채로 남는다.
            ReleaseManual();

            _state = (IArmState)robot;
            _driver = (IArmDriver)robot;
            _hold = robot.GetComponent<IManualHold>();
            _baseSection = ArmBaseControlSection.TryCreate(robot, _baseMoveSpeedMps, _baseTurnSpeedDegPerSec, _baseFastMultiplier);
            _taughtSection = TaughtPoseSection.TryCreate(robot, _baseSection, _pickPoses, LoadTaughtPose);

            // 로봇마다 관절 수와 한계가 다르므로 다음 갱신에서 다시 만든다. 이전 로봇은 손대지 않고 그대로 둔다.
            _editor = null;
            _isManual = false;

            _view.ShowRobot(robot.name, _robots.HasChoice);
        }

        private void EnsureEditor()
        {
            if (_editor != null)
            {
                return;
            }

            int count = _state.JointNames.Count;
            var lower = new float[count];
            var upper = new float[count];
            _state.GetJointLimits(lower, upper);

            _editor = new JointTargetEditor(lower, upper);
            _currentRad = new float[count];
            _targetRad = new float[count];

            int hidden = _view.ShowJointLayout(_state.JointNames, _editor, _nudgeDeg);
            if (hidden > 0)
            {
                Debug.LogWarning($"{name}: 관절 줄이 모자라 {hidden}개 관절은 화면에 나오지 않는다(프리팹의 관절 줄을 늘린다).", this);
            }
        }

        // 켜는 순간 자동 목표를 막고(남은 IK 목표를 지우고 새 목표도 받지 않는다), 목표를 지금 자세에 맞춰 팔이 튀지 않게 한다.
        // 막힌 동안 하던 자동 키팅은 정리되어 끝나고 쉬므로 팔은 그 자리에 남는다. 끄면 자동 키팅이 다시 이어진다.
        private void SetManual(bool isManual)
        {
            if (!isManual)
            {
                ReleaseManual();
                return;
            }

            _isManual = true;
            _hold?.BeginHold();
            _editor.Reset(_currentRad);
            SendTargets();
        }

        // 저장된 자세로 로봇을 옮긴다: ArmA를 저장된 자리와 방향에 놓고 관절 목표를 저장된 각도로 보낸다.
        // 팔이 자동 동작에 밀리지 않도록 수동 제어를 먼저 켠다(켜지 않았을 때만). 슬라이더 목표도 같이 맞춰 다음 조작에서 튀지 않게 한다.
        private void LoadTaughtPose(TaughtPickPose pose)
        {
            if (!_isManual)
            {
                SetManual(true);
            }

            _baseSection.PlaceAt(pose.RootPosition, pose.RootYawDeg);
            _editor.Reset(pose.JointsRad());
            SendTargets();
        }

        // 수동 제어를 끄고 막아 둔 자동 목표를 푼다. 켜져 있지 않아도 안전하게 부를 수 있다.
        private void ReleaseManual()
        {
            _isManual = false;
            _hold?.EndHold();
        }

        private void SendTargets()
        {
            _editor.CopyTargetsTo(_targetRad);
            _driver.SetJointTargets(_targetRad);
        }

        // 화면의 눌림은 본문이 보이는 동안에만 오므로 편집기는 이미 만들어져 있다. 그래도 로봇을 바꾼 바로 다음 프레임 사이를 위해 확인한다.
        private bool CanEdit => _editor != null && _state.IsReady;

        private void OnRobotChipClicked()
        {
            SelectRobot(_robots.SelectNext());
        }

        private void OnManualToggled(bool wanted)
        {
            if (CanEdit)
            {
                SetManual(wanted);
            }
        }

        private void OnJointEdited(int index, float degrees)
        {
            if (!CanEdit || !_isManual || index >= _editor.Count)
            {
                return;
            }

            _editor.SetTargetDeg(index, degrees);
            SendTargets();
        }

        private void OnHomeRequested()
        {
            if (!CanEdit || !_isManual)
            {
                return;
            }

            _editor.Reset(_driver.HomePoseRad);
            SendTargets();
        }

        // 슬라이더로 정한 목표를 버리고 목표를 지금 각도로 되돌린다. 슬라이더 값을 적용하는 버튼이 아니다.
        private void OnMatchRequested()
        {
            if (!CanEdit || !_isManual)
            {
                return;
            }

            _editor.Reset(_currentRad);
            SendTargets();
        }

        private void OnKeyboardToggled(bool wanted)
        {
            _isKeyboardWanted = wanted;
        }

        private void OnSaveRequested()
        {
            if (_taughtSection != null)
            {
                _taughtSection.SlotId = _view.SlotId;
                _taughtSection.Save();
            }
        }

        private void OnLoadRequested()
        {
            if (_taughtSection != null && CanEdit)
            {
                _taughtSection.SlotId = _view.SlotId;
                _taughtSection.Load();
            }
        }

        private void OnRemoveRequested()
        {
            if (_taughtSection != null)
            {
                _taughtSection.SlotId = _view.SlotId;
                _taughtSection.Remove();
            }
        }
    }
}
