using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;
using Warcool.WarehouseRobot;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 컨베이어 끝 받침대. 박스가 받침대에 앉았다는 알림(AtConveyorEnd)을 받으면, 잠시 정지해 있는지 본 뒤 박스를 하역 로봇이 집을 수 있게
    /// 칸(ShelfStorage)에 등록한다. (FR-12)
    /// 박스는 받침(KitBoxCarrier)에서 떼어 CargoBox와 KitCargo를 붙이고, 물리로 밀리지 않게 키네마틱 Rigidbody를 달아 칸에 놓는다. 빈 받침은 지운다.
    /// 컨베이어를 직접 참조하지 않고, 이벤트 버스의 키트와 IKitCarrier로 박스를 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AgvPickPlatform : MonoBehaviour
    {
        private const int MaxOverlaps = 16;

        [Tooltip("박스를 담을 칸을 가진 ShelfStorage. 보통 이 오브젝트에 붙어 있다.")]
        [SerializeField] private ShelfStorage _storage;

        [Tooltip("박스를 놓을 ShelfStorage의 열 번호.")]
        [SerializeField, Min(0)] private int _column;

        [Tooltip("박스를 놓을 칸 번호. 로봇 팔 높이가 칸 번호로 정해지므로 벨트 높이에 닿는 Level 2(2)이다.")]
        [SerializeField, Min(0)] private int _level = 2;

        [Tooltip("박스가 받침대에 앉은 뒤 움직이지 않는지 보려고 기다리는 시뮬레이션 시간(초).")]
        [SerializeField, Min(0f)] private float _settleSeconds = 0.3f;

        [Tooltip("칸 위치 주변에서 박스를 찾는 범위의 반 크기(m). 박스(0.098 x 0.10 x 0.239)가 들어가고 이웃 물체는 닿지 않는 크기이다.")]
        [SerializeField] private Vector3 _searchHalfExtents = new Vector3(0.15f, 0.1f, 0.2f);

        [Tooltip("박스를 칸 위치로 옮길 때 이 거리(m)보다 많이 움직이면 경고한다. 칸 위치와 박스 원점의 가정이 어긋났다는 뜻이다.")]
        [SerializeField, Min(0f)] private float _maxSnapM = 0.02f;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];

        /// <summary>박스가 칸에 등록되어 하역 로봇이 집을 수 있다.</summary>
        public event Action<KitData> BoxRegistered;

        public ShelfStorage Storage => _storage;

        public int Column => _column;

        public int Level => _level;

        private void Awake()
        {
            if (_storage == null)
            {
                Debug.LogError($"{name}: Storage를 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            CellEvents.AtConveyorEnd += OnAtConveyorEnd;
        }

        private void OnDisable()
        {
            CellEvents.AtConveyorEnd -= OnAtConveyorEnd;
        }

        private void OnAtConveyorEnd(KitData kit)
        {
            if (kit != null)
            {
                StartCoroutine(Register(kit));
            }
        }

        private IEnumerator Register(KitData kit)
        {
            float waited = 0f;
            while (waited < _settleSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Transform slot = _storage.GetPoint(_column, _level);
            if (slot == null)
            {
                Debug.LogError($"{name}: 열 {_column}의 칸 {_level}이 없어 박스를 등록하지 못한다.", this);
                yield break;
            }

            IKitCarrier carrier = FindCarrier(slot.position, kit);
            if (carrier == null)
            {
                Debug.LogWarning($"{name}: 칸 근처에서 키트 {kit.KitSequence}의 박스를 찾지 못했다.", this);
                yield break;
            }

            if (_storage.GetBox(_column, _level) != null)
            {
                Debug.LogWarning($"{name}: 칸 {_level}에 이미 박스가 있어 키트 {kit.KitSequence}를 등록하지 않는다.", this);
                yield break;
            }

            Transform box = carrier.Box;
            float snapM = Vector3.Distance(box.position, slot.position);
            if (snapM > _maxSnapM)
            {
                Debug.LogWarning($"{name}: 박스를 칸 위치로 {snapM * 100f:0.0}cm 옮긴다. 박스 원점이 박스 한가운데라는 가정을 확인하라.", this);
            }

            var cargo = GetOrAdd<CargoBox>(box.gameObject);
            GetOrAdd<KitCargo>(box.gameObject).Bind(kit);

            var body = GetOrAdd<Rigidbody>(box.gameObject);
            body.isKinematic = true;
            body.useGravity = false;

            // 박스를 받침에서 떼어 칸의 자식으로 옮긴다. 그 뒤에 남은 빈 받침을 지운다.
            _storage.PutBox(_column, _level, cargo);
            if (carrier is Component carrierComponent)
            {
                Destroy(carrierComponent.gameObject);
            }

            BoxRegistered?.Invoke(kit);
        }

        // 칸 주변을 물리로 훑어 이 키트의 받침을 찾는다.
        private IKitCarrier FindCarrier(Vector3 center, KitData kit)
        {
            int count = Physics.OverlapBoxNonAlloc(center, _searchHalfExtents, _overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                IKitCarrier carrier = _overlaps[i].GetComponentInParent<IKitCarrier>();
                if (carrier != null && ReferenceEquals(carrier.Kit, kit))
                {
                    return carrier;
                }
            }

            return null;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }
    }
}
