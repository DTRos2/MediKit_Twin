using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅이 바깥에 알리는 일. 키팅 로직이 CellEvents(정적 이벤트)를 직접 부르지 않고 이 인터페이스로만 알려서,
    /// 이벤트 버스 없이도 시험할 수 있다. (주문 관리, 재고의 알림 접점과 같은 방식)
    /// </summary>
    public interface IKittingEventSink
    {
        /// <summary>키트를 다 만들었다. 컨베이어가 이 신호로 박스를 받아 간다.</summary>
        void KitReady(KitData kit);

        void WarningRaised(string code, string message);
    }
}
