namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일의 1행. 병원 1곳이 키트 1종을 수량만큼 요청한 것이다. (FR-01)
    /// 한 번 만들면 바뀌지 않는다. 보류와 재개는 주문 단위로 처리하므로 순번이 주문의 식별자이다.
    /// </summary>
    public sealed class OrderRecord
    {
        public OrderRecord(int sequence, string hospitalName, string kitCode, int quantity)
        {
            Sequence = sequence;
            HospitalName = hospitalName ?? string.Empty;
            // 레시피의 키트 코드(대문자)와 조회가 어긋나지 않도록 KitData와 같은 방식으로 맞춘다.
            KitCode = kitCode?.Trim().ToUpperInvariant();
            Quantity = quantity;
        }

        public int Sequence { get; }

        public string HospitalName { get; }

        public string KitCode { get; }

        public int Quantity { get; }
    }
}
