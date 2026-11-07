using System;
using MediKitTwin.Common;
using MediKitTwin.Recipes;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 로봇암 A에게 다음 키트를 내주고, 들어오는 신호를 보류·재개로 연결한다. (FR-01, FR-02, FR-13, FR-16)
    /// 로봇암 A는 IKitSource로만, 대시보드는 IOrderStatus로만 이 클래스를 본다.
    /// 주문 상태는 OrderBoard, 만드는 중인 키트는 InFlightKits, 보류 규칙은 OrderHoldController가 맡고
    /// 이 클래스는 그것들을 묶어 외부 인터페이스를 제공하기만 한다.
    /// </summary>
    public sealed class OrderManager : IKitSource, IOrderStatus
    {
        private readonly IRecipeCatalog _recipes;
        private readonly IOrderEventSink _events;
        private readonly OrderManagerSettings _settings;
        private readonly OrderBoard _board;
        private readonly InFlightKits _inFlight;
        private readonly OrderHoldController _holds;

        private int _lastKitSequence;

        public OrderManager(OrderIntakeResult intake, IRecipeCatalog recipes, IOrderEventSink events, OrderManagerSettings settings)
        {
            if (intake == null)
            {
                throw new ArgumentNullException(nameof(intake));
            }

            _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _board = new OrderBoard(intake.Orders, intake.KitRequests);
            _inFlight = new InFlightKits();
            _holds = new OrderHoldController(_board, _inFlight, _events, _settings);
        }

        public int HeldOrderCount => _board.HeldCount;

        public int PendingKitCount => _board.PendingKitCount;

        public bool TryGetNext(out KitData kit, out RecipeSO recipe)
        {
            ManagedOrder order;
            while ((order = _board.FirstReady) != null)
            {
                // 만들 키트가 없는 주문이 맨 앞에 남아 있으면 Hold가 아무것도 하지 않아 같은 주문을 계속 보게 되므로 먼저 치운다.
                if (order.PendingCount == 0)
                {
                    _board.RemoveFromReady(order);
                    continue;
                }

                if (!_recipes.TryGetRecipe(order.Order.KitCode, out recipe))
                {
                    _events.WarningRaised(OrderIssueCodes.RecipeMissing,
                        $"주문 {order.Sequence}의 키트 코드 '{order.Order.KitCode}'에 맞는 레시피가 없어 보류한다.");
                    _holds.Hold(order, HoldReason.RecipeMissing());
                    continue;
                }

                order.TryDequeue(out KitRequest request);
                if (order.PendingCount == 0)
                {
                    _board.RemoveFromReady(order);
                }

                kit = request.CreateKitData(++_lastKitSequence);
                _inFlight.Add(kit.KitSequence, request);
                return true;
            }

            kit = null;
            recipe = null;
            return false;
        }

        /// <summary>키트를 다 만들었다(KitReady). 이후 그 주문이 보류되어도 이 키트는 되돌리지 않는다.</summary>
        public void OnKitReady(KitData kit)
        {
            _inFlight.Complete(kit.KitSequence);
        }

        /// <summary>재고(IStock)가 꺼내기에 실패했다. 그 주문을 보류한다. 같은 칸을 기다리는 다른 주문도 이 신호로 보류된다.</summary>
        public void OnStockEmpty(string shelfSlotId, int orderSequence)
        {
            if (_board.TryGetOrder(orderSequence, out ManagedOrder order))
            {
                _holds.Hold(order, HoldReason.StockShortage(shelfSlotId));
            }
        }

        /// <summary>선반 칸이 보충되었다. 그 칸만 기다리던 보류 주문을 대기열 맨 앞으로 옮겨 다시 시작한다.</summary>
        public void OnStockRefilled(string shelfSlotId)
        {
            _holds.Release(HoldReason.StockShortage(shelfSlotId));
        }

        /// <summary>설비 상태가 바뀌었다. 정해 둔 설비가 Fault이면 만드는 중인 주문을 보류하고, Fault에서 벗어나면 되돌린다.</summary>
        public void OnStationStateChanged(StationId station, StationState state)
        {
            if (!_settings.HoldsOrderOnFault(station))
            {
                return;
            }

            HoldReason reason = HoldReason.StationFault(station);
            if (state == StationState.Fault)
            {
                _holds.HoldAllInFlight(reason);
            }
            else
            {
                _holds.Release(reason);
            }
        }
    }
}
