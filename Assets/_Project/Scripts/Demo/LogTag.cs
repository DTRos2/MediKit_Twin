using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>로그 줄에 붙는 짧은 분류 이름표(READY, CAMERA …)와 그 색 계열.</summary>
    public readonly struct LogTag
    {
        public LogTag(string name, TagTone tone)
        {
            Name = name;
            Tone = tone;
        }

        public string Name { get; }

        public TagTone Tone { get; }
    }
}
