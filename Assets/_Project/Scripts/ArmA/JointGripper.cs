using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 프로필의 그리퍼 관절 목록을 개폐 정도(0~1) 하나로 구동한다. 직선 관절 2개(FR5)도,
    /// mimic으로 묶인 회전 관절 6개(UR3의 Robotiq)도 같은 코드로 다룬다. mimic은 Unity가 처리하지 않으므로
    /// 프로필에 관절별 닫힘 값(multiplier를 곱한 값)을 적어 이 클래스가 대신 구동한다.
    /// 닫은 뒤 손가락 사이에 Graspable이 있으면 GraspAttachment로 붙여서 쥔 것으로 처리한다.
    /// </summary>
    public class JointGripper : MonoBehaviour, IGripper, IGripContact
    {
        [Tooltip("그리퍼가 붙은 로봇(IGripperHost 구현체, 보통 ArmController).")]
        [SerializeField] private MonoBehaviour _arm;

        [Tooltip("완전히 열린 상태에서 닫히기까지(또는 반대) 걸리는 시간.")]
        [SerializeField, Min(0.05f)] private float _travelSeconds = 0.8f;

        [Tooltip("닫힌 뒤 물체가 안정될 때까지 기다리는 시간. 지나면 물체를 쥔 것으로 본다.")]
        [SerializeField, Min(0f)] private float _settleSeconds = 0.2f;

        [Tooltip("TCP 중심에서 이 반경 안의 Graspable을 쥔다.")]
        [SerializeField, Min(0.005f)] private float _graspRadius = 0.06f;

        [SerializeField] private LayerMask _graspMask = ~0;

        [Tooltip("손가락이 열린 값에서 닫힌 값까지 이 비율 이상 닫혔으면 끝까지 닫힌 것으로 본다. 이보다 덜 닫힌 채 멈췄으면 손가락 사이에 물체가 걸린 것이다.")]
        [SerializeField, Range(0.5f, 0.99f)] private float _fullyClosedRatio = 0.85f;

        [Tooltip("물건을 쥔 뒤 손가락이 쥔 자리에서 닫히는 쪽으로 더 가려는 양(열린 값에서 닫힌 값까지 거리의 비율). " +
                 "0이면 물건에 닿은 자리를 지키기만 한다. 쥔 뒤에는 물건과 손가락의 충돌이 꺼져 있어서 0보다 크면 그만큼 손가락이 물건에 파고든 모습이 된다. " +
                 "쥔 뒤에도 닫힌 값을 계속 향하면 얇은 물건을 뚫고 손가락이 끝까지 닫힌다.")]
        [SerializeField, Range(0f, 0.2f)] private float _holdPreloadRatio = 0f;

        [Tooltip("물건을 쥐고 있는 동안 손가락 구동의 강성, 감쇠, 힘 한계에 곱하는 배수. 1이면 그대로다. " +
                 "기본 구동으로는 쥔 직후 손가락이 목표보다 4~8mm 더 닫혀 얇은 물건에 파고들어서, 쥔 자리에서 밀리지 않게 단단히 잡는다.")]
        [SerializeField, Range(1f, 50f)] private float _holdDriveMultiplier = 10f;

        private IGripperHost _host;
        private ArticulationBody[] _joints;
        private IReadOnlyList<ArmProfileSO.GripperJointEntry> _entries;
        private float[] _positions;
        private float[] _openValues;
        private float[] _closedValues;
        private Collider[] _fingerColliders = Array.Empty<Collider>();
        private GripClosureJudge _closureJudge;
        private GripHoldTargets _holdPlanner;
        // 물건을 쥐고 있는 동안 손가락 관절이 향할 목표. 쥐고 있지 않으면 null이라 개폐 정도(_opening)를 따른다.
        private float[] _holdTargets;
        // 쥐기 전의 손가락 구동 값. 쥐는 동안 키운 구동을 놓을 때 되돌리는 데 쓴다.
        private ArticulationDrive[] _normalDrives;
        private readonly GraspAttachment _attachment = new GraspAttachment();
        private float _opening = 1f;
        private float _targetOpening = 1f;
        private float _settleTimer;
        private bool _ready;

        public event Action Grasped;
        public event Action Released;

        public float Opening => _opening;
        public bool IsHolding => _attachment.IsAttached;
        public Graspable HeldObject => _attachment.Held;

        public IReadOnlyList<Collider> FingerColliders => _fingerColliders;

        public Vector3 GraspCenter => _host != null ? _host.ToolPose.position : transform.position;

        public float GraspRadius => _graspRadius;

        /// <summary>손가락이 열린 값에서 닫힌 값까지 충분히 닫히지 못하고 멈췄으면 true. 닫는 명령을 내리고 기다린 뒤에 읽는다.</summary>
        public bool IsBlockedByObject
        {
            get
            {
                if (!_ready)
                {
                    return false;
                }

                ReadJointPositions();
                return _closureJudge.IsBlocked(_positions, _openValues, _closedValues);
            }
        }

        private void Start()
        {
            _host = _arm as IGripperHost;
            if (_host == null)
            {
                Debug.LogError($"{name}: Arm이 IGripperHost를 구현해야 한다.", this);
                enabled = false;
                return;
            }

            _entries = _host.GripperJoints;
            _joints = new ArticulationBody[_entries.Count];
            for (int i = 0; i < _entries.Count; i++)
            {
                Transform link = TransformSearch.FindDeep(_host.RobotRoot, _entries[i].LinkName);
                _joints[i] = link != null ? link.GetComponent<ArticulationBody>() : null;
                if (_joints[i] == null)
                {
                    Debug.LogError($"{name}: 그리퍼 관절 링크 '{_entries[i].LinkName}'를 찾지 못했다.", this);
                    enabled = false;
                    return;
                }
            }

            CacheFingerState();
            ApplyOpening();
            _ready = true;
        }

        // 손가락 판정에 쓰는 값(열린·닫힌 값)과 손가락 충돌체를 한 번만 모아 둔다.
        private void CacheFingerState()
        {
            _positions = new float[_joints.Length];
            _openValues = new float[_joints.Length];
            _closedValues = new float[_joints.Length];
            var colliders = new List<Collider>();

            for (int i = 0; i < _joints.Length; i++)
            {
                _openValues[i] = _entries[i].OpenValue;
                _closedValues[i] = _entries[i].ClosedValue;
                colliders.AddRange(_joints[i].GetComponentsInChildren<Collider>());
            }

            _fingerColliders = colliders.ToArray();
            _closureJudge = new GripClosureJudge(_fullyClosedRatio);
            _holdPlanner = new GripHoldTargets(_holdPreloadRatio);
        }

        // 쥐고 있는 동안 손가락 구동을 단단히 한다. 원래 값은 놓을 때 되돌리려고 모아 둔다.
        private void StrengthenDrives()
        {
            _normalDrives = new ArticulationDrive[_joints.Length];
            for (int i = 0; i < _joints.Length; i++)
            {
                ArticulationDrive drive = _joints[i].xDrive;
                _normalDrives[i] = drive;
                drive.stiffness *= _holdDriveMultiplier;
                drive.damping *= _holdDriveMultiplier;
                drive.forceLimit *= _holdDriveMultiplier;
                _joints[i].xDrive = drive;
            }
        }

        // 쥐기 전의 강성, 감쇠, 힘 한계로 되돌린다. 목표는 개폐 정도가 다시 정한다.
        private void RestoreDrives()
        {
            if (_normalDrives == null)
            {
                return;
            }

            for (int i = 0; i < _joints.Length; i++)
            {
                ArticulationDrive drive = _joints[i].xDrive;
                drive.stiffness = _normalDrives[i].stiffness;
                drive.damping = _normalDrives[i].damping;
                drive.forceLimit = _normalDrives[i].forceLimit;
                _joints[i].xDrive = drive;
            }

            _normalDrives = null;
        }

        // 손가락 관절의 지금 위치를 프로필 값과 같은 단위(직선은 m, 회전은 rad)로 _positions에 읽는다.
        private void ReadJointPositions()
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                _positions[i] = _joints[i].jointPosition[0];
            }
        }

        private void FixedUpdate()
        {
            if (!_ready)
            {
                return;
            }

            _opening = Mathf.MoveTowards(_opening, _targetOpening, Time.fixedDeltaTime / _travelSeconds);
            ApplyOpening();
            UpdateGrasp();
        }

        public void SetOpening(float opening)
        {
            _targetOpening = Mathf.Clamp01(opening);
            if (_targetOpening > 0f)
            {
                ReleaseHeld();
            }
        }

        public void Open()
        {
            SetOpening(1f);
        }

        public void Close()
        {
            SetOpening(0f);
        }

        private void ApplyOpening()
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                float value = _holdTargets != null
                    ? _holdTargets[i]
                    : Mathf.Lerp(_entries[i].ClosedValue, _entries[i].OpenValue, _opening);
                bool revolute = _joints[i].jointType == ArticulationJointType.RevoluteJoint;

                ArticulationDrive drive = _joints[i].xDrive;
                drive.target = revolute ? value * Mathf.Rad2Deg : value;
                _joints[i].xDrive = drive;
            }
        }

        // 명령이 완전히 닫힌 뒤 잠깐 기다렸다가 손가락 사이의 물체를 붙인다. 물체가 있으면 손가락은 완전히 닫히지 못하고 멈춘다.
        private void UpdateGrasp()
        {
            bool closedCommand = _targetOpening <= 0f && _opening <= 0f;
            if (!closedCommand || IsHolding)
            {
                _settleTimer = 0f;
                return;
            }

            _settleTimer += Time.fixedDeltaTime;
            if (_settleTimer >= _settleSeconds)
            {
                TryGrasp();
            }
        }

        private void TryGrasp()
        {
            Graspable nearest = FindNearestGraspable();
            if (nearest == null)
            {
                return;
            }

            _attachment.Attach(nearest, _host.ToolBody);

            // 닫는 명령은 끝까지 닫힌 값을 향해 와 있으므로, 그대로 두면 물건이 풀리는 순간 손가락이 한계 힘으로 물건을 뚫는다.
            // 손가락이 물건에 닿아 멈춘 자리를 목표로 바꿔 그 자리를 지키며 누르게 한다.
            ReadJointPositions();
            _holdTargets = _holdPlanner.Compute(_positions, _openValues, _closedValues);

            // 구동에 바로 반영한다. 다음 FixedUpdate까지 기다리면 그 사이 물리가 한 번 돌고, 그동안 손가락은 옛 목표(끝까지 닫힘)로 밀려 튀어 나간다.
            StrengthenDrives();
            ApplyOpening();
            Grasped?.Invoke();
        }

        private Graspable FindNearestGraspable()
        {
            Vector3 center = _host.ToolPose.position;
            Collider[] hits = Physics.OverlapSphere(center, _graspRadius, _graspMask, QueryTriggerInteraction.Ignore);

            Graspable nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Collider hit in hits)
            {
                Graspable candidate = hit.GetComponentInParent<Graspable>();
                if (candidate == null || candidate.IsHeld)
                {
                    continue;
                }

                float distance = (candidate.transform.position - center).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void ReleaseHeld()
        {
            if (!IsHolding)
            {
                return;
            }

            // 여는 동작을 손가락이 있는 자리에서 시작한다. 닫힌 쪽 목표에서 시작하면 열리기 전에 한 번 더 세게 쥐게 된다.
            ReadJointPositions();
            _opening = GripHoldTargets.CurrentOpening(_positions, _openValues, _closedValues);
            _holdTargets = null;
            RestoreDrives();

            _attachment.Detach();
            Released?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (_host == null || !_ready)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawWireSphere(_host.ToolPose.position, _graspRadius);
        }
    }
}
