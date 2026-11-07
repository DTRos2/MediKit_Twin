using System.Collections.Generic;

namespace MediKitTwin.Orders
{
    /// <summary>주문 불러오기 결과. 순번이 겹치지 않는 유효한 주문(파일 순서)과 건너뛴 행의 경고를 담는다.</summary>
    public sealed class OrderLoadResult
    {
        public OrderLoadResult(IReadOnlyList<OrderRecord> orders, IReadOnlyList<OrderIssue> issues)
        {
            Orders = orders;
            Issues = issues;
        }

        public IReadOnlyList<OrderRecord> Orders { get; }

        public IReadOnlyList<OrderIssue> Issues { get; }
    }
}
