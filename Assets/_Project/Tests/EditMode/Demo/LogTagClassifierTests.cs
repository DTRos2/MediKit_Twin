using System;
using System.Collections.Generic;
using MediKitTwin.Dashboard;
using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class LogTagClassifierTests
    {
        private static LogTagClassifier WithRules(params LogTagRule[] rules)
        {
            return new LogTagClassifier(new List<LogTagRule>(rules));
        }

        [Test]
        public void AWarning_IsTaggedWarn_NoMatterWhatTheTextSays()
        {
            LogTag tag = LogTagClassifier.Default.Classify(LogSeverity.Warning, "카메라 켬");

            Assert.That(tag.Name, Is.EqualTo("WARN"));
            Assert.That(tag.Tone, Is.EqualTo(TagTone.Warning));
        }

        [Test]
        public void AnError_IsTaggedError_NoMatterWhatTheTextSays()
        {
            LogTag tag = LogTagClassifier.Default.Classify(LogSeverity.Error, "충돌 상자 생성");

            Assert.That(tag.Name, Is.EqualTo("ERROR"));
            Assert.That(tag.Tone, Is.EqualTo(TagTone.Danger));
        }

        [Test]
        public void AKeywordInTheText_PicksTheRuleTag()
        {
            LogTagClassifier classifier = WithRules(new LogTagRule("카메라", "CAMERA", TagTone.Accent));

            LogTag tag = classifier.Classify(LogSeverity.Info, "카메라 켬 · 물건 인식 켬");

            Assert.That((tag.Name, tag.Tone), Is.EqualTo(("CAMERA", TagTone.Accent)));
        }

        [Test]
        public void TheFirstMatchingRule_Wins()
        {
            LogTagClassifier classifier = WithRules(
                new LogTagRule("충돌", "COLLIDER", TagTone.Violet),
                new LogTagRule("상자", "BOX", TagTone.Neutral));

            LogTag tag = classifier.Classify(LogSeverity.Info, "충돌 상자 223개 생성");

            Assert.That(tag.Name, Is.EqualTo("COLLIDER"));
        }

        [Test]
        public void TheKeywordMatch_IgnoresCase()
        {
            LogTagClassifier classifier = WithRules(new LogTagRule("ready", "READY", TagTone.Success));

            Assert.That(classifier.Classify(LogSeverity.Info, "Robot READY now").Name, Is.EqualTo("READY"));
        }

        [Test]
        public void WhenNoRuleMatches_TheFallbackTagIsUsed()
        {
            LogTagClassifier classifier = WithRules(new LogTagRule("카메라", "CAMERA", TagTone.Accent));

            LogTag tag = classifier.Classify(LogSeverity.Info, "아무 상관 없는 글");

            Assert.That((tag.Name, tag.Tone), Is.EqualTo(("INFO", TagTone.Neutral)));
        }

        [Test]
        public void TheFallbackName_CanBeChanged()
        {
            var classifier = new LogTagClassifier(new List<LogTagRule>(), "LOG");

            Assert.That(classifier.Classify(LogSeverity.Info, "글").Name, Is.EqualTo("LOG"));
        }

        [Test]
        public void ANullText_FallsBackInsteadOfThrowing()
        {
            Assert.That(LogTagClassifier.Default.Classify(LogSeverity.Info, null).Name, Is.EqualTo("INFO"));
        }

        [Test]
        public void TheRules_CannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LogTagClassifier(null));
        }

        [Test]
        public void TheDefaultRules_TagTheProjectsOwnLogWording()
        {
            // 규칙은 위에서부터 보므로 '렌더'가 '카메라'보다 먼저 맞는다.
            Assert.That(LogTagClassifier.Default.Classify(LogSeverity.Info, "카메라 화면에서 로봇 몸 렌더러 11개 숨김").Name, Is.EqualTo("RENDER"));
            Assert.That(LogTagClassifier.Default.Classify(LogSeverity.Info, "충돌 상자 223개를 만들었다").Name, Is.EqualTo("COLLIDER"));
            Assert.That(LogTagClassifier.Default.Classify(LogSeverity.Info, "베이스 이동 시작").Name, Is.EqualTo("BASE"));
        }
    }
}
