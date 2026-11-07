namespace MediKitTwin.ArmA
{
    /// <summary>키트 1개를 만든 결과.</summary>
    public enum KittingResult
    {
        /// <summary>아직 끝나지 않았다.</summary>
        None,

        /// <summary>모든 구성품을 담았다.</summary>
        Completed,

        /// <summary>재고가 비어 멈췄다. 주문 관리가 StockEmpty로 그 주문을 보류하므로 다음 주문으로 넘어가면 된다. (FR-13)</summary>
        StockShortage,

        /// <summary>로봇이 닿지 못하는 등 오류로 멈췄다. 설비 상태를 Fault로 바꿔야 한다. (FR-03 예외)</summary>
        Fault
    }
}
