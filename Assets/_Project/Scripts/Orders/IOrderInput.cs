namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문을 넣는 접점. 화면과 시험 도구는 이 인터페이스로만 주문을 바꾸고, 주문을 어디서 읽어 어떻게 관리하는지는 모른다.
    /// 구현체가 지금의 주문 관리를 새 주문으로 바꾼다. (FR-01)
    /// </summary>
    public interface IOrderInput
    {
        /// <summary>가장 최근에 불러온 결과. 아직 불러온 적이 없으면 null이다.</summary>
        OrderIntakeResult LastIntake { get; }

        /// <summary>CSV 글자를 받아 주문을 바꾼다.</summary>
        OrderIntakeResult LoadText(string csv);

        /// <summary>설정된 주문 파일을 읽어 주문을 바꾼다. (파일이 없으면 기본 주문을 쓴다)</summary>
        OrderIntakeResult LoadFromFile();
    }
}
