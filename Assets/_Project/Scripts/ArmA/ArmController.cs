using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암 한 대를 조립하는 컴포넌트. 실제 일은 다른 클래스에 맡기고 순서만 정한다.
    ///   링크 찾기 = ArmRig, 관절 구동 = JointBank, 기준 보정 = ReferenceOffsetCalibrator,
    ///   순기구학 = KinematicChainBuilder, 시작 자세 선택 = HomePoseSelector.
    /// 밖으로 내보내는 관절 각도는 항상 URDF 기준(rad)이다. 로봇 종류를 모르고 ArmProfileSO만 읽는다.
    /// 첫 물리 스텝 뒤에 초기화한다(그 전에는 관절 각도를 믿을 수 없다).
    /// </summary>
    // 다른 컴포넌트(IK, 그리퍼, 강화학습 환경)가 Start에서 JointNames를 읽으므로 먼저 초기화한다.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ArmDriveSetup))]
    public class ArmController : MonoBehaviour, IArmModel, IArmDriver, IForwardKinematics, IGripperHost, IArmBaseLocator
    {
        // 물리 엔진이 관절 상태를 안정적으로 돌려주기까지 기다리는 FixedUpdate 횟수.
        private const int WarmupFixedSteps = 2;

        [SerializeField] private ArmProfileSO _profile;

        [Tooltip("관절 목표를 주는 쪽(IArmTarget 구현체: IkArmTarget, RosArmTarget). 비우면 SetJointTargets로만 구동한다.")]
        [SerializeField] private MonoBehaviour _targetSource;

        [Tooltip("시작할 때 프로필의 시작 자세로 옮긴다. 끄면 링크가 놓인 자세 그대로 시작한다.")]
        [SerializeField] private bool _applyHomePoseOnStart = true;

        [Tooltip("Tools > Arm 메뉴가 편집 모드에서 링크를 돌려 둔 자세(URDF 기준, rad). 직접 고치지 않는다.")]
        [SerializeField] private float[] _editorPoseRad = Array.Empty<float>();

        [SerializeField] private bool _drawToolGizmo = true;

        private ArmRig _rig;
        private JointBank _bank;
        private string[] _jointNames;
        private float[] _pendingPose;
        private string _pendingPoseCaller;
        private IArmTarget _target;
        private KinematicChain _chain;
        private int _fixedSteps;
        private bool _resolved;

        public bool IsReady { get; private set; }
        public IReadOnlyList<string> JointNames => _jointNames;
        public int JointCount => _jointNames.Length;
        public ArmProfileSO Profile => _profile;
        public KinematicChain Chain => _chain;
        public Transform BaseLink => _rig.Base;
        /// <summary>팔 베이스(로봇 기준 링크)의 월드 위치. 링크를 찾기 전(Start 전)에는 오브젝트 위치를 돌려준다.</summary>
        public Vector3 BaseWorldPosition => _rig != null ? _rig.Base.position : transform.position;

        public DlsIkSettings IkSettings => _profile.Ik;
        public Transform RobotRoot => transform;
        public IReadOnlyList<ArmProfileSO.GripperJointEntry> GripperJoints => _profile.GripperJoints;
        public ArticulationBody ToolBody => _rig.Tool != null ? _rig.Tool.GetComponent<ArticulationBody>() : null;
        public float MaxJointSpeedRadPerSec => _profile.MaxJointSpeedRadPerSec;

        /// <summary>프로필 시작 자세(rad, URDF 기준). 순기구학이 준비되면 부호가 맞는 쪽을 고른다.</summary>
        public float[] HomePoseRad => _chain != null
            ? HomePoseSelector.Choose(_profile, _chain, _rig.Base)
            : _profile.HomePoseRad;

        public Pose ToolPose => new Pose(
            _rig.Tool.TransformPoint(_profile.ToolPositionOffset),
            _rig.Tool.rotation * _profile.ToolRotationOffset);

        private void Start()
        {
            if (_profile == null)
            {
                Debug.LogError($"{name}: ArmProfileSO가 없다.", this);
                enabled = false;
                return;
            }

            if (_targetSource != null && !(_targetSource is IArmTarget))
            {
                Debug.LogError($"{name}: Target Source가 IArmTarget을 구현하지 않는다.", this);
                enabled = false;
                return;
            }

            _target = _targetSource as IArmTarget;

            if (!ArmRig.TryResolve(transform, _profile, out _rig, out string error))
            {
                Debug.LogError($"{name}: {error}", this);
                enabled = false;
                return;
            }

            _jointNames = new string[_rig.Joints.Length];
            for (int i = 0; i < _jointNames.Length; i++)
            {
                _jointNames[i] = _profile.Joints[i].JointName;
            }

            _bank = new JointBank(_rig.Joints);
            _resolved = true;
        }

        private void FixedUpdate()
        {
            if (!_resolved)
            {
                return;
            }

            if (!IsReady)
            {
                _fixedSteps++;
                if (_fixedSteps >= WarmupFixedSteps)
                {
                    Initialize();
                }

                return;
            }

            // 목표가 없으면 직전 목표를 그대로 유지한다.
            _target?.TryGetTargets(_bank.Goal);
            _bank.Step(MaxJointSpeedRadPerSec * Time.fixedDeltaTime);
        }

        // 첫 물리 스텝 뒤에 한 번만 실행한다.
        private void Initialize()
        {
            _bank.SetOffsets(ReferenceOffsetCalibrator.Calibrate(_rig.Joints, _editorPoseRad, out string warning));
            if (warning != null)
            {
                Debug.LogWarning($"{name}: {warning} Tools > Arm > Revert 후 다시 적용한다.", this);
            }

            _bank.SyncToCurrent();

            var current = new float[_bank.Count];
            _bank.ReadAngles(current);
            _chain = KinematicChainBuilder.Build(
                _rig.Base, _rig.Joints, _rig.Tool, _profile.ToolPositionOffset, _profile.ToolRotationOffset, current);

            IsReady = true;

            // 빈 배열은 자세가 아니라 "예약 없음"이다. Enter Play Mode Options로 도메인과 씬을 다시 불러오지 않으면
            // 이전 실행의 빈 예약값이 남을 수 있어, 에러로 알리지 않고 시작 자세로 시작한다.
            if (_pendingPose != null && _pendingPose.Length == 0)
            {
                _pendingPose = null;
            }

            if (_pendingPose != null)
            {
                ApplyPendingPose();
            }
            else if (_applyHomePoseOnStart)
            {
                ResetToPose(HomePoseRad);
            }
        }

        // 준비 전에 예약된 자세를 적용한다. 예약할 때는 관절 수를 알 수 없었을 수 있으므로 길이가 맞지 않으면
        // 예약한 곳을 알리고 시작 자세로 대신한다(팔이 초기화되지 않은 채 남지 않게 한다).
        private void ApplyPendingPose()
        {
            float[] pose = _pendingPose;
            _pendingPose = null;

            if (pose.Length != ExpectedJointCount)
            {
                Debug.LogError(
                    $"{name}: 시작 전에 예약된 자세의 길이({pose.Length})가 관절 수({ExpectedJointCount})와 다르다. 시작 자세로 대신한다. 예약한 곳:\n{_pendingPoseCaller}",
                    this);
                pose = HomePoseRad;
            }

            ResetToPose(pose);
        }

        public void SetJointTargets(float[] anglesRad)
        {
            if (HasValidLength(anglesRad, nameof(SetJointTargets)))
            {
                _bank.SetGoal(anglesRad);
            }
        }

        public void ResetToPose(float[] anglesRad)
        {
            if (!HasValidLength(anglesRad, nameof(ResetToPose)))
            {
                return;
            }

            // 물리 엔진 준비 전에는 값만 기억해 두었다가 Initialize에서 적용한다.
            if (!IsReady)
            {
                _pendingPose = (float[])anglesRad.Clone();
                // 파일 이름과 줄 번호까지 남기기 위해 System.Diagnostics.StackTrace를 쓴다(ExtractStackTrace는 빈 문자열이 나올 수 있다).
                _pendingPoseCaller = new System.Diagnostics.StackTrace(1, true).ToString();
                return;
            }

            _bank.ResetTo(anglesRad);
        }

        public Pose ComputeToolPose(float[] anglesRad)
        {
            Pose inBase = _chain.Forward(anglesRad);
            return new Pose(
                _rig.Base.TransformPoint(inBase.position),
                _rig.Base.rotation * inBase.rotation);
        }

        public bool IsAtTarget(float toleranceRad) => _bank.IsAtGoal(toleranceRad);

        public void GetJointPositions(float[] anglesRad) => _bank.ReadAngles(anglesRad);

        public void GetJointVelocities(float[] velocitiesRadPerSec) => _bank.ReadVelocities(velocitiesRadPerSec);

        public void GetJointLimits(float[] lowerRad, float[] upperRad) => _bank.GetLimits(lowerRad, upperRad);

        // 관절 수는 Start에서 만드는 관절 이름 배열보다 프로필이 먼저 알고 있다. 이름 배열만 보면 Start 전의 호출은 기대 길이가 0이 되어
        // 빈 배열이 검증을 통과해 저장되었다가 Initialize에서야 실패하고, 그러면 스택에 진짜 호출한 곳이 나오지 않는다.
        private int ExpectedJointCount
        {
            get
            {
                if (_profile != null)
                {
                    return _profile.Joints.Length;
                }

                return _jointNames != null ? _jointNames.Length : 0;
            }
        }

        // 호출한 쪽이 관절 수와 다른 길이의 배열을 주면 예외로 죽지 않고, 어디서 호출했는지 스택과 함께 알린다.
        private bool HasValidLength(float[] anglesRad, string caller)
        {
            int expected = ExpectedJointCount;
            if (anglesRad != null && anglesRad.Length == expected)
            {
                return true;
            }

            Debug.LogError(
                $"{name}: {caller}에 준 각도 배열 길이({(anglesRad == null ? "null" : anglesRad.Length.ToString())})가 관절 수({expected})와 다르다. " +
                $"프로필 '{(_profile != null ? _profile.name : "없음")}'의 관절 목록과 호출한 쪽을 확인한다. " +
                $"{StackTraceUtility.ExtractStackTrace()}",
                this);
            return false;
        }

        // TCP 축이 맞는지 눈으로 확인한다. 파랑(+Z)이 손가락 쪽 접근 방향이어야 한다.
        private void OnDrawGizmos()
        {
            if (!_drawToolGizmo || !_resolved)
            {
                return;
            }

            Pose pose = ToolPose;
            const float length = 0.08f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pose.position, pose.position + pose.rotation * Vector3.right * length);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pose.position, pose.position + pose.rotation * Vector3.up * length);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pose.position, pose.position + pose.rotation * Vector3.forward * length);
        }
    }
}
