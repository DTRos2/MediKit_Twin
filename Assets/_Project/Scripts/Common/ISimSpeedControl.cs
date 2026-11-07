namespace MediKitTwin.Common
{
    /// <summary>
    /// 공정 전체 배속을 읽고 바꾸는 입구. (FR-15)
    /// 배속 버튼 화면은 시뮬레이션 시계가 정적 클래스(SimClock)인지 모르고 이 인터페이스만 부르므로,
    /// 시계가 바뀌어도 화면은 그대로이고 가짜 구현으로 시험할 수 있다.
    /// </summary>
    public interface ISimSpeedControl
    {
        /// <summary>지금 배속. 1이면 보통 속도다.</summary>
        float Speed { get; }

        /// <summary>true이면 배속을 바꿀 수 없다(ROS 제어 중 등).</summary>
        bool IsLocked { get; }

        /// <summary>배속을 바꾼다. 허용하지 않는 값이거나 고정되어 있으면 적용하지 않고 false를 돌려준다.</summary>
        bool TrySetSpeed(float speed);
    }
}
