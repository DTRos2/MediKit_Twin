namespace MediKitTwin.Demo
{
    /// <summary>화면에 그릴 로그 한 줄: 원래 줄, 붙일 분류 이름표, 줄 높이.</summary>
    public readonly struct LogRowEntry
    {
        public LogRowEntry(LogLine line, LogTag tag, float height)
        {
            Line = line;
            Tag = tag;
            Height = height;
        }

        public LogLine Line { get; }

        public LogTag Tag { get; }

        public float Height { get; }
    }
}
