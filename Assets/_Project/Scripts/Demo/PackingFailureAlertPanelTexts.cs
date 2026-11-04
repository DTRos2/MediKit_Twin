namespace MediKitTwin.Demo
{
    /// <summary>
    /// 팩킹 실패 경고 패널에 보이는 글. 문구를 화면 코드와 편집기 도구에 흩어 두지 않고 한곳에 모았다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class PackingFailureAlertPanelTexts
    {
        public const string Title = "팩킹 실패";
        public const string Subtitle = "PACKING · 확인 필요";

        /// <summary>뚜껑을 덮지 못한 키트를 알리는 줄. 키트 순번이 아직 없으면 순번을 0으로 적는다.</summary>
        public static string Headline(int kitSequence)
        {
            return $"키트 {kitSequence}번 뚜껑을 덮지 못했습니다.";
        }

        /// <summary>멈춘 것과 확인한 뒤에 일어날 일을 알리는 안내.</summary>
        public const string Guidance = "컨베이어와 팔 A를 멈췄습니다. 확인하면 팔 B가 다시 덮고, 덮이면 이어서 움직입니다.";

        /// <summary>다시 덮기를 요청하는 버튼에 적는 글.</summary>
        public const string RetryButton = "확인하고 다시 덮기";
    }
}
