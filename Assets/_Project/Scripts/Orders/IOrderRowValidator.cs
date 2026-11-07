namespace MediKitTwin.Orders
{
    /// <summary>
    /// 해석이 끝난 주문 행 하나를 검사한다. 규칙을 추가할 때 파서를 고치지 않고 이 인터페이스 구현을 하나 더 넘긴다.
    /// </summary>
    public interface IOrderRowValidator
    {
        /// <summary>받아들이면 true이다. 거부하면 false이고 <paramref name="issue"/>에 건너뛰는 이유를 담는다.</summary>
        bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue);
    }
}
