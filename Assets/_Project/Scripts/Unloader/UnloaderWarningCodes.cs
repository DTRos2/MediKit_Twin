namespace MediKitTwin.Unloader
{
    /// <summary>하역 로봇이 WarningRaised로 알리는 경고 코드. (FR-12)</summary>
    public static class UnloaderWarningCodes
    {
        /// <summary>정해진 시간 안에 목표 자리에 도착하지 못했다.</summary>
        public const string DriveTimeout = "UNLOADER_DRIVE_TIMEOUT";

        /// <summary>컨베이어 끝 받침대에서 박스를 집지 못했다.</summary>
        public const string LoadFailed = "UNLOADER_LOAD_FAILED";

        /// <summary>하역장에 박스를 내려놓지 못했다.</summary>
        public const string UnloadFailed = "UNLOADER_UNLOAD_FAILED";

        /// <summary>박스의 키트 코드에 맞는 하역장 구역이 없어 첫 구역에 내려놓는다.</summary>
        public const string NoDockZone = "UNLOADER_NO_DOCK_ZONE";
    }
}
