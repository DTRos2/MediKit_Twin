namespace MediKitTwin.Conveyor
{
    /// <summary>컨베이어 경고 코드. 대시보드와 로거가 코드로 경고 종류를 구분한다. 문장은 코드가 아니라 메시지에 담는다. (FR-05)</summary>
    public static class ConveyorWarningCodes
    {
        /// <summary>키트를 다 만들었지만 컨베이어 입구가 차 있어 박스를 보내지 못하고 있다.</summary>
        public const string EntryBlocked = "CONVEYOR_ENTRY_BLOCKED";

        /// <summary>박스를 지그에서 떼었으나 컨베이어가 받지 않았다. 이 키트의 박스는 컨베이어에 오르지 못했다.</summary>
        public const string EntryFailed = "CONVEYOR_ENTRY_FAILED";

        /// <summary>박스가 팩킹 구역에 선 채 뚜껑이 덮이지 않고 너무 오래 기다리고 있다.</summary>
        public const string PackingTimeout = "PACKING_LID_TIMEOUT";
    }
}
