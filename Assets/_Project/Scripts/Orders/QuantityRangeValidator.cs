using System;

namespace MediKitTwin.Orders
{
    /// <summary>수량이 범위 안인 주문만 받아들인다. 범위는 코드에 쓰지 않고 호출하는 쪽(인스펙터, SO)이 정한다.</summary>
    public sealed class QuantityRangeValidator : IOrderRowValidator
    {
        private readonly int _minQuantity;
        private readonly int _maxQuantity;

        public QuantityRangeValidator(int minQuantity, int maxQuantity)
        {
            if (minQuantity < 1 || maxQuantity < minQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQuantity), "수량 범위는 1 이상이고 최대가 최소 이상이어야 한다.");
            }

            _minQuantity = minQuantity;
            _maxQuantity = maxQuantity;
        }

        public bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue)
        {
            if (order.Quantity >= _minQuantity && order.Quantity <= _maxQuantity)
            {
                issue = default;
                return true;
            }

            issue = new OrderIssue(OrderIssueCodes.QuantityOutOfRange,
                $"{lineNumber}행을 건너뛴다: 수량 {order.Quantity}이 {_minQuantity}~{_maxQuantity} 범위 밖이다");
            return false;
        }
    }
}
