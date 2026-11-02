using System.Globalization;

namespace MediKitTwin.Demo
{
    /// <summary>로그 한 줄: 플레이를 시작하고 흐른 시간(초), 무게, 글. 사람이 읽고 붙여 넣기 좋은 한 줄 글로 바꿀 수 있다.</summary>
    public readonly struct LogLine
    {
        public LogLine(double seconds, LogSeverity severity, string text)
        {
            Seconds = seconds;
            Severity = severity;
            Text = text ?? string.Empty;
        }

        public double Seconds { get; }

        public LogSeverity Severity { get; }

        public string Text { get; }

        /// <summary>"[12.3s] 글", 경고와 오류는 "[12.3s] 경고: 글", "[12.3s] 오류: 글".</summary>
        public override string ToString()
        {
            string time = Seconds.ToString("0.0", CultureInfo.InvariantCulture);
            switch (Severity)
            {
                case LogSeverity.Warning: return $"[{time}s] 경고: {Text}";
                case LogSeverity.Error: return $"[{time}s] 오류: {Text}";
                default: return $"[{time}s] {Text}";
            }
        }
    }
}
