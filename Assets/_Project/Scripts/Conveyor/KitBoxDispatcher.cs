using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 키팅이 끝나면(KitReady) 지그의 박스를 컨베이어에 태운다. 임시 장치였던 ConveyorHandoffStub를 대신한다. (FR-05)
    /// 박스를 보내는 순서와 막혔을 때의 동작은 BoxHandoff가 맡고, 이 컴포넌트는 씬의 오브젝트를 이어 주기만 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitBoxDispatcher : MonoBehaviour
    {
        [Tooltip("박스 공급부(IBoxSupply 구현체, 보통 KitBoxSupply).")]
        [SerializeField] private MonoBehaviour _boxSupply;

        [Tooltip("박스를 올릴 첫 컨베이어 구간. 키팅 지그가 놓인 구간이다.")]
        [SerializeField] private ConveyorBase _entry;

        private BoxHandoff _handoff;

        private void Awake()
        {
            var supply = _boxSupply as IBoxSupply;
            if (supply == null || _entry == null)
            {
                Debug.LogError($"{name}: Box Supply(IBoxSupply)와 Entry(ConveyorBase)를 모두 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _handoff = new BoxHandoff(supply, new ConveyorBoxEntry(_entry, new KitBoxCarrierFactory()), CellEvents.RaiseWarning);
        }

        private void OnEnable()
        {
            CellEvents.KitReady += OnKitReady;
        }

        private void OnDisable()
        {
            CellEvents.KitReady -= OnKitReady;
        }

        private void Update()
        {
            // 입구가 막혔거나 지그에 새 박스가 아직 없어 못 보낸 키트가 있으면 다시 시도한다.
            if (_handoff.HasPending)
            {
                _handoff.Pump();
            }
        }

        private void OnKitReady(KitData kit)
        {
            _handoff.Enqueue(kit);
        }
    }
}
