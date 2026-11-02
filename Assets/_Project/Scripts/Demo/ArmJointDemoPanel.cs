using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 관절을 하나씩 손으로 움직여 보는 패널. 관절마다 슬라이더와 -/+ 버튼이 있고, 시작 자세로 보내거나 지금 자세에 맞출 수 있다.
    /// 어느 각도에서 물건에 닿는지, 어느 관절이 한계에 붙는지를 눈으로 찾는 데 쓴다.
    /// 로봇은 IArmState(읽기)와 IArmDriver(쓰기)로만 다루므로 FR5, UR3 어느 쪽에도 쓴다. 조작할 로봇을 지정하면 그 로봇만, 아니면 버튼으로 바꿔 가며 조작한다.
    /// "수동 제어"를 켠 동안만 목표를 보낸다. 끄면 현재 각도를 보여 주기만 한다.
    /// 수동 제어를 켜면 로봇의 자동 목표(IK)를 막는다(IManualHold). 남은 IK 목표를 지우고 새 목표도 받지 않으므로 하던 자동 키팅은
    /// 정리되어 끝나고 쉬며, 팔은 그 자리에 남아 사람이 관절을 맞출 수 있다. 끄면 자동 키팅이 다시 이어진다.
    /// 로봇이 굴러가는 베이스(ArmA)에 실려 있으면 키보드로 베이스를 움직이고 자리를 Console에 적는다(ArmBaseControlSection).
    /// 베이스 키보드 조작은 수동 제어 켜기와 상관없이 키를 누를 때만 움직이므로, 자동 키팅 중에는 키를 누르지 않는다.
    /// </summary>
    public sealed class ArmJointDemoPanel : DemoPanel
    {
        private const float RobotButtonWidth = 90f;

        [Tooltip("조작할 로봇(IArmState와 IArmDriver를 구현한 컴포넌트, 보통 ArmController). 지정하면 이 로봇만 조작하고 다른 로봇은 고르지 않는다. 비워 두면 씬에서 찾은 모든 로봇을 버튼으로 바꿔 가며 조작한다.")]
        [SerializeField] private MonoBehaviour _arm;

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

        public ArmJointDemoPanel()
            : base("Arm Joints", DemoPanelAnchor.BottomRight, new Vector2(10f, 10f), new Vector2(380f, 460f))
        {
        }

        private void Awake()
        {
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

        private void Update()
        {
            _baseSection?.Update(Time.unscaledDeltaTime);
        }

        // 패널이 꺼지거나 사라질 때 수동 제어로 막아 둔 자동 목표를 풀어, 로봇이 영영 자동 동작을 못 받는 일이 없게 한다.
        private void OnDisable()
        {
            ReleaseManual();
        }

        protected override void DrawContents()
        {
            DrawRobotSelector();

            if (!_state.IsReady)
            {
                GUILayout.Label("로봇이 준비되는 중...");
                return;
            }

            EnsureEditor();
            _state.GetJointPositions(_currentRad);

            bool wantManual = GUILayout.Toggle(_isManual, "수동 제어 (켜면 자동 키팅이 멈추고 팔이 그 자리에 남는다)");
            if (wantManual != _isManual)
            {
                SetManual(wantManual);
            }

            bool isEdited = DrawJointRows();
            if (isEdited)
            {
                SendTargets();
            }

            DrawShortcuts();
            _baseSection?.Draw();
            DrawToolPose();
            _taughtSection?.Draw();
        }

        private void DrawRobotSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"로봇: {_robots.Current.name}");
            if (_robots.HasChoice && GUILayout.Button("다음 로봇", GUILayout.Width(RobotButtonWidth)))
            {
                SelectRobot(_robots.SelectNext());
            }

            GUILayout.EndHorizontal();
        }

        // 관절 한 줄: 이름, -, 슬라이더, +, 값. 수동 제어가 꺼져 있으면 현재 각도만 보여 주고 만질 수 없다.
        private bool DrawJointRows()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && _isManual;

            bool isEdited = false;
            for (int i = 0; i < _editor.Count; i++)
            {
                float currentDeg = _currentRad[i] * Mathf.Rad2Deg;
                float shownDeg = _isManual ? _editor.TargetDeg(i) : currentDeg;
                string valueText = $"{shownDeg:0.0}° ({currentDeg:0.0}°)";

                if (DemoSliderRow.Draw(_state.JointNames[i], shownDeg, _editor.LowerDeg(i), _editor.UpperDeg(i), _nudgeDeg, valueText, _isManual, out float editedDeg))
                {
                    _editor.SetTargetDeg(i, editedDeg);
                    isEdited = true;
                }
            }

            GUI.enabled = wasEnabled;
            return isEdited;
        }

        private void DrawShortcuts()
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && _isManual;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("시작 자세로"))
            {
                _editor.Reset(_driver.HomePoseRad);
                SendTargets();
            }

            // 슬라이더로 정한 목표를 버리고 목표를 지금 각도로 되돌린다. 슬라이더 값을 적용하는 버튼이 아니다.
            if (GUILayout.Button("지금 자세에 맞춤"))
            {
                _editor.Reset(_currentRad);
                SendTargets();
            }

            GUILayout.EndHorizontal();
            GUI.enabled = wasEnabled;
        }

        private void DrawToolPose()
        {
            Pose tool = _state.ToolPose;
            Vector3 position = tool.position;
            GUILayout.Label($"도구 끝점(월드): x {position.x:0.00}  y {position.y:0.00}  z {position.z:0.00} m");

            // 접근 방향(+Z)과 월드 오일러각. 칸 안으로 들어가는 방향이 얼마나 기울었는지, 손목을 얼마나 돌렸는지 읽는 데 쓴다.
            Vector3 approach = tool.rotation * Vector3.forward;
            Vector3 euler = tool.rotation.eulerAngles;
            GUILayout.Label($"도구 접근 방향(+Z): x {approach.x:+0.00;-0.00}  y {approach.y:+0.00;-0.00}  z {approach.z:+0.00;-0.00}");
            GUILayout.Label($"도구 방향(월드 각): x {Mathf.DeltaAngle(0f, euler.x):0}°  y {Mathf.DeltaAngle(0f, euler.y):0}°  z {Mathf.DeltaAngle(0f, euler.z):0}°");
            GUILayout.Label("값: 목표° (현재°)");
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

            // 로봇마다 관절 수와 한계가 다르므로 다음 그리기에서 다시 만든다. 이전 로봇은 손대지 않고 그대로 둔다.
            _editor = null;
            _isManual = false;
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
    }
}
