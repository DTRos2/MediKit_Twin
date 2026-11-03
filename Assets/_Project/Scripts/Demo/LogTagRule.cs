using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>로그 글에 keyword가 들어 있으면 name 이름표를 붙인다는 규칙 하나.</summary>
    public sealed class LogTagRule
    {
        public LogTagRule(string keyword, string name, TagTone tone)
        {
            Keyword = keyword;
            Name = name;
            Tone = tone;
        }

        public string Keyword { get; }

        public string Name { get; }

        public TagTone Tone { get; }
    }
}
