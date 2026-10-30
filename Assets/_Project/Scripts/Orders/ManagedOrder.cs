using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 1건의 진행 상태: 아직 투입하지 않은 키트와 보류 이유.
    /// 상태를 여러 컬렉션에 흩어 두지 않고 주문 쪽에 모아, 주문 단위 규칙(보류와 재개)을 한 곳에서 지킨다.
    /// 상태를 바꾸는 메서드는 같은 어셈블리의 OrderBoard와 OrderHoldController만 부른다.
    /// </summary>
    public sealed class ManagedOrder
    {
        private readonly LinkedList<KitRequest> _pending = new LinkedList<KitRequest>();
        private readonly HashSet<HoldReason> _holdReasons = new HashSet<HoldReason>();

        public ManagedOrder(OrderRecord order)
        {
            Order = order;
        }

        public OrderRecord Order { get; }

        public int Sequence => Order.Sequence;

        /// <summary>아직 피킹을 시작하지 않은 키트 수.</summary>
        public int PendingCount => _pending.Count;

        public bool IsHeld => _holdReasons.Count > 0;

        /// <summary>지금까지 보류된 횟수. 같은 주문이 반복해서 보류되는지 알아보는 데 쓴다. (FR-13)</summary>
        public int HoldCount { get; private set; }

        internal void Enqueue(KitRequest request)
        {
            _pending.AddLast(request);
        }

        /// <summary>투입하다 만 키트를 맨 앞으로 되돌린다. 재개하면 이 키트부터 다시 만든다.</summary>
        internal void RequeueFront(KitRequest request)
        {
            _pending.AddFirst(request);
        }

        internal bool TryDequeue(out KitRequest request)
        {
            if (_pending.Count == 0)
            {
                request = null;
                return false;
            }

            request = _pending.First.Value;
            _pending.RemoveFirst();
            return true;
        }

        /// <summary>이유를 더한다. 이 호출로 보류가 시작되었으면(이전에는 보류가 아니었으면) true이다.</summary>
        internal bool AddHoldReason(HoldReason reason)
        {
            bool wasHeld = IsHeld;
            _holdReasons.Add(reason);
            if (!wasHeld)
            {
                HoldCount++;
            }

            return !wasHeld;
        }

        /// <summary>이유를 지운다. 그 이유가 있었으면 true이다. 모든 이유가 풀렸는지는 IsHeld로 본다.</summary>
        internal bool RemoveHoldReason(HoldReason reason)
        {
            return _holdReasons.Remove(reason);
        }
    }
}
