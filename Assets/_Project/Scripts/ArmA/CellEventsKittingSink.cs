using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>키팅의 알림을 CellEvents로 내보낸다. 실제 씬에서 쓰는 구현이다.</summary>
    public sealed class CellEventsKittingSink : IKittingEventSink
    {
        public void KitReady(KitData kit) => CellEvents.RaiseKitReady(kit);

        public void WarningRaised(string code, string message) => CellEvents.RaiseWarning(code, message);
    }
}
