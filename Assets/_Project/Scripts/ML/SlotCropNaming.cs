namespace MediKitTwin.ML
{
    /// <summary>슬롯 크롭 이미지의 저장 경로 규칙. labels.csv의 이미지 경로와 실제 파일이 같은 규칙을 쓴다.</summary>
    public static class SlotCropNaming
    {
        /// <summary>데이터셋 폴더 기준 상대 경로. 예: train/case_00012_slot3.png (구분자는 항상 슬래시).</summary>
        public static string RelativePath(DataSplit split, int caseIndex, int slotNumber)
        {
            return $"{split.ToString().ToLowerInvariant()}/case_{caseIndex:D5}_slot{slotNumber}.png";
        }
    }
}
