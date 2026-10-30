namespace MediKitTwin.Orders
{
    /// <summary>
    /// 순번이 겹치는 주문을 거부한다. 보류와 재개가 순번으로 주문을 가리키므로 순번은 주문의 식별자이다.
    /// 앞선 검증기가 거부한 행의 순번은 쓰지 않은 것으로 남도록, 순번 검증은 마지막에 둔다.
    /// </summary>
    public sealed class UniqueSequenceValidator : IOrderRowValidator
    {
        public bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue)
        {
            if (context.TryRegisterSequence(order.Sequence))
            {
                issue = default;
                return true;
            }

            issue = new OrderIssue(OrderIssueCodes.DuplicateSequence,
                $"{lineNumber}행을 건너뛴다: 순번 {order.Sequence}이 이미 있다");
            return false;
        }
    }
}
