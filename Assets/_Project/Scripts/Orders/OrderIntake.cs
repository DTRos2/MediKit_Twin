using System;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 불러오기부터 키트 요청 만들기까지를 한 번에 처리하는 진입점. (FR-01)
    /// 대기열 관리, 보류와 재개(IKitSource)는 이 결과를 받아 쓰는 쪽에서 구현하므로 여기서는 다루지 않는다.
    /// </summary>
    public sealed class OrderIntake
    {
        private readonly OrderLoader _loader;
        private readonly KitRequestPlanner _planner;

        public OrderIntake(OrderLoader loader, KitRequestPlanner planner)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public OrderIntakeResult Run()
        {
            OrderLoadResult loaded = _loader.Load();
            return new OrderIntakeResult(loaded.Orders, _planner.Plan(loaded.Orders), loaded.Issues);
        }
    }
}
