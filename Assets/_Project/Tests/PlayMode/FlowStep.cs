#if UNITY_EDITOR
namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>키트가 키팅에서 하역까지 거치는 알림의 종류. 시험이 키트별로 받은 순서를 따질 때 쓴다.</summary>
    internal enum FlowStep
    {
        KitReady,
        AtInspection,
        InspectionResult,
        KitRejected,
        AtPacking,
        LidClosed,
        AtConveyorEnd
    }
}
#endif
