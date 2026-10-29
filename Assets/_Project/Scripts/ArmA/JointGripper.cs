using System;
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
    public class JointGripper : MonoBehaviour, IGripper
    {
        [SerializeField] private ArmController _arm;

        [Tooltip("완전히 열린 상태에서 닫히기까지(또는 반대) 걸리는 시간.")]
        [SerializeField, Min(0.05f)] private float _travelSeconds = 0.8f;

        [Tooltip("닫힌 뒤 물체가 안정될 때까지 기다리는 시간. 지나면 물체를 쥔 것으로 본다.")]
        [SerializeField, Min(0f)] private float _settleSeconds = 0.2f;

        [Tooltip("TCP 중심에서 이 반경 안의 Graspable을 쥔다.")]
        [SerializeField, Min(0.005f)] private float _graspRadius = 0.06f;

        [SerializeField] private LayerMask _graspMask = ~0;

        private ArticulationBody[] _joints;
        private ArmProfileSO.GripperJointEntry[] _entries;
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

        private void Start()
        {
            if (_arm == null || _arm.Profile == null)
            {
                Debug.LogError($"{name}: ArmController 또는 프로필이 없다.", this);
                enabled = false;
                return;
            }

            _entries = _arm.Profile.GripperJoints;
            _joints = new ArticulationBody[_entries.Length];
            for (int i = 0; i < _entries.Length; i++)
            {
                Transform link = TransformSearch.FindDeep(_arm.transform, _entries[i].LinkName);
                _joints[i] = link != null ? link.GetComponent<ArticulationBody>() : null;
                if (_joints[i] == null)
                {
                    Debug.LogError($"{name}: 그리퍼 관절 링크 '{_entries[i].LinkName}'를 찾지 못했다.", this);
                    enabled = false;
                    return;
                }
            }

            ApplyOpening();
            _ready = true;
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
                float value = Mathf.Lerp(_entries[i].ClosedValue, _entries[i].OpenValue, _opening);
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

            _attachment.Attach(nearest, _arm.ToolBody);
            Grasped?.Invoke();
        }

        private Graspable FindNearestGraspable()
        {
            Vector3 center = _arm.ToolPose.position;
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

            _attachment.Detach();
            Released?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (_arm == null || !_ready)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
            Gizmos.DrawWireSphere(_arm.ToolPose.position, _graspRadius);
        }
    }
}
