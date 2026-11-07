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
        GraspFailed,

        /// <summary>카메라로 물건을 찾지 못했고, 인식이 필수여서 집지 않았다.</summary>
        RecognitionFailed,

        /// <summary>팔이 선반이나 다른 물체와 겹치지 않고 닿는 자세를 찾지 못해, 움직이지 않았다.</summary>
        CollisionRisk,

        /// <summary>알려 준 쥐는 자리가 지금 물건에서 서는 자리를 옮겨 맞출 수 있는 한계보다 멀어(다른 칸이거나 선반이 많이 바뀜), 그 자세를 쓰지 않고 움직이지 않았다.</summary>
        TaughtPoseStale
    }
}
