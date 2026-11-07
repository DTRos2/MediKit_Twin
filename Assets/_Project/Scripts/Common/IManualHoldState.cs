namespace MediKitTwin.Common
{
    /// <summary>
    /// 사람이 로봇을 직접 조작하는 중인지 읽는 쪽. 자동 동작을 돌리는 쪽이 이 값을 보고 사람이 쓰는 동안 새 일을 시작하지 않는다.
    /// 켜고 끄는 일은 IManualHold가 맡는다.
    /// </summary>
    public interface IManualHoldState
    {
        /// <summary>사람이 직접 조작하는 중이면 true.</summary>
        bool IsHeld { get; }
    }
}
