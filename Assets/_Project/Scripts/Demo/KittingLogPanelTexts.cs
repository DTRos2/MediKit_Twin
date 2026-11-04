using System.Globalization;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키팅 로그 패널에 나오는 글과 글 만들기. 프리팹을 만드는 도구와 화면 코드와 동작 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public static class KittingLogPanelTexts
    {
        public const string Title = "키팅 로그";
        public const string Subtitle = "KITTING LOG";

        public const string Filter = "경고·오류만";
        public const string Copy = "복사";
        public const string Clear = "지우기";

        /// <summary>화면에 보이는 로그 줄 수. 줄을 미리 이만큼 놓고 최근 줄부터 채운다(담아 둔 전체는 복사할 때 쓴다).</summary>
        public const int RowCapacity = 60;

        /// <summary>복사한 뒤 머리줄에 잠깐 적는 글. 복사할 줄이 없으면 그렇다고 알린다.</summary>
        public static string Copied(int lineCount)
        {
            return lineCount > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}줄 복사했다", lineCount)
                : "복사할 로그가 없다";
        }

        /// <summary>플레이를 시작하고 흐른 시간 글. 예: 12.3s</summary>
        public static string Elapsed(double seconds)
        {
            return seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }
    }
}
