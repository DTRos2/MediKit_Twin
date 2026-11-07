using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Pusher
{
    /// <summary>
    /// 푸셔. 불량 판정(InspectionResult)을 받으면 박스가 푸셔 앞에 오기를 기다렸다가, 헤드를 앞으로 밀었다가 되돌려
    /// 컨베이어 위의 키트 박스를 불량 박스(RejectBin)에 떨어뜨린다. (FR-09) 박스는 물리로 떨어지고, 세는 일과 KitRejected는 RejectBin이 한다.
    /// 밀기 직전에 박스에 Rigidbody를 켜고 RejectedKit 표시를 붙인다. 시간은 시뮬레이션 시간이라 배속과 일시정지를 따른다.
    /// 박스는 푸셔 앞의 감지 영역을 물리로 훑어 찾으므로, 박스를 옮기는 컨베이어나 시험용 장치를 직접 참조하지 않는다.
    /// 밀기 방향은 이 오브젝트의 로컬 +X이다. 헤드는 키네마틱 Rigidbody로 움직여 박스를 정확히 민다.
    /// </summary>
    public sealed class Pusher : MonoBehaviour
    {
        private const string HeadName = "PushHead";

        [Header("헤드")]
        [Tooltip("미는 헤드. 비워 두면 자식 PushHead를 쓴다. 키네마틱 Rigidbody가 없으면 붙인다.")]
        [SerializeField] private Transform _head;
        [Tooltip("헤드가 앞으로 가는 거리(m). 박스 중심이 컨베이어 가장자리를 넘을 때까지 밀어야 한다. Tools > MediKit > Measure Pusher Layout으로 잰 값(약 1.05m)이다.")]
        [SerializeField, Min(0.1f)] private float _pushDistance = 1.05f;
        [Tooltip("미는 속도(m/s). 박스가 컨베이어 가장자리를 넘어 불량 박스 벽을 넘으려면 가장자리에서 약 0.7m/s 이상이 필요하다.")]
        [SerializeField, Min(0.1f)] private float _pushSpeed = 1.0f;
        [Tooltip("돌아오는 속도(m/s). 미는 속도와 같거나 조금 빠르게 한다.")]
        [SerializeField, Min(0.1f)] private float _returnSpeed = 1.2f;

        [Header("박스 찾기")]
        [Tooltip("박스가 놓이는 자리의 중심, 이 오브젝트 기준 로컬 좌표(m). 박스 중심이 컨베이어 중심 위에 놓인다. 높이는 박스 중심 높이이다.")]
        [SerializeField] private Vector3 _boxSpotLocal = new Vector3(0.9f, 0.905f, 0f);
        [Tooltip("감지 영역 절반 크기(m). 높이는 컨베이어 윗면과 레일에 닿지 않고 박스에만 닿도록 얇게 둔다.")]
        [SerializeField] private Vector3 _boxSpotHalfExtents = new Vector3(0.15f, 0.04f, 0.2f);
        [Tooltip("박스가 이 거리 안(수평, m)에 오고 가만히 있어야 밀기 시작한다. 컨베이어로 오는 박스가 지나가는 중에 밀지 않기 위해서이다.")]
        [SerializeField, Min(0.01f)] private float _spotTolerance = 0.06f;
        [Tooltip("박스가 이 시간(초) 동안 움직이지 않아야 밀기 시작한다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _settleSeconds = 0.3f;
        [Tooltip("키트 박스로 보는 오브젝트 이름의 앞부분. 감지된 충돌체에서 위로 올라가며 이 이름으로 시작하는 오브젝트를 박스로 본다.")]
        [SerializeField] private string _boxNamePrefix = "KitBox";
        [Tooltip("불량 키트가 푸셔 앞에 오기를 기다리는 최대 시간(초). 넘으면 경고를 남기고 그 키트는 건너뛴다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(1f)] private float _boxWaitSeconds = 30f;

        [Header("밀려난 박스")]
        [Tooltip("밀 때 박스의 질량(kg).")]
        [SerializeField, Min(0.01f)] private float _boxMassKg = 0.3f;
        [Tooltip("민 뒤 불량 박스에 들어오기를 기다리는 시간(초). 넘으면 밖으로 떨어진 것으로 보고 박스를 지운다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(0.5f)] private float _fallSettleSeconds = 5f;

        private readonly Queue<KitData> _pending = new Queue<KitData>();
        private readonly Collider[] _hits = new Collider[16];
        private IStationStateReporter _reporter;
        private RejectBin _bin;
        private Rigidbody _headBody;
        private Vector3 _headRestWorld;
        private Vector3 _pushDirection;
        private bool _running;
        private Transform _trackedBox;
        private Vector3 _trackedPosition;
        private float _stillSince;

        /// <summary>박스가 놓여야 하는 자리의 월드 좌표(박스 중심). 컨베이어나 시험용 장치가 박스를 이 자리로 가져온다.</summary>
        public Vector3 BoxSpotWorld => transform.TransformPoint(_boxSpotLocal);

        private void Awake()
        {
            if (_head == null)
            {
                _head = transform.Find(HeadName);
            }

            if (_head == null)
            {
                Debug.LogError($"{name}: 헤드({HeadName})가 없다.", this);
                enabled = false;
                return;
            }

            _headBody = _head.GetComponent<Rigidbody>();
            if (_headBody == null)
            {
                _headBody = _head.gameObject.AddComponent<Rigidbody>();
            }

            _headBody.isKinematic = true;
            _headBody.useGravity = false;

            _headRestWorld = _head.position;
            _pushDirection = transform.right;
            _reporter = new StationStateReporter(StationId.Pusher);
            _bin = FindFirstObjectByType<RejectBin>();
            if (_bin == null)
            {
                Debug.LogWarning($"{name}: 씬에 RejectBin이 없어 밀려난 박스를 셀 수 없다.", this);
            }
        }

        private void OnEnable()
        {
            CellEvents.InspectionResult += OnInspectionResult;
        }

        private void OnDisable()
        {
            CellEvents.InspectionResult -= OnInspectionResult;

            // 코루틴이 멈추면 처리 중이던 키트는 버리고, 다시 켜졌을 때 새로 시작할 수 있게 한다.
            _pending.Clear();
            _running = false;
        }

        private void Update()
        {
            _reporter.Tick(Time.unscaledTime);
        }

        private void OnInspectionResult(KitData kit)
        {
            if (!kit.Verdict.HasValue || kit.Verdict.Value.IsPass)
            {
                return;
            }

            _pending.Enqueue(kit);
            if (!_running)
            {
                StartCoroutine(Process());
            }
        }

        private IEnumerator Process()
        {
            _running = true;
            while (_pending.Count > 0)
            {
                KitData kit = _pending.Dequeue();
                Transform box = null;
                float deadline = Time.time + _boxWaitSeconds;
                while (box == null && Time.time < deadline)
                {
                    Transform found = FindBox();
                    if (found != null && IsSettled(found))
                    {
                        box = found;
                    }
                    else
                    {
                        yield return null;
                    }
                }

                if (box == null)
                {
                    CellEvents.RaiseWarning("PUSHER_BOX_NOT_FOUND", $"불량 키트가 푸셔 앞에 {_boxWaitSeconds:F0}초 안에 오지 않아 건너뛴다. (로트 {kit.LotNumber})");
                    continue;
                }

                _reporter.Set(StationState.Running);
                RejectedKit marker = PrepareBox(box, kit);

                Vector3 pushEnd = _headRestWorld + _pushDirection * _pushDistance;
                yield return MoveHead(pushEnd, _pushSpeed);
                yield return MoveHead(_headRestWorld, _returnSpeed);

                yield return WaitUntilCounted(marker, kit);
                _reporter.Set(StationState.Idle);
            }

            _running = false;
        }

        // 박스가 불량 박스에 들어왔는지 기다린다. 시간 안에 들어오지 않으면 밖으로 떨어진 것으로 처리하고 박스를 지운다.
        private IEnumerator WaitUntilCounted(RejectedKit marker, KitData kit)
        {
            float until = Time.time + _fallSettleSeconds;
            while (marker != null && !marker.Counted && Time.time < until)
            {
                yield return null;
            }

            if (marker == null || marker.Counted)
            {
                yield break;
            }

            CellEvents.RaiseWarning("PUSHER_KIT_OUTSIDE", $"밀려난 키트가 불량 박스에 들어가지 않았다. 박스를 지우고 누적 불량 수에만 포함한다. (로트 {kit.LotNumber})");
            if (_bin != null)
            {
                _bin.RegisterOutside(marker);
            }
            else
            {
                marker.Counted = true;
                kit.CompleteTime = SimClock.Now;
                CellEvents.RaiseKitRejected(kit);
            }

            Destroy(marker.gameObject);
        }

        // 헤드를 목표까지 일정한 속도로 옮긴다. 물리 스텝마다 키네마틱 Rigidbody로 움직여 박스를 밀 수 있게 한다.
        private IEnumerator MoveHead(Vector3 target, float speed)
        {
            Vector3 position = _headBody.position;
            while (position != target)
            {
                position = Vector3.MoveTowards(position, target, speed * Time.fixedDeltaTime);
                _headBody.MovePosition(position);
                yield return new WaitForFixedUpdate();
            }
        }

        // 박스가 자리 가까이에서 가만히 있는지 본다. 컨베이어로 오는 박스가 지나가는 중에는 false이다.
        private bool IsSettled(Transform box)
        {
            Vector3 position = box.position;
            Vector3 offset = position - BoxSpotWorld;
            offset.y = 0f;
            if (box != _trackedBox || (position - _trackedPosition).sqrMagnitude > 0.002f * 0.002f)
            {
                _trackedBox = box;
                _trackedPosition = position;
                _stillSince = Time.time;
                return false;
            }

            return offset.magnitude <= _spotTolerance && Time.time - _stillSince >= _settleSeconds;
        }

        // 감지 영역 안의 키트 박스. 이미 민 박스(RejectedKit)는 제외한다.
        private Transform FindBox()
        {
            Vector3 center = BoxSpotWorld;
            int count = Physics.OverlapBoxNonAlloc(center, _boxSpotHalfExtents, _hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform root = FindBoxRoot(_hits[i].transform);
                if (root != null && root.GetComponent<RejectedKit>() == null)
                {
                    return root;
                }
            }

            return null;
        }

        private Transform FindBoxRoot(Transform from)
        {
            for (Transform t = from; t != null; t = t.parent)
            {
                if (t.name.StartsWith(_boxNamePrefix))
                {
                    return t;
                }
            }

            return null;
        }

        // 밀기 직전에 박스를 물리로 움직이게 하고 어느 키트인지 표시한다.
        private RejectedKit PrepareBox(Transform box, KitData kit)
        {
            // 컨베이어에 실려 있으면 먼저 푸셔가 가져간다. 컨베이어가 박스를 더 붙잡지 않아야 물리로 밀 수 있다.
            box.GetComponent<IBoxConveyorRelease>()?.ReleaseToPhysics();

            Rigidbody body = box.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = box.gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.mass = _boxMassKg;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            RejectedKit marker = box.GetComponent<RejectedKit>();
            if (marker == null)
            {
                marker = box.gameObject.AddComponent<RejectedKit>();
            }

            marker.Configure(kit);
            return marker;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(BoxSpotWorld, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _boxSpotHalfExtents * 2f);
        }
    }
}
