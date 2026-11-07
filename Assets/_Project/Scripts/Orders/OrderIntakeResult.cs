using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>주문 접수 결과. 주문 목록, 투입 순서대로 펼친 키트 요청, 경고를 담는다.</summary>
    public sealed class OrderIntakeResult
    {
        public OrderIntakeResult(
            IReadOnlyList<OrderRecord> orders,
            IReadOnlyList<KitRequest> kitRequests,
            IReadOnlyList<OrderIssue> issues)
        {
            Orders = orders;
            KitRequests = kitRequests;
            Issues = issues;
        }

        public IReadOnlyList<OrderRecord> Orders { get; }

        public IReadOnlyList<KitRequest> KitRequests { get; }

        public IReadOnlyList<OrderIssue> Issues { get; }
    }
}
