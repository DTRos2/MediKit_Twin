namespace MediKitTwin.Demo
{
    /// <summary>
    /// 공정 현황 패널에 나오는 고정 글. 프리팹을 만드는 도구와 화면 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// 단계와 설비 상태를 말로 바꾸는 규칙은 ProcessStatusText가 맡는다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ProcessStatusPanelTexts
    {
        public const string Title = "공정 현황";
        public const string Subtitle = "PROCESS · 전체";
        public const string CurrentKit = "지금 키트";
        public const string Stations = "설비 상태";
        public const string Completed = "완료";
        public const string Rejected = "불량품";
        public const string StockShortage = "재고부족";
        public const string Fault = "오류";
    }
}
