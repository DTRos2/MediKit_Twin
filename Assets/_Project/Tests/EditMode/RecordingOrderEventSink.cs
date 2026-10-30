using System.Collections.Generic;

namespace MediKitTwin.Orders.Tests
{
    /// <summary>주문 관리가 알린 일을 순서대로 모아 두는 테스트용 구현. CellEvents 없이 알림을 검증한다.</summary>
    internal sealed class RecordingOrderEventSink : IOrderEventSink
    {
        internal List<int> Held { get; } = new List<int>();

        internal List<int> Resumed { get; } = new List<int>();

        internal List<string> WarningCodes { get; } = new List<string>();

        public void OrderHeld(int orderSequence) => Held.Add(orderSequence);

        public void OrderResumed(int orderSequence) => Resumed.Add(orderSequence);

        public void WarningRaised(string code, string message) => WarningCodes.Add(code);
    }
}
