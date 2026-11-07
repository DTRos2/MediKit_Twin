namespace MediKitTwin.ML
{
    /// <summary>합성 데이터 박스의 내용 종류. 정상이 아니면 불량이며, 누락과 오구성 모두 판정에서는 불량이다.</summary>
    public enum BoxVariant
    {
        /// <summary>레시피대로 모든 구성품이 제자리에 있다.</summary>
        Normal,

        /// <summary>구성품 1개가 빠져 슬롯 1칸이 비어 있다.</summary>
        Missing,

        /// <summary>구성품 1개가 레시피에 없는 다른 구성품으로 바뀌어 있다.</summary>
        WrongItem
    }
}
