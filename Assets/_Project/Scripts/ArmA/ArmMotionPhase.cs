namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 옮기는 동안 거치는 단계. 관찰하는 쪽(진단)이 "접은 뒤", "도착한 뒤"에 맞춰 무언가를 재려고 알림을 받는다.
    /// 이동 방식은 이 단계를 알리기만 하고, 알림을 어떻게 쓰는지는 모른다.
    /// </summary>
    public enum ArmMotionPhase
    {
        /// <summary>이동 전에 팔을 다 접었다.</summary>
        Stowed,

        /// <summary>베이스가 서는 자리에 도착했다.</summary>
        BaseArrived,
    }
}
