namespace MediKitTwin.ArmA
{
    /// <summary>키팅 경고 코드. 대시보드와 로거가 코드로 경고 종류를 구분한다. 문장은 코드가 아니라 메시지에 담는다.</summary>
    public static class KittingIssueCodes
    {
        /// <summary>로봇암 A가 오류(Fault)로 키팅을 멈췄다. (FR-03 예외 처리)</summary>
        public const string ArmFault = "ARM_A_FAULT";
    }
}
