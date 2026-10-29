using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 프로필에 적힌 관절을 찾아 목표 각도로 구동한다. 목표는 IArmTarget(IK, ROS)이 주거나 SetJointTargets로 직접 준다.
    /// 목표까지는 프로필의 최대 각속도로 접근하므로 ROS도 최종 각도만 보내면 된다.
    /// 밖으로 내보내는 관절 각도는 항상 URDF 기준이다. 에디터에서 링크를 돌려 둔 로봇은 Unity가 그 자세를 0도 기준으로
    /// 잡을 수도, 이미 돌아간 자세로 읽을 수도 있어서, 첫 물리 스텝 뒤에 어느 쪽인지 판별해 기준 차이(offset)를 보정한다.
    /// 로봇 종류를 모르고 ArmProfileSO만 읽는다.
    /// </summary>
    // 다른 컴포넌트(IK, 그리퍼, 강화학습 환경)가 Start에서 JointNames를 읽으므로 먼저 초기화한다.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ArmDriveSetup))]
    public class ArmController : MonoBehaviour, IArmState, IArmDriver, IForwardKinematics
    {
        // 물리 엔진이 관절 상태를 안정적으로 돌려주기까지 기다리는 FixedUpdate 횟수.
        private const int WarmupFixedSteps = 2;
        private const float ReferenceToleranceRad = 0.05f;

        [SerializeField] private ArmProfileSO _profile;

        [Tooltip("관절 목표를 주는 쪽(IArmTarget 구현체: IkArmTarget, RosArmTarget). 비우면 SetJointTargets로만 구동한다.")]
        [SerializeField] private MonoBehaviour _targetSource;

        [Tooltip("시작할 때 프로필의 시작 자세로 옮긴다. 끄면 링크가 놓인 자세 그대로 시작한다.")]
        [SerializeField] private bool _applyHomePoseOnStart = true;

        [Tooltip("Tools > Arm 메뉴가 편집 모드에서 링크를 돌려 둔 자세(URDF 기준, rad). 직접 고치지 않는다.")]
        [SerializeField] private float[] _editorPoseRad = Array.Empty<float>();

        [SerializeField] private bool _drawToolGizmo = true;

        private ArmRig _rig;
        private string[] _jointNames;
        private float[] _command;
        private float[] _goal;
        private float[] _lowerRad;
        private float[] _upperRad;
        private float[] _offsetRad;
        private float[] _pendingPose;
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

            int count = _rig.Joints.Length;
            _jointNames = new string[count];
            for (int i = 0; i < count; i++)
            {
                _jointNames[i] = _profile.Joints[i].JointName;
            }

            _command = new float[count];
            _goal = new float[count];
            _lowerRad = new float[count];
            _upperRad = new float[count];
            _offsetRad = new float[count];
            ReadLimits();
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
            _target?.TryGetTargets(_goal);

            float maxStep = MaxJointSpeedRadPerSec * Time.fixedDeltaTime;
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                float goal = Mathf.Clamp(_goal[i], _lowerRad[i], _upperRad[i]);
                _command[i] = Mathf.MoveTowards(_command[i], goal, maxStep);
                ApplyDriveTarget(i, _command[i]);
            }
        }

        // 첫 물리 스텝 뒤에 한 번만 실행한다. 그 전에는 관절 각도를 믿을 수 없다.
        private void Initialize()
        {
            DetectReferenceOffset();

            var urdfAngles = new float[_rig.Joints.Length];
            ReadUrdfAngles(urdfAngles);

            _chain = KinematicChainBuilder.Build(
                _rig.Base, _rig.Joints, _rig.Tool, _profile.ToolPositionOffset, _profile.ToolRotationOffset, urdfAngles);

            Array.Copy(urdfAngles, _command, urdfAngles.Length);
            Array.Copy(urdfAngles, _goal, urdfAngles.Length);
            IsReady = true;

            if (_pendingPose != null)
            {
                ResetToPose(_pendingPose);
                _pendingPose = null;
            }
            else if (_applyHomePoseOnStart)
            {
                ResetToPose(HomePoseRad);
            }
        }

        // 편집 모드에서 링크를 돌려 둔 로봇인지 보고, Unity가 그 자세를 0도 기준으로 잡았는지(기준이 밀림) 판별한다.
        private void DetectReferenceOffset()
        {
            if (_editorPoseRad == null || _editorPoseRad.Length != _rig.Joints.Length)
            {
                return;
            }

            var raw = new float[_rig.Joints.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = _rig.Joints[i].jointPosition[0];
            }

            if (MaxAbsDifference(raw, _editorPoseRad) <= ReferenceToleranceRad)
            {
                return; // 돌려 둔 자세를 그대로 URDF 각도로 읽었다. 보정 필요 없음.
            }

            if (MaxAbsDifference(raw, null) <= ReferenceToleranceRad)
            {
                // 돌려 둔 자세가 0도 기준이 되었다. URDF 각도 = 읽은 값 + 돌려 둔 각도.
                Array.Copy(_editorPoseRad, _offsetRad, _offsetRad.Length);
                ShiftDriveLimits();
                return;
            }

            Debug.LogWarning($"{name}: 편집 모드 자세와 첫 관절 각도가 어느 쪽과도 맞지 않아 각도 기준을 보정하지 못했다. Tools > Arm > Revert 후 다시 적용한다.", this);
        }

        // 기준이 밀리면 Unity의 관절 한계도 새 0도 기준으로 해석되므로, URDF 한계에서 offset만큼 빼서 다시 쓴다.
        private void ShiftDriveLimits()
        {
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                ArticulationBody body = _rig.Joints[i];
                if (body.twistLock != ArticulationDofLock.LimitedMotion)
                {
                    continue;
                }

                ArticulationDrive drive = body.xDrive;
                drive.lowerLimit -= _offsetRad[i] * Mathf.Rad2Deg;
                drive.upperLimit -= _offsetRad[i] * Mathf.Rad2Deg;
                body.xDrive = drive;
            }
        }

        public void SetJointTargets(float[] anglesRad)
        {
            Array.Copy(anglesRad, _goal, _goal.Length);
        }

        public Pose ComputeToolPose(float[] anglesRad)
        {
            Pose inBase = _chain.Forward(anglesRad);
            return new Pose(
                _rig.Base.TransformPoint(inBase.position),
                _rig.Base.rotation * inBase.rotation);
        }

        public void ResetToPose(float[] anglesRad)
        {
            // 물리 엔진 준비 전에는 값만 기억해 두었다가 Initialize에서 적용한다.
            if (!IsReady)
            {
                _pendingPose = (float[])anglesRad.Clone();
                return;
            }

            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                float urdfAngle = Mathf.Clamp(anglesRad[i], _lowerRad[i], _upperRad[i]);
                float raw = urdfAngle - _offsetRad[i];

                _rig.Joints[i].jointPosition = new ArticulationReducedSpace(raw);
                _rig.Joints[i].jointVelocity = new ArticulationReducedSpace(0f);
                ApplyDriveTarget(i, urdfAngle);

                _command[i] = urdfAngle;
                _goal[i] = urdfAngle;
            }
        }

        public bool IsAtTarget(float toleranceRad)
        {
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                float current = _rig.Joints[i].jointPosition[0] + _offsetRad[i];
                if (Mathf.Abs(current - _goal[i]) > toleranceRad)
                {
                    return false;
                }
            }

            return true;
        }

        public void GetJointPositions(float[] anglesRad)
        {
            ReadUrdfAngles(anglesRad);
        }

        public void GetJointVelocities(float[] velocitiesRadPerSec)
        {
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                velocitiesRadPerSec[i] = _rig.Joints[i].jointVelocity[0];
            }
        }

        public void GetJointLimits(float[] lowerRad, float[] upperRad)
        {
            Array.Copy(_lowerRad, lowerRad, _lowerRad.Length);
            Array.Copy(_upperRad, upperRad, _upperRad.Length);
        }

        private void ReadUrdfAngles(float[] urdfAngles)
        {
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                urdfAngles[i] = _rig.Joints[i].jointPosition[0] + _offsetRad[i];
            }
        }

        private void ApplyDriveTarget(int index, float urdfAngle)
        {
            ArticulationDrive drive = _rig.Joints[index].xDrive;
            drive.target = (urdfAngle - _offsetRad[index]) * Mathf.Rad2Deg;
            _rig.Joints[index].xDrive = drive;
        }

        private void ReadLimits()
        {
            for (int i = 0; i < _rig.Joints.Length; i++)
            {
                bool limited = _rig.Joints[i].twistLock == ArticulationDofLock.LimitedMotion;
                _lowerRad[i] = limited ? _rig.Joints[i].xDrive.lowerLimit * Mathf.Deg2Rad : -Mathf.PI;
                _upperRad[i] = limited ? _rig.Joints[i].xDrive.upperLimit * Mathf.Deg2Rad : Mathf.PI;
            }
        }

        // reference가 null이면 0과 비교한다.
        private static float MaxAbsDifference(float[] values, float[] reference)
        {
            float largest = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float difference = Mathf.Abs(values[i] - (reference != null ? reference[i] : 0f));
                largest = Mathf.Max(largest, difference);
            }

            return largest;
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
