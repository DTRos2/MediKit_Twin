using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암 관절을 하나씩 손으로 움직여 보는 패널. 관절마다 슬라이더와 -/+ 버튼이 있고, 시작 자세로 보내거나 지금 자세에 맞출 수 있다.
    /// 어느 각도에서 물건에 닿는지, 어느 관절이 한계에 붙는지를 눈으로 찾는 데 쓴다.
    /// 로봇은 IArmState(읽기)와 IArmDriver(쓰기)로만 다루므로 FR5, UR3 어느 쪽에도 쓴다. 조작할 로봇을 지정하면 그 로봇만, 아니면 버튼으로 바꿔 가며 조작한다.
    /// "수동 제어"를 켠 동안만 목표를 보낸다. 끄면 현재 각도를 보여 주기만 해서 자동 키팅과 겨루지 않는다.
    /// 수동 제어를 켤 때 도구 끝점 목표(IK)를 지운다. 남아 있으면 IK가 매 프레임 관절 목표를 덮어쓰기 때문이다.
    /// </summary>
    public sealed class ArmJointDemoPanel : DemoPanel
    {
        private const float NameWidth = 100f;
        private const float NudgeButtonWidth = 28f;
        private const float ValueWidth = 110f;
        private const float RobotButtonWidth = 90f;

        [Tooltip("조작할 로봇(IArmState와 IArmDriver를 구현한 컴포넌트, 보통 ArmController). 지정하면 이 로봇만 조작하고 다른 로봇은 고르지 않는다. 비워 두면 씬에서 찾은 모든 로봇을 버튼으로 바꿔 가며 조작한다.")]
        [SerializeField] private MonoBehaviour _arm;

        [Tooltip("-/+ 버튼을 한 번 누를 때 움직이는 각도(도).")]
        [SerializeField, Min(0.1f)] private float _nudgeDeg = 5f;

        private CandidateSelection<MonoBehaviour> _robots;

        private IArmState _state;
        private IArmDriver _driver;
        private IToolPoseTarget _tool;

        private JointTargetEditor _editor;
        private float[] _currentRad;
        private float[] _targetRad;
        private bool _isManual;

        public ArmJointDemoPanel()
            : base("Arm Joints", DemoPanelAnchor.BottomRight, new Vector2(10f, 10f), new Vector2(380f, 330f))
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

            SelectRobot(_robots.Current);
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

            bool wantManual = GUILayout.Toggle(_isManual, "수동 제어 (켜면 자동 키팅과 겨룬다)");
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
            DrawToolPose();
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

                GUILayout.BeginHorizontal();
                GUILayout.Label(_state.JointNames[i], GUILayout.Width(NameWidth));

                if (GUILayout.Button("-", GUILayout.Width(NudgeButtonWidth)))
                {
                    _editor.NudgeDeg(i, -_nudgeDeg);
                    isEdited = true;
                }

                float sliderDeg = GUILayout.HorizontalSlider(shownDeg, _editor.LowerDeg(i), _editor.UpperDeg(i));
                if (_isManual && !Mathf.Approximately(sliderDeg, shownDeg))
                {
                    _editor.SetTargetDeg(i, sliderDeg);
                    isEdited = true;
                }

                if (GUILayout.Button("+", GUILayout.Width(NudgeButtonWidth)))
                {
                    _editor.NudgeDeg(i, _nudgeDeg);
                    isEdited = true;
                }

                GUILayout.Label($"{shownDeg:0.0}° ({currentDeg:0.0}°)", GUILayout.Width(ValueWidth));
                GUILayout.EndHorizontal();
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
            Vector3 position = _state.ToolPose.position;
            GUILayout.Label($"도구 끝점(월드): x {position.x:0.00}  y {position.y:0.00}  z {position.z:0.00} m");
            GUILayout.Label("값: 목표° (현재°)");
        }

        private void SelectRobot(MonoBehaviour robot)
        {
            _state = (IArmState)robot;
            _driver = (IArmDriver)robot;
            _tool = robot.GetComponent<IToolPoseTarget>();

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

        // 켜는 순간 목표를 지금 자세에 맞춰 팔이 튀지 않게 하고, 남은 IK 목표를 지워 관절 목표를 덮어쓰지 못하게 한다.
        private void SetManual(bool isManual)
        {
            _isManual = isManual;
            if (!isManual)
            {
                return;
            }

            _tool?.ClearToolTarget();
            _editor.Reset(_currentRad);
            SendTargets();
        }

        private void SendTargets()
        {
            _editor.CopyTargetsTo(_targetRad);
            _driver.SetJointTargets(_targetRad);
        }
    }
}
