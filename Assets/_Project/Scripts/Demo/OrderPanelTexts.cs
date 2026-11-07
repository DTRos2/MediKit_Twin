namespace MediKitTwin.Demo
{
    /// <summary>
    /// 주문 패널에 나오는 글. 화면 부품(프리팹을 만드는 도구와 화면 코드)이 같은 문구를 쓰도록 한곳에 모았다.
    /// 문구를 바꾸려면 이 클래스만 고친다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class OrderPanelTexts
    {
        public const string Title = "주문";
        public const string Subtitle = "ORDERS · CSV";
        public const string Guide = "순번,병원명,키트 코드,수량 — 한 줄에 주문 1건";
        public const string EmptyOrders = "주문이 없다";
        public const string ApplyButton = "적용";
        public const string SampleButton = "샘플 채우기";
        public const string FileButton = "파일 읽기";

        private const string IssueBullet = "• ";
        private const string TimesMark = "×";

        /// <summary>머리줄 오른쪽 요약: 주문 건수와 펼친 키트 수.</summary>
        public static string Summary(int orderCount, int kitCount)
        {
            return $"{orderCount}건 · 키트 {kitCount}개";
        }

        public static string Held(int count)
        {
            return $"보류 {count}건";
        }

        public static string Pending(int count)
        {
            return $"대기 키트 {count}개";
        }

        public static string Issues(int count)
        {
            return count == 0 ? "경고 없음" : $"경고 {count}건";
        }

        public static string Issue(string message)
        {
            return IssueBullet + message;
        }

        public static string Quantity(int quantity)
        {
            return TimesMark + quantity;
        }

        /// <summary>목록의 미리 놓은 칸에 들어가지 못한 항목 수를 알리는 글.</summary>
        public static string Overflow(int hiddenCount)
        {
            return $"외 {hiddenCount}건 더";
        }
    }
}
