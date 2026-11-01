namespace MediKitTwin.ML
{
    /// <summary>슬롯 크롭 이미지 한 장과 그 정답. labels.csv의 한 행이 된다.</summary>
    public readonly struct SlotCropRecord
    {
        /// <summary>빈 슬롯의 라벨. CNN은 크롭 하나를 이 값 또는 구성품 ID 중 하나로 분류한다.</summary>
        public const string EmptyLabel = "empty";

        public SlotCropRecord(string imagePath, SyntheticBoxCase boxCase, int slotNumber)
        {
            ImagePath = imagePath;
            Split = boxCase.Split;
            CaseIndex = boxCase.Index;
            KitCode = boxCase.KitCode;
            SlotNumber = slotNumber;
            ExpectedComponentId = boxCase.GetExpectedComponentId(slotNumber);
            Label = boxCase.GetActualComponentId(slotNumber);
            BoxVariant = boxCase.Variant;
        }

        public string ImagePath { get; }

        public DataSplit Split { get; }

        public int CaseIndex { get; }

        public string KitCode { get; }

        public int SlotNumber { get; }

        /// <summary>레시피가 이 슬롯에 요구하는 구성품 ID.</summary>
        public string ExpectedComponentId { get; }

        /// <summary>크롭에 실제로 찍힌 내용: 구성품 ID 또는 EmptyLabel. 학습할 때 정답이 되는 값이다.</summary>
        public string Label { get; }

        /// <summary>이 크롭이 속한 박스의 종류. 슬롯 하나만 보면 정상이어도 박스는 불량일 수 있다.</summary>
        public BoxVariant BoxVariant { get; }
    }
}
