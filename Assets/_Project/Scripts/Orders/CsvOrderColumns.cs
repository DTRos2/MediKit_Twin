namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 CSV의 열 배치. 열 순서가 바뀌어도 파서를 고치지 않고 이 값만 바꿔 넘긴다.
    /// 기본 배치는 SRS FR-01의 순번, 병원명, 키트 코드, 수량이다.
    /// </summary>
    public sealed class CsvOrderColumns
    {
        public CsvOrderColumns(int sequenceIndex, int hospitalNameIndex, int kitCodeIndex, int quantityIndex)
        {
            SequenceIndex = sequenceIndex;
            HospitalNameIndex = hospitalNameIndex;
            KitCodeIndex = kitCodeIndex;
            QuantityIndex = quantityIndex;
        }

        public static CsvOrderColumns Default => new CsvOrderColumns(0, 1, 2, 3);

        public int SequenceIndex { get; }

        public int HospitalNameIndex { get; }

        public int KitCodeIndex { get; }

        public int QuantityIndex { get; }

        /// <summary>한 행이 가져야 하는 열 개수. 가장 뒤 열 번호에서 구한다.</summary>
        public int RequiredCount => System.Math.Max(
            System.Math.Max(SequenceIndex, HospitalNameIndex),
            System.Math.Max(KitCodeIndex, QuantityIndex)) + 1;
    }
}
