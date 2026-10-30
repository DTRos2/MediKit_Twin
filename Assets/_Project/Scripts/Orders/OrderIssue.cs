namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문을 불러오거나 키트를 만드는 중 생긴 경고 1건. 이 단계는 Unity와 CellEvents를 모르므로
    /// 경고를 반환값으로 모아 두고, 호출하는 쪽이 WarningRaised로 알린다.
    /// </summary>
    public readonly struct OrderIssue
    {
        public OrderIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>WarningRaised의 경고 코드로 그대로 쓴다. OrderIssueCodes의 값이다.</summary>
        public string Code { get; }

        public string Message { get; }
    }
}
