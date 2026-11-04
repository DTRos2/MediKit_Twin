namespace MediKitTwin.Demo
{
    /// <summary>화면에 보여 줄 로그 한 줄: 원래 줄과 붙일 분류 이름표. 줄 높이는 화면의 레이아웃이 글 길이에 맞춰 정한다.</summary>
    public readonly struct LogRowEntry
    {
        public LogRowEntry(LogLine line, LogTag tag)
        {
            Line = line;
            Tag = tag;
        }

        public LogLine Line { get; }

        public LogTag Tag { get; }
    }
}
