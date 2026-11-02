namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 박스에 놓는 동안 쥔 자세를 재는 지점의 이름. 로그에 그대로 찍히고, 이름이 달라지면 로그를 읽는 쪽이 지점을 알아보지 못한다.
    /// 기준은 들어 올린 직후(놓으러 가기 시작할 때)에 잰 자세다. 그 뒤 순서대로:
    /// 팔 접은 직후 → 베이스 도착 직후 (서는 자리를 찾는 동안 되풀이될 수 있다) → 박스 곁 도착(서는 자리에서 방향 찾기까지 끝난 뒤)
    /// → 슬롯 위 도착 → 놓기 직전(슬롯 자리까지 내린 뒤).
    /// </summary>
    public static class SlipPoints
    {
        public const string Stowed = "팔 접은 직후";
        public const string BaseArrived = "베이스 도착 직후";
        public const string BoxSide = "박스 곁 도착 직후";
        public const string AboveSlot = "슬롯 위 도착 직후";
        public const string BeforeRelease = "놓기 직전";

        /// <summary>
        /// 같은 지점이 여러 번 나올 때 몇 번째인지 붙인 이름. 첫 번째는 이름 그대로다.
        /// 놓는 높이를 바꿔 다시 찾을 때마다 접기와 이동이 되풀이되므로, 첫 번째와 그 뒤를 로그에서 가르려는 것이다.
        /// </summary>
        public static string Label(string point, int occurrence)
        {
            return occurrence <= 1 ? point : $"{point} {occurrence}번째";
        }

        /// <summary>팔 베이스를 옮기는 단계에 해당하는 지점 이름.</summary>
        public static string For(ArmMotionPhase phase)
        {
            return phase == ArmMotionPhase.Stowed ? Stowed : BaseArrived;
        }
    }
}
