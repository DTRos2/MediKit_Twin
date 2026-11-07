namespace MediKitTwin.Label
{
    /// <summary>라벨 설비의 경고 코드. 대시보드와 로거가 코드로 경고 종류를 구분한다. (FR-11)</summary>
    public static class LabelWarningCodes
    {
        /// <summary>뚜껑이 덮였다고 알린 키트의 박스에서 뚜껑을 찾지 못해 라벨을 붙이지 못했다.</summary>
        public const string NoLid = "LABEL_NO_LID";
    }
}
