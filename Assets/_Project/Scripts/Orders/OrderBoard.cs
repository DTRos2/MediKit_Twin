using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문들의 상태 보관소: 만들 수 있는 주문(ready)의 순서와 보류 중인 주문(held)의 순서를 갖는다.
    /// 무엇을 언제 보류할지는 모르고 순서와 이동만 맡으므로, 보류 정책이 바뀌어도 이 클래스는 바뀌지 않는다.
    /// </summary>
    public sealed class OrderBoard
    {
        private readonly Dictionary<int, ManagedOrder> _ordersBySequence = new Dictionary<int, ManagedOrder>();
        private readonly List<ManagedOrder> _ready = new List<ManagedOrder>();
        private readonly List<ManagedOrder> _held = new List<ManagedOrder>();

        public OrderBoard(IEnumerable<OrderRecord> orders, IEnumerable<KitRequest> kitRequests)
        {
            foreach (OrderRecord order in orders)
            {
                var managed = new ManagedOrder(order);
                _ordersBySequence.Add(order.Sequence, managed);
                _ready.Add(managed);
            }

            // 키트 요청은 파일 순서대로 펼쳐져 있으므로 주문별로 나눠 담아도 투입 순서가 그대로 유지된다.
            foreach (KitRequest request in kitRequests)
            {
                if (_ordersBySequence.TryGetValue(request.OrderSequence, out ManagedOrder owner))
                {
                    owner.Enqueue(request);
                }
            }
        }

        /// <summary>대기열 맨 앞 주문. 없으면 null이다.</summary>
        public ManagedOrder FirstReady => _ready.Count > 0 ? _ready[0] : null;

        public IReadOnlyList<ManagedOrder> Held => _held;

        public int HeldCount => _held.Count;

        /// <summary>아직 피킹을 시작하지 않은 키트 수(대기열과 보류 주문의 남은 키트 합계).</summary>
        public int PendingKitCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _ready.Count; i++)
                {
                    count += _ready[i].PendingCount;
                }

                for (int i = 0; i < _held.Count; i++)
                {
                    count += _held[i].PendingCount;
                }

                return count;
            }
        }

        /// <summary>끝난 주문도 찾을 수 있다. 늦게 온 신호가 어느 주문인지 알아보는 데 쓴다.</summary>
        public bool TryGetOrder(int orderSequence, out ManagedOrder order)
        {
            return _ordersBySequence.TryGetValue(orderSequence, out order);
        }

        public void RemoveFromReady(ManagedOrder order)
        {
            _ready.Remove(order);
        }

        public void MoveToHeld(ManagedOrder order)
        {
            _ready.Remove(order);
            _held.Add(order);
        }

        /// <summary>보류가 풀린 주문들을 주어진 순서 그대로 대기열 맨 앞으로 옮긴다.</summary>
        public void ResumeToFront(IReadOnlyList<ManagedOrder> orders)
        {
            foreach (ManagedOrder order in orders)
            {
                _held.Remove(order);
            }

            _ready.InsertRange(0, orders);
        }
    }
}
