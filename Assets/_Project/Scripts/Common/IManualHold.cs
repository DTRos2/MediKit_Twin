namespace MediKitTwin.Common
{
    /// <summary>
    /// 사람이 로봇을 직접 조작하는 동안 자동 목표를 막는 접점. 켜면 남은 도구 끝점 목표를 지우고 새 목표도 받지 않아서,
    /// 자동 동작이 사람이 정한 관절 목표를 덮어쓰지 못하고 팔이 그 자리에 남는다. 끄면 다시 목표를 받는다.
    /// </summary>
    public interface IManualHold : IManualHoldState
    {
        /// <summary>자동 목표를 막는다. 이미 막혀 있으면 아무것도 하지 않는다.</summary>
        void BeginHold();

        /// <summary>자동 목표를 다시 받는다. 막혀 있지 않으면 아무것도 하지 않는다.</summary>
        void EndHold();
    }
}
