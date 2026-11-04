namespace MediKitTwin.Demo
{
    /// <summary>
    /// 컨베이어 속도 패널에 나오는 글. 프리팹을 만드는 도구와 화면 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// 속도 숫자를 적는 모양은 ConveyorSpeedText가 맡는다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ConveyorSpeedPanelTexts
    {
        public const string Title = "컨베이어";
        public const string Subtitle = "CONVEYOR · 속도";
        public const string SpeedLabel = "속도";

        /// <summary>예: 0.5 → "처음 속도로 (0.50 m/s)".</summary>
        public static string Reset(float initialSpeedMps)
        {
            return $"처음 속도로 ({ConveyorSpeedText.Format(initialSpeedMps)})";
        }
    }
}
