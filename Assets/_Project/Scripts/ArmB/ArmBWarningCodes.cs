namespace MediKitTwin.ArmB
{
    /// <summary>팔 B 경고 코드. 대시보드와 로거가 코드로 경고 종류를 구분한다. 문장은 코드가 아니라 메시지에 담는다. (FR-10)</summary>
    public static class ArmBWarningCodes
    {
        /// <summary>팔 B가 뚜껑 덮기에 실패해 Fault가 되었다.</summary>
        public const string LidCoverFailed = "ARMB_LID_COVER_FAILED";
    }
}
