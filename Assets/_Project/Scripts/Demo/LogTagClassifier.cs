using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로그 한 줄에 분류 이름표를 정한다. 경고와 오류는 글 내용과 상관없이 WARN, ERROR로 눈에 띄게 하고,
    /// 나머지는 규칙 목록을 위에서부터 보며 글에 키워드가 처음 들어 있는 규칙의 이름표를 쓴다(대소문자 무시). 맞는 규칙이 없으면 기본 이름표다.
    /// 규칙을 코드 밖에서 주입할 수 있어, 로그 말투가 바뀌어도 이 클래스는 그대로 둔다. Unity 없이 시험할 수 있다.
    /// </summary>
    public sealed class LogTagClassifier
    {
        private const string WarningName = "WARN";
        private const string ErrorName = "ERROR";
        private const string DefaultFallbackName = "INFO";

        private static readonly LogTagRule[] DefaultRules =
        {
            new LogTagRule("충돌", "COLLIDER", TagTone.Violet),
            new LogTagRule("렌더", "RENDER", TagTone.Neutral),
            new LogTagRule("켬", "READY", TagTone.Success),
            new LogTagRule("준비", "READY", TagTone.Success),
            new LogTagRule("카메라", "CAMERA", TagTone.Accent),
            new LogTagRule("인식", "VISION", TagTone.Accent),
            new LogTagRule("완료", "DONE", TagTone.Success),
            new LogTagRule("집는", "PICK", TagTone.Accent),
            new LogTagRule("집음", "PICK", TagTone.Accent),
            new LogTagRule("놓", "PLACE", TagTone.Accent),
            new LogTagRule("재고", "STOCK", TagTone.Neutral),
            new LogTagRule("주문", "ORDER", TagTone.Neutral),
            new LogTagRule("베이스", "BASE", TagTone.Neutral),
        };

        private readonly IReadOnlyList<LogTagRule> _rules;
        private readonly string _fallbackName;

        public LogTagClassifier(IReadOnlyList<LogTagRule> rules, string fallbackName = DefaultFallbackName)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _fallbackName = fallbackName;
        }

        /// <summary>프로젝트의 로그 말투에 맞춘 기본 규칙.</summary>
        public static LogTagClassifier Default { get; } = new LogTagClassifier(DefaultRules);

        public LogTag Classify(LogSeverity severity, string text)
        {
            switch (severity)
            {
                case LogSeverity.Error:
                    return new LogTag(ErrorName, TagTone.Danger);
                case LogSeverity.Warning:
                    return new LogTag(WarningName, TagTone.Warning);
            }

            string body = text ?? string.Empty;
            foreach (LogTagRule rule in _rules)
            {
                if (body.IndexOf(rule.Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new LogTag(rule.Name, rule.Tone);
                }
            }

            return new LogTag(_fallbackName, TagTone.Neutral);
        }
    }
}
