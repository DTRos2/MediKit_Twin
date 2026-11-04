namespace MediKitTwin.Common
{
    /// <summary>ISimSpeedControl을 시뮬레이션 시계(SimClock)에 이어 주는 얇은 연결부. 상태를 따로 갖지 않는다.</summary>
    public sealed class SimClockSpeedControl : ISimSpeedControl
    {
        public float Speed => SimClock.Speed;

        public bool IsLocked => SimClock.IsSpeedLocked;

        public bool TrySetSpeed(float speed)
        {
            return SimClock.TrySetSpeed(speed);
        }
    }
}
