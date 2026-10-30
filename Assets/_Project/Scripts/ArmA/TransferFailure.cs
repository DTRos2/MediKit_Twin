namespace MediKitTwin.ArmA
{
    /// <summary>구성품 이송이 실패한 이유.</summary>
    public enum TransferFailure
    {
        None,

        /// <summary>로봇암이 목표 자세에 시간 안에 닿지 못했다.</summary>
        MotionFailed,

        /// <summary>집는 순간 재고에서 꺼내지 못했다. 재고가 이미 비었다는 뜻이다.</summary>
        CommitRejected,

        /// <summary>그리퍼를 닫았지만 물건을 쥐지 못했다.</summary>
        GraspFailed
    }
}
