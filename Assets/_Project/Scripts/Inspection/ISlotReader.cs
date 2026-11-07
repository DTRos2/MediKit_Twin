namespace MediKitTwin.Inspection
{
    /// <summary>박스 슬롯의 내용을 읽는 접점. 규칙 판정(FR-06)은 슬롯 감지 센서로 읽는다는 사실을 몰라도 된다.</summary>
    public interface ISlotReader
    {
        /// <summary>
        /// 슬롯 1번부터 slotCount번까지의 구성품 ID를 읽는다. 인덱스는 슬롯 번호 - 1이고 빈 슬롯은 null이다.
        /// 센서가 하나라도 없거나 응답하지 않으면 false이고 componentIds는 null이다. (FR-06 예외 처리)
        /// </summary>
        bool TryRead(int slotCount, out string[] componentIds);
    }
}
