namespace MediKitTwin.Demo
{
    /// <summary>컨베이어 속도를 화면에 적는 글. 패널의 머리줄, 슬라이더 값, 버튼이 같은 모양으로 적도록 한곳에 둔다.</summary>
    public static class ConveyorSpeedText
    {
        private const string Unit = "m/s";

        /// <summary>예: 0.8 → "0.80 m/s".</summary>
        public static string Format(float speedMps)
        {
            return $"{speedMps:0.00} {Unit}";
        }
    }
}
