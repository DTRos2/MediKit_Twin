using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Pusher
{
    /// <summary>
    /// 불량 박스. 안쪽 영역(트리거)에 푸셔가 밀어낸 키트 박스(RejectedKit)가 들어오면 세고, KitRejected를 보낸다. (FR-09)
    /// 적재 수가 정원(기본 5)에 차면 안의 키트 박스를 모두 지우고 RejectBinCleared를 보낸다. 누적 불량 수는 비워도 유지된다.
    /// 계산은 RejectBinTally가 하고, 이 컴포넌트는 물리 접촉과 이벤트를 이어 준다. 시간은 시뮬레이션 시간이라 배속과 일시정지를 따른다.
    /// 영역은 Tools > MediKit > Add Load Zone To RejectBin Prefab 으로 프리팹에 붙인다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RejectBin : MonoBehaviour
    {
        [Tooltip("정원. 적재 수가 이 값에 도달하면 비운다.")]
        [SerializeField, Min(1)] private int _capacity = RejectBinTally.DefaultCapacity;

        [Tooltip("정원이 찬 뒤 키트 박스를 지우기까지 기다리는 시간(초). 마지막 박스가 자리 잡는 모습을 보여 주기 위한 시간이다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _clearDelaySeconds = 1f;

        private readonly List<RejectedKit> _loaded = new List<RejectedKit>();
        private RejectBinTally _tally;
        private int _testBoxCount;

        /// <summary>지금 들어 있는 키트 수.</summary>
        public int Loaded => _tally.Loaded;

        /// <summary>지금까지 불량으로 처리한 키트 수. 비워도 유지된다.</summary>
        public int Total => _tally.Total;

        public int Capacity => _tally.Capacity;

        public int ClearCount => _tally.ClearCount;

        private void Awake()
        {
            _tally = new RejectBinTally(_capacity);
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            RejectedKit kit = other.GetComponentInParent<RejectedKit>();
            if (kit == null || kit.Kit == null || _loaded.Contains(kit))
            {
                return;
            }

            Receive(kit);
        }

        private void Receive(RejectedKit box)
        {
            _loaded.Add(box);
            box.Kit.CompleteTime = SimClock.Now;
            bool full = _tally.AddLoaded();
            Debug.Log($"[RejectBin] 로트 {box.Kit.LotNumber}가 들어왔다. 적재 {(full ? _tally.Capacity : _tally.Loaded)}/{_tally.Capacity}, 누적 {_tally.Total}", this);
            CellEvents.RaiseKitRejected(box.Kit);

            if (full)
            {
                // 비우는 동안 들어오는 키트가 섞이지 않도록 지금 들어 있는 것만 떼어 둔다.
                var toRemove = new List<RejectedKit>(_loaded);
                _loaded.Clear();
                StartCoroutine(Clear(toRemove));
            }
        }

        private IEnumerator Clear(List<RejectedKit> boxes)
        {
            if (_clearDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(_clearDelaySeconds);
            }

            foreach (RejectedKit box in boxes)
            {
                if (box != null)
                {
                    Destroy(box.gameObject);
                }
            }

            Debug.Log($"[RejectBin] 정원 {_tally.Capacity}개가 차서 비웠다. 누적 {_tally.Total}", this);
            CellEvents.RaiseRejectBinCleared();
        }

        // 푸셔가 없어도 세기와 비우기를 확인하도록 키트 박스 크기의 상자를 불량 박스 위에서 떨어뜨린다. (시험용)
        [ContextMenu("Drop Test Box")]
        private void DropTestBox()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 모드에서만 시험 박스를 떨어뜨릴 수 있다.", this);
                return;
            }

            _testBoxCount++;
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"TestKitBox_{_testBoxCount}";
            box.transform.localScale = new Vector3(0.0976f, 0.10f, 0.2387f);
            box.transform.position = GetComponent<BoxCollider>().bounds.center + Vector3.up * 0.6f;

            // 불량 박스 하위에 두어 Hierarchy를 어지럽히지 않고, 불량 박스가 사라지면 함께 사라지게 한다. 월드 위치와 크기는 유지한다.
            box.transform.SetParent(transform, true);
            box.AddComponent<Rigidbody>();
            box.AddComponent<RejectedKit>().Configure(new KitData(_testBoxCount, _testBoxCount, $"TEST-{_testBoxCount:0000}", "시험", "EM"));
        }
    }
}
