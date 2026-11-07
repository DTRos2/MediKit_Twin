namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 관리가 바깥에 알리는 일. 주문 관리가 CellEvents(정적 이벤트)를 직접 부르지 않고 이 인터페이스로만 알려서,
    /// 이벤트 버스 없이도 테스트할 수 있고 알리는 방법을 바꿔 끼울 수 있다.
    /// </summary>
    public interface IOrderEventSink
    {
        void OrderHeld(int orderSequence);

        void OrderResumed(int orderSequence);

        void WarningRaised(string code, string message);
    }
}
