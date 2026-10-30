using System;
using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문을 보류하고 재개하는 정책. (FR-13, FR-16) 보류 이유를 모아 이유가 모두 풀려야 재개하고,
    /// 만들던 키트를 되돌리며, 같은 주문이 반복해서 보류되면 경고한다.
    /// 보관(OrderBoard)과 알림(IOrderEventSink)은 각자 맡으므로, 보류 규칙을 바꿀 때 이 클래스만 고치면 된다.
    /// </summary>
    public sealed class OrderHoldController
    {
        private readonly OrderBoard _board;
        private readonly InFlightKits _inFlight;
        private readonly IOrderEventSink _events;
        private readonly OrderManagerSettings _settings;

        public OrderHoldController(OrderBoard board, InFlightKits inFlight, IOrderEventSink events, OrderManagerSettings settings)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _inFlight = inFlight ?? throw new ArgumentNullException(nameof(inFlight));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Hold(ManagedOrder order, HoldReason reason)
        {
            // 만들던 키트는 담긴 구성품을 버리고 처음부터 다시 만들어야 하므로 주문의 남은 키트로 되돌린다. (7-7)
            // 맨 앞으로 되돌리므로 투입 순서가 큰 키트부터 넣어야 원래 순서가 유지된다.
            IReadOnlyList<KitRequest> interrupted = _inFlight.TakeByOrder(order.Sequence);
            for (int i = interrupted.Count - 1; i >= 0; i--)
            {
                order.RequeueFront(interrupted[i]);
            }

            // 남은 키트도 만드는 중인 키트도 없으면 보류할 것이 없다(이미 끝난 주문에 늦게 온 신호).
            if (order.PendingCount == 0)
            {
                return;
            }

            if (!order.AddHoldReason(reason))
            {
                return;
            }

            _board.MoveToHeld(order);
            _events.OrderHeld(order.Sequence);

            if (order.HoldCount == _settings.RepeatedHoldWarningCount)
            {
                _events.WarningRaised(OrderIssueCodes.RepeatedHold,
                    $"주문 {order.Sequence}이 {order.HoldCount}번 보류되었다.");
            }
        }

        /// <summary>키트를 만드는 중인 모든 주문을 같은 이유로 보류한다. 설비 Fault처럼 어느 주문 탓인지 가릴 수 없을 때 쓴다.</summary>
        public void HoldAllInFlight(HoldReason reason)
        {
            foreach (int orderSequence in _inFlight.OrderSequences())
            {
                if (_board.TryGetOrder(orderSequence, out ManagedOrder order))
                {
                    Hold(order, reason);
                }
            }
        }

        /// <summary>이유 하나를 푼다. 그 이유가 마지막이었던 주문은 보류된 순서를 유지한 채 대기열 맨 앞에서 재개한다.</summary>
        public void Release(HoldReason reason)
        {
            List<ManagedOrder> resumed = null;

            for (int i = 0; i < _board.Held.Count; i++)
            {
                ManagedOrder order = _board.Held[i];
                if (order.RemoveHoldReason(reason) && !order.IsHeld)
                {
                    (resumed ?? (resumed = new List<ManagedOrder>())).Add(order);
                }
            }

            if (resumed == null)
            {
                return;
            }

            _board.ResumeToFront(resumed);
            foreach (ManagedOrder order in resumed)
            {
                _events.OrderResumed(order.Sequence);
            }
        }
    }
}
