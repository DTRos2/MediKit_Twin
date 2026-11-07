namespace MediKitTwin.ArmA
{
    /// <summary>키팅 계획의 항목 하나가 지금 어느 단계인지.</summary>
    public enum KittingItemState
    {
        /// <summary>아직 차례가 아니다.</summary>
        Waiting,

        /// <summary>선반에서 집는 중이다.</summary>
        Picking,

        /// <summary>쥐고 박스 슬롯으로 옮겨 놓는 중이다.</summary>
        Placing,

        /// <summary>박스 슬롯에 놓았다.</summary>
        Placed,

        /// <summary>누락 규칙이 빠뜨리기로 해서 집지 않는다.</summary>
        Skipped
    }
}
