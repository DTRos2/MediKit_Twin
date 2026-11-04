using System.Globalization;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 배속 패널에 보이는 글. 문구를 화면 코드와 편집기 도구에 흩어 두지 않고 한곳에 모았고, 숫자는 문화권과 상관없이 같은 모양으로 적는다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class SimSpeedPanelTexts
    {
        public const string Title = "배속";
        public const string Subtitle = "SIM · SPEED";

        /// <summary>버튼에 적는 배속. 0.5 → "0.5배", 2 → "2배".</summary>
        public static string Speed(float speed)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}배", speed);
        }

        /// <summary>머리줄 오른쪽에 보이는 현재 배속.</summary>
        public static string Current(float speed)
        {
            return string.Format(CultureInfo.InvariantCulture, "현재 {0:0.##}배속", speed);
        }

        /// <summary>배속을 바꿀 수 없을 때 머리줄 오른쪽에 보이는 글.</summary>
        public static string Locked(float speed)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}배속 고정", speed);
        }
    }
}
