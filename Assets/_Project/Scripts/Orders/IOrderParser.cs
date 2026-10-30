namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일 내용을 주문 목록으로 바꾼다. CSV 말고 다른 형식이 필요하면 이 인터페이스만 새로 구현한다.
    /// </summary>
    public interface IOrderParser
    {
        OrderLoadResult Parse(string text);
    }
}
