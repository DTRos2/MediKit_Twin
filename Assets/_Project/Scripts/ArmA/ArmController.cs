using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 프로필에 적힌 관절을 찾아 목표 각도로 구동한다. 목표는 IArmTarget(IK, ROS)이 주거나 SetJointTargets로 직접 준다.
    /// 목표까지는 프로필의 최대 각속도로 접근하므로 ROS도 최종 각도만 보내면 된다.
    /// 로봇 최상위 오브젝트에 붙인다. 로봇 종류를 모르고 ArmProfileSO만 읽는다.
    /// </summary>
    // 다른 컴포넌트(IK, 그리퍼, 강화학습 환경)가 Start에서 JointNames와 Chain을 읽으므로 먼저 초기화한다.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(ArmDriveSetup))]
    public class ArmController : MonoBehaviour, IArmState, IArmDriver, IForwardKinematics
    {
        [SerializeField] private ArmProfileSO _profile;

        [Tooltip("관절 목표를 주는 쪽(IArmTarget 구현체: IkArmTarget, RosArmTarget). 비우면 SetJointTargets로만 구동한다.")]
        [SerializeField] private MonoBehaviour _targetSource;

        [SerializeField] private bool _drawToolGizmo = true;

        private ArticulationBody[] _joints;
        private string[] _jointNames;
        private float[] _command;
        private float[] _goal;
        private float[] _lowerRad;
        private float[] _upperRad;
        private Transform _baseLink;
        private Transform _toolLink;
        private IArmTarget _target;
        private KinematicChain _chain;
        private bool _ready;

        public IReadOnlyList<string> JointNames => _jointNames;
        public int JointCount => _joints.Length;
        public ArmProfileSO Profile => _profile;
        public KinematicChain Chain => _chain;
        public Transform BaseLink => _baseLink;
        public ArticulationBody ToolBody => _toolLink != null ? _toolLink.GetComponent<ArticulationBody>() : null;
        public float MaxJointSpeedRadPerSec => _profile.MaxJointSpeedRadPerSec;

        public Pose ToolPose => new Pose(
            _toolLink.TransformPoint(_profile.ToolPositionOffset),
            _toolLink.rotation * _profile.ToolRotationOffset);

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

            if (!ResolveLinks())
            {
                enabled = false;
                return;
            }

            ReadLimits();
            _chain = KinematicChainBuilder.Build(_baseLink, _joints, _toolLink, _profile.ToolPositionOffset, _profile.ToolRotationOffset);

            _command = new float[_joints.Length];
            _goal = new float[_joints.Length];
            GetJointPositions(_command);
            System.Array.Copy(_command, _goal, _command.Length);
            _ready = true;
        }

        private void FixedUpdate()
        {
            if (!_ready)
            {
                return;
            }

            // 목표가 없으면 직전 목표를 그대로 유지한다.
            _target?.TryGetTargets(_goal);

            float maxStep = MaxJointSpeedRadPerSec * Time.fixedDeltaTime;
            for (int i = 0; i < _joints.Length; i++)
            {
                float goal = Mathf.Clamp(_goal[i], _lowerRad[i], _upperRad[i]);
                _command[i] = Mathf.MoveTowards(_command[i], goal, maxStep);

                ArticulationDrive drive = _joints[i].xDrive;
                drive.target = _command[i] * Mathf.Rad2Deg;
                _joints[i].xDrive = drive;
            }
        }

        public void SetJointTargets(float[] anglesRad)
        {
            System.Array.Copy(anglesRad, _goal, _goal.Length);
        }

        public Pose ComputeToolPose(float[] anglesRad)
        {
            Pose inBase = _chain.Forward(anglesRad);
            return new Pose(
                _baseLink.TransformPoint(inBase.position),
                _baseLink.rotation * inBase.rotation);
        }

        public void ResetToPose(float[] anglesRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                float angle = Mathf.Clamp(anglesRad[i], _lowerRad[i], _upperRad[i]);
                _joints[i].jointPosition = new ArticulationReducedSpace(angle);
                _joints[i].jointVelocity = new ArticulationReducedSpace(0f);

                ArticulationDrive drive = _joints[i].xDrive;
                drive.target = angle * Mathf.Rad2Deg;
                _joints[i].xDrive = drive;

                _command[i] = angle;
                _goal[i] = angle;
            }
        }

        public bool IsAtTarget(float toleranceRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                if (Mathf.Abs(_joints[i].jointPosition[0] - _goal[i]) > toleranceRad)
                {
                    return false;
                }
            }

            return true;
        }

        public void GetJointPositions(float[] anglesRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                anglesRad[i] = _joints[i].jointPosition[0];
            }
        }

        public void GetJointVelocities(float[] velocitiesRadPerSec)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                velocitiesRadPerSec[i] = _joints[i].jointVelocity[0];
            }
        }

        public void GetJointLimits(float[] lowerRad, float[] upperRad)
        {
            System.Array.Copy(_lowerRad, lowerRad, _lowerRad.Length);
            System.Array.Copy(_upperRad, upperRad, _upperRad.Length);
        }

        private bool ResolveLinks()
        {
            _baseLink = TransformSearch.FindDeep(transform, _profile.BaseLinkName);
            _toolLink = TransformSearch.FindDeep(transform, _profile.ToolLinkName);
            if (_baseLink == null || _toolLink == null)
            {
                Debug.LogError($"{name}: 링크를 찾지 못했다. base='{_profile.BaseLinkName}', tool='{_profile.ToolLinkName}'", this);
                return false;
            }

            ArmProfileSO.JointEntry[] entries = _profile.Joints;
            _joints = new ArticulationBody[entries.Length];
            _jointNames = new string[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                Transform link = TransformSearch.FindDeep(transform, entries[i].LinkName);
                _joints[i] = link != null ? link.GetComponent<ArticulationBody>() : null;
                _jointNames[i] = entries[i].JointName;

                if (_joints[i] == null)
                {
                    Debug.LogError($"{name}: 관절 링크 '{entries[i].LinkName}'(관절 {entries[i].JointName})의 ArticulationBody를 찾지 못했다.", this);
                    return false;
                }
            }

            return true;
        }

        private void ReadLimits()
        {
            _lowerRad = new float[_joints.Length];
            _upperRad = new float[_joints.Length];
            for (int i = 0; i < _joints.Length; i++)
            {
                bool limited = _joints[i].twistLock == ArticulationDofLock.LimitedMotion;
                _lowerRad[i] = limited ? _joints[i].xDrive.lowerLimit * Mathf.Deg2Rad : -Mathf.PI;
                _upperRad[i] = limited ? _joints[i].xDrive.upperLimit * Mathf.Deg2Rad : Mathf.PI;
            }
        }

        // TCP 축이 맞는지 눈으로 확인한다. 파랑(+Z)이 손가락 쪽 접근 방향이어야 한다.
        private void OnDrawGizmos()
        {
            if (!_drawToolGizmo || !_ready)
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
